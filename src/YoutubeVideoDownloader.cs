using FFMpegCore;
using System.Text.RegularExpressions;
using VideoLibrary;
using Xabe.FFmpeg.Downloader;

namespace src
{
    class YoutubeVideoDownloader
    {
        private const string VideoFileExtension = ".mp4";
        private const string AudioFileExtension = ".mp3";

        private static readonly YouTube _youTube = YouTube.Default;

        private static string MainPath = "";
        private static string videoTitle = " ";
        private static List<int>? audioBitRate = new();
        private static List<int>? videoResolution = new();


        private static async Task Main()
        {
            await ConsoleApp();
        }

        //For test in console
        private static async Task ConsoleApp()
        {
            ClearVideoData();

            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Full);

            Console.WriteLine("Enter path to video download: ");
            MainPath = Console.ReadLine();
            MainPath = @"" + MainPath;
            if (MainPath == null)
            {
                Console.WriteLine("Wrong path");
                return;
            }

            Console.WriteLine("Enter video url: ");
            var url = Console.ReadLine();

            IEnumerable<YouTubeVideo> videoData = GetVideoData(url);
            videoTitle = GetVideoTitle(videoData);
            audioBitRate = GetAudioBitRate(videoData);
            videoResolution = GetVideoResulotion(videoData);
            OutputVideoData(videoTitle, videoResolution, audioBitRate);

            Console.WriteLine("Enter video resolution(if 0 only audio): ");
            int resolution = int.Parse(Console.ReadLine());

            Console.WriteLine("Enter bit rate(if 0 only video): ");
            int bitRate = int.Parse(Console.ReadLine());

            if (resolution > 0 && bitRate > 0)
            {
                await Download(url, MainPath, videoTitle, resolution, bitRate: bitRate);
            }
            else if (resolution > 0)
            {
                await Download(url, MainPath, videoTitle, resolution);
            }
            else if (bitRate > 0)
            {
                await Download(url, MainPath, videoTitle, bitRate: bitRate);
            }

            Console.WriteLine("Download ended");
        }

        //Video with audio
        private static async Task Download(string url, string path, string videoTitle, int resolution, int bitRate)
        {
            Task audio = SourceDownloader(MainPath + "Audio.mp4", TakeAudio(url, bitRate));
            Task video = SourceDownloader(MainPath + "Video.mp4", TakeVideo(url, resolution));

            await audio;

            FFMpeg.ExtractAudio(MainPath + "Audio.mp4", MainPath + "Audio.mp3");
            File.Delete(MainPath + "Audio.mp4");

            await video;

            FFMpeg.ReplaceAudio(MainPath + "Video.mp4", MainPath + "Audio.mp3", MainPath + videoTitle + VideoFileExtension);
            File.Delete(MainPath + "Video.mp4");
            File.Delete(MainPath + "Audio.mp3");
        }

        //Only audio
        private static async Task Download(string url, string path, string videoTitle, int bitRate, bool isOnlyAudio = true)
        {

            Task audio = SourceDownloader(MainPath + "Audio.mp4", TakeAudio(url, bitRate));
            await audio;
            FFMpeg.ExtractAudio(MainPath + "Audio.mp4", MainPath + videoTitle + "Audio.mp3");
            File.Delete(MainPath + "Audio.mp4");
        }

        //Only video
        private static async Task Download(string url, string path, string videoTitle, int resolution)
        {
            Task video = SourceDownloader(MainPath + "Video.mp4", TakeVideo(url, resolution));
            await video;
        }

        //Download video
        private static async Task SourceDownloader(string path, YouTubeVideo video)
        {
            HttpClient httpClient = new();
            using (Stream output = File.OpenWrite(path))
            {
                long? totalBytes = GetTotalVideoBytes(video, httpClient);
                await StreamInFile(video, httpClient, output, totalBytes);
            }
            httpClient.Dispose();
        }

        //Write stream in file
        private static async Task StreamInFile(YouTubeVideo video, HttpClient httpClient, Stream output, long? totalBytes)
        {
            using (Stream input = await httpClient.GetStreamAsync(video.Uri))
            {
                byte[] buffer = new byte[16 * 1024];
                int read;
                int totalRead = 0;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                    totalRead += read;
                    //Console.WriteLine(totalRead + " / " + totalBytes);
                }
            }
        }

        //Get download size
        private static long? GetTotalVideoBytes(YouTubeVideo video, HttpClient httpClient)
        {
            long? totalBytes;
            using (HttpRequestMessage? request = new HttpRequestMessage(HttpMethod.Head, video.Uri))
            {
                totalBytes = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result.Content.Headers.ContentLength;
            }

            return totalBytes;
        }

        //Return one audio with selected bit rate
        private static YouTubeVideo TakeAudio(string url, int selectedAudioBitRate)
        {
            var TargetAudio = GetVideoData(url).Where(response => response.AdaptiveKind == AdaptiveKind.Audio &&
               response.AudioBitrate == selectedAudioBitRate).
               Select(response => response);
            return TargetAudio.First();
        }

        //Return one video with selected resolution
        private static YouTubeVideo TakeVideo(string url, int selectedVideoQuality)
        {
            var TargetVideo = GetVideoData(url).Where(response => response.AdaptiveKind == AdaptiveKind.Video &&
                response.Format == VideoFormat.Mp4 && response.Resolution == selectedVideoQuality).
                Select(response => response);
            return TargetVideo.First();
        }

        //Output data for console app
        private static void OutputVideoData(string? videoTitle, List<int> videoResolution, List<int> audioBitRate)
        {
            Console.WriteLine($"Title: {videoTitle}");
            Console.WriteLine("Video resolution: ");
            for (int i = 0; i < videoResolution.Count; i++)
            {
                Console.WriteLine(videoResolution[i]);
            }

            Console.WriteLine("Audio bitRate: ");

            for (int i = 0; i < audioBitRate.Count; i++)
            {
                Console.WriteLine(audioBitRate[i]);
            }
        }

        //Get all data of video without video
        private static IEnumerable<YouTubeVideo> GetVideoData(string url) =>
            _youTube.GetAllVideos(url);

        //Get video title
        private static string? GetVideoTitle(IEnumerable<YouTubeVideo>? videoData) =>
            Regex.Replace(videoData?.First().Title, @"\W+", " ");

        //Get all audio bit rates
        private static List<int> GetAudioBitRate(IEnumerable<YouTubeVideo>? videoData)
        {
            if (videoData == null) throw new Exception("Wrong data");
            List<int> audioBitRate = new();
            IEnumerable<int>? bitRate = videoData.Where(response => response.AdaptiveKind == AdaptiveKind.Audio)
                            .Select(response => response.AudioBitrate);

            foreach (int item in bitRate)
            {
                if (!audioBitRate.Contains(item))
                    audioBitRate.Add(item);
            }

            audioBitRate.Sort();

            return audioBitRate;
        }

        //Get all video resolutions
        private static List<int> GetVideoResulotion(IEnumerable<YouTubeVideo>? videoData)
        {
            if (videoData == null) throw new Exception("Wrong data");
            List<int> videoResolution = new();
            IEnumerable<int>? resolution = videoData.Where(response => response.AdaptiveKind == AdaptiveKind.Video && response.Format == VideoFormat.Mp4).
                            Select(response => response.Resolution);

            foreach (int item in resolution)
            {
                if (!videoResolution.Contains(item))
                    videoResolution.Add(item);
            }

            videoResolution.Sort();

            return videoResolution;
        }

        //Clear videotitle and lists of resolution and bit rate
        private static void ClearVideoData()
        {
            videoTitle = null;
            videoResolution?.Clear();
            audioBitRate?.Clear();
        }
    }
}