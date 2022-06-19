using FFMpegCore;
using System.Text.RegularExpressions;
using VideoLibrary;
using Xabe.FFmpeg.Downloader;

namespace src
{
    class Video : IVideo
    {
        private const string VideoFileExtension = ".mp4";
        private const string AudioFileExtension = ".mp3";

        private readonly YouTube _youTube = YouTube.Default;

        private string _url = " ";
        private string _path = " ";
        private IEnumerable<YouTubeVideo> _videoData;
        private string _videoTitle = " ";
        private List<int>? _audioBitRate = new();
        private List<int>? _videoResolution = new();

        public string VideoTitle { get => _videoTitle; }
        public List<int>? AudioBitRate { get => _audioBitRate; }
        public List<int>? VideoResolution { get => _videoResolution; }

        public Video(string url, string path)
        {
            _url = url;
            _videoData = GetVideoData(_url);
            _path = @"" + path;
            _videoTitle = GetVideoTitle(_videoData);
            _videoResolution = GetVideoResulotion(_videoData);
            _audioBitRate = GetAudioBitRate(_videoData);
        }

        public async Task DownloadVideoWithAduio(int resolution, int bitRate)
        {
            string audioPath = await DownloadAudio(bitRate);
            string videoPath = await DownloadVideo(resolution);

            string path = _path + _videoTitle + "l" + VideoFileExtension;

            FFMpeg.ReplaceAudio(videoPath, audioPath, path);
            File.Delete(videoPath);
        }

        public async Task<string> DownloadVideo(int resolution)
        {
            string uri = GetUri(GetVideo(_url, resolution));
            string path = _path + _videoTitle + VideoFileExtension;

            await Download(path, uri);
            return path;
        }

        public async Task<string> DownloadAudio(int bitRate)
        {
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Full);

            string uri = GetUri(GetAudio(_url, bitRate));
            string TempPath = _path + _videoTitle + "Temp" + VideoFileExtension;

            await Download(TempPath, uri);
            string path = _path + _videoTitle + AudioFileExtension;

            FFMpeg.ExtractAudio(TempPath, path);
            File.Delete(TempPath);
            return path;
        }

        private async Task DownloadVideo(string uri)
        {
            string path = _path + _videoTitle + VideoFileExtension;
            await Download(path, uri);
        }

        private async Task DownloadAudio(string uri)
        {
            string TempPath = _path + _videoTitle + "Temp" + VideoFileExtension;

            await Download(TempPath, uri);
            FFMpeg.ExtractAudio(TempPath, _path + _videoTitle + AudioFileExtension);
            File.Delete(TempPath);
        }

        private async Task Download(string path, string uri)
        {
            using (Stream output = File.OpenWrite(path))
            {
                await StreamInFile(uri, output);
            }
        }

        private async Task StreamInFile(string uri, Stream output)
        {
            HttpClient httpClient = new();
            using Stream input = await httpClient.GetStreamAsync(uri);
            byte[] buffer = new byte[16 * 1024];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, read);
            httpClient.Dispose();
        }

        private string GetUri(YouTubeVideo data) =>
            data.Uri;

        private YouTubeVideo GetAudio(string url, int selectedAudioBitRate)
        {
            var TargetAudio = GetVideoData(url).Where(response => response.AdaptiveKind == AdaptiveKind.Audio &&
               response.AudioBitrate == selectedAudioBitRate).
               Select(response => response);
            return TargetAudio.First();
        }

        private YouTubeVideo GetVideo(string url, int selectedVideoQuality)
        {
            var TargetVideo = GetVideoData(url).Where(response => response.AdaptiveKind == AdaptiveKind.Video &&
                response.Format == VideoFormat.Mp4 && response.Resolution == selectedVideoQuality).
                Select(response => response);
            return TargetVideo.First();
        }

        private IEnumerable<YouTubeVideo> GetVideoData(string url) =>
            _youTube.GetAllVideos(url);

        private string? GetVideoTitle(IEnumerable<YouTubeVideo> videoData)
        {
            string title = videoData.First().Title;
            if(title == null) throw new Exception("You died");
            return Regex.Replace(title, @"\W+", " ");
        }

        private List<int> GetAudioBitRate(IEnumerable<YouTubeVideo>? videoData)
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

        private List<int> GetVideoResulotion(IEnumerable<YouTubeVideo>? videoData)
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
    }
}
