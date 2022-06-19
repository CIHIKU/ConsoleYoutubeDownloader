namespace src
{
    interface IVideo
    {
        string VideoTitle { get; }
        List<int>? AudioBitRate { get; }
        List<int>? VideoResolution { get; }
        

        Task<string> DownloadAudio(int bitRate);
        Task<string> DownloadVideo(int resolution);
        Task DownloadVideoWithAduio(int resolution, int bitRate);
    }
}