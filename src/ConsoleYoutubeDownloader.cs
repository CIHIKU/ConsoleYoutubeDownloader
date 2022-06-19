namespace src
{
    class ConsoleYoutubeDownloader
    {
        private static async Task Main()
        {
            Video video = new(InputUrl(), InputPath());

            OutputVideoData(video);

            await video.DownloadVideoWithAduio(InputResolution(), InputBitRate());

            Console.WriteLine("Dowload end");
            Console.ReadLine();
        }

        private static string InputPath()
        {
            Console.WriteLine("Enter path");
            string path = Console.ReadLine();
            if(path == null) throw new Exception("You lox");
            return path;
        }

        private static void OutputVideoData(Video video)
        {
            Console.WriteLine($"Title: {video.VideoTitle}");
            Console.WriteLine("Resolutions: ");
            for (int i = 0; i < video.VideoResolution.Count; i++)
            {
                Console.WriteLine(video.VideoResolution[i]);
            }
            Console.WriteLine("-----------");
            Console.WriteLine("Audio bitRate: ");
            for (int i = 0; i < video.AudioBitRate.Count; i++)
            {
                Console.WriteLine(video.AudioBitRate[i]);
            }
        }

        private static int InputBitRate()
        {
            try
            {
                Console.WriteLine("Enter bit rate: ");
                string? stringBiR = Console.ReadLine();
                if (stringBiR == null) return 0;
                int bitRate = int.Parse(stringBiR);
                return bitRate;
            }
            catch
            {
                return 0;
            }
            
        }

        private static int InputResolution()
        {
            try
            {
                Console.WriteLine("Enter resolution: ");
                string? stringRes = Console.ReadLine();
                if(stringRes == null) return 0;
                int resolution = int.Parse(stringRes);
                return resolution;
            }
            catch
            {
                return 0;
            }
        }

        private static string? InputUrl()
        {
            Console.WriteLine("Enter url: ");
            var url = Console.ReadLine();
            if (url == null) throw new Exception("You lox");
            return url;
        }
    }
}