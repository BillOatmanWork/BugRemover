using System.Globalization;
using System.Text.RegularExpressions;

namespace BugRemover
{
    /// <summary>
    /// Auto detect bug regions (yes potentially plural) in a video file. Considered experimental at this point.
    /// </summary>
    public static class BugDetection
    {
        public static List<BugRegion> DetectBugRegions(string ffmpegPath, string inputFilePath, double staticFrameThreshold = 0.01, double logoDurationThreshold = 5)
        {
            double frameRate = GetFrameRate(ffmpegPath, inputFilePath);

            // Adjust the ffmpeg arguments to use the freezedetect filter for detecting static regions
            string arguments = $"-i \"{inputFilePath}\" -vf \"freezedetect=n={staticFrameThreshold}:d={logoDurationThreshold}\" -an -f null -";
            string output = Common.RunFfmpeg(ffmpegPath, arguments);

            // Dump FFmpeg output to a file
            File.WriteAllText(@"..\..\..\Doc\freezedetect.txt", output);

            List<BugRegion> bugRegions = new List<BugRegion>();

            string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            double startTime = 0;
            foreach (string line in lines)
            {
                if (line.Contains("freeze_start"))
                {
                    startTime = ExtractTime(line, "freeze_start");
                }
                else if (line.Contains("freeze_end"))
                {
                    double endTime = ExtractTime(line, "freeze_end");
                    int startFrame = (int)(startTime * frameRate);
                    int endFrame = (int)(endTime * frameRate);

                    // Parse the line to extract the position and dimensions
                    int startX = ExtractValue(line, "x");
                    int startY = ExtractValue(line, "y");
                    int width = ExtractValue(line, "w");
                    int height = ExtractValue(line, "h");

                    bugRegions.Add(new BugRegion
                    {
                        StartFrame = startFrame,
                        EndFrame = endFrame,
                        StartX = startX,
                        StartY = startY,
                        Width = width,
                        Height = height
                    });
                }
            }

            return bugRegions;
        }

        private static double GetFrameRate(string ffmpegPath, string inputFilePath)
        {
            string ffmpegArgs = $"-v 0 -of csv=p=0 -select_streams v:0 -show_entries stream=r_frame_rate {inputFilePath}";
            string output = Common.RunFfmpeg(ffmpegPath, ffmpegArgs);

            string[] parts = output.Trim().Split('/');
            if (parts.Length == 2 && int.TryParse(parts[0], out int numerator) && int.TryParse(parts[1], out int denominator))
            {
                return (double)numerator / denominator;
            }
            return 0;
        }

        private static double ExtractTime(string line, string keyword)
        {
            string pattern = $@"{keyword}: (\d+(\.\d+)?)";
            Match match = Regex.Match(line, pattern);
            if (match.Success)
            {
                return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            }
            return 0;
        }

        private static int ExtractValue(string line, string keyword)
        {
            string pattern = $@"{keyword}=(\d+)";
            Match match = Regex.Match(line, pattern);
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            }
            return 0;
        }
    }

    public class BugRegion
    {
        public int StartFrame { get; set; }
        public int EndFrame { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public override string ToString()
        {
            return $"StartFrame: {StartFrame}, EndFrame: {EndFrame}, X: {StartX}, Y: {StartY}, Width: {Width}, Height: {Height}";
        }
    }
}
