using System.Diagnostics;

namespace BugRemover
{
    public static class Common
    {
        public static string RunFfmpeg(string ffmpegPath, string ffmpegArgs)
        {
            string output = string.Empty;
            using (Process ffmpeg = new Process())
            {
                ffmpeg.StartInfo.FileName = $"\"{ffmpegPath}\\ffmpeg\"";
                ffmpeg.StartInfo.Arguments = ffmpegArgs;
                ffmpeg.StartInfo.RedirectStandardError = true;
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.CreateNoWindow = true;

                ffmpeg.Start();

                output = ffmpeg.StandardError.ReadToEnd();
                ffmpeg.WaitForExit();
            }

            return output;
        }
    }
}
