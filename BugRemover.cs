using System.Diagnostics;
using System.Globalization;


namespace BugRemover
{

    // Switch to 
    public class BugRemover
    {
        private string _ffmpegPath = string.Empty;

        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Utilities.ConsoleWithLog("BugRemover version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
                Utilities.ConsoleWithLog("");
                DisplayHelp();

                return;
            }
            BugRemover p = new BugRemover();
            p.RealMain(args);
        }

        public void RealMain(string[] args)
        {
            Utilities.CleanLog();

            Utilities.ConsoleWithLog("BugRemover version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            Utilities.ConsoleWithLog("");
        
            string inFile = string.Empty;
            int framesToExtract = 0;
            bool found;
            int startX = -1;
            int startY = -1;
            int width = -1;
            int height = -1;

            bool paramsOK = true;
            bool doExtract = false;
            foreach (string arg in args)
            {
                if (arg.ToLower() == "-?" || arg.ToLower() == "-h" || arg.ToLower() == "-help")
                {
                    DisplayHelp();
                    return;
                }

                switch (arg.Substring(0, arg.IndexOf('=')).ToLower())
                {
                    case "-extract":
                        doExtract = true;

                        found = int.TryParse(arg.Substring(arg.IndexOf('=') + 1).Trim(), CultureInfo.InvariantCulture, out framesToExtract);
                        if (!found || framesToExtract <= 0)
                        {
                            Utilities.ConsoleWithLog("Invalid or no frame count specified.");
                            DisplayHelp();
                            return;
                        }

                        break;

                    case "-ffmpegpath":
                        _ffmpegPath = arg.Substring(arg.IndexOf('=') + 1).Trim();
                        if(!File.Exists($"{_ffmpegPath}\\ffmpeg.exe"))
                        {
                            Utilities.ConsoleWithLog($"ffmpeg not found at the specified path {_ffmpegPath}.");
                            DisplayHelp();
                            return;
                        }
                        break;

                    case "-infile":
                        inFile = arg.Substring(arg.IndexOf('=') + 1).Trim();
                        break;

                    case "-startx":
                        found = int.TryParse(arg.Substring(arg.IndexOf('=') + 1).Trim(), CultureInfo.InvariantCulture, out startX);
                        if (!found || startX <= 0)
                        {
                            Utilities.ConsoleWithLog("Invalid or no startX specified.");
                            DisplayHelp();
                            return;
                        }

                        break;

                    case "-starty":
                        found = int.TryParse(arg.Substring(arg.IndexOf('=') + 1).Trim(), CultureInfo.InvariantCulture, out startY);
                        if (!found || startY <= 0)
                        {
                            Utilities.ConsoleWithLog("Invalid or no startY specified.");
                            DisplayHelp();
                            return;
                        }

                        break;

                    case "-width":
                        found = int.TryParse(arg.Substring(arg.IndexOf('=') + 1).Trim(), CultureInfo.InvariantCulture, out width);
                        if (!found || width <= 0)
                        {
                            Utilities.ConsoleWithLog("Invalid or no width specified.");
                            DisplayHelp();
                            return;
                        }

                        break;

                    case "-height":
                        found = int.TryParse(arg.Substring(arg.IndexOf('=') + 1).Trim(), CultureInfo.InvariantCulture, out height);
                        if (!found || height <= 0)
                        {
                            Utilities.ConsoleWithLog("Invalid or no height specified.");
                            DisplayHelp();
                            return;
                        }

                        break;

                    default:
                        paramsOK = false;
                        Utilities.ConsoleWithLog("Unknown parameter: " + arg);
                        break;
                }
            }

            if (paramsOK == false)
                return;

            if(!File.Exists(inFile))
            {
                Utilities.ConsoleWithLog($"{inFile} not found.");
                DisplayHelp();
                return;
            }

            if (doExtract)
            {
                Utilities.ConsoleWithLog($"Extracting {framesToExtract} frames.");

                if (string.IsNullOrEmpty(inFile) || string.IsNullOrEmpty(_ffmpegPath))
                {
                    Utilities.ConsoleWithLog("Input file and ffmpeg path must be specified.");
                    DisplayHelp();
                    return;
                }

                if (ExtractSomeVideoFrames(inFile, framesToExtract))
                    Utilities.ConsoleWithLog($"{framesToExtract} frames extracted successfully to {Path.GetDirectoryName(inFile)}.");
                else
                    Utilities.ConsoleWithLog("Error extracting frames.");

                return;
            }

            string outputFile = $"{inFile.FullFileNameWithoutExtention()}_bugFree.mp4";

            Utilities.ConsoleWithLog("Starting the bug removal process ...");

            RemoveTheBug(inFile, startX, startY, width, height);

            Utilities.ConsoleWithLog($"All done. {outputFile} created.");
        }

        private bool ExtractSomeVideoFrames(string videoFilePath, int frameCount)
        {
            string outputFile = $"{videoFilePath.FullFileNameWithoutExtention()}_%08d.png";
            string ffmpegArgs = $"-i \"{videoFilePath}\" -vf \"select='lte(n\\,{frameCount - 1})'\" -fps_mode vfr -q:v 2 \"{outputFile}\"";

            RunFfmpeg(ffmpegArgs);

            return true;
        }

        /// <summary>
        /// Remove the bug (logo) from the video file.
        /// </summary>
        /// <param name="videoFilePath"></param>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        /// <see cref="https://www.ffmpegtoolkit.com/2021/07/23/remove-station-logo-from-video-using-ffmpeg/"/>
        private bool RemoveTheBug(string videoFilePath, int startX, int startY, int width, int height)
        {
            // For debugging, show=1 draws a green box around the bug and -t 15 to only apply to the first 15 seconds of the video
            string outputFile = $"{videoFilePath.FullFileNameWithoutExtention()}_BugFree.mkv";
            string ffmpegArgs = $"-i \"{videoFilePath}\" -vf delogo=x={startX}:y={startY}:w={width}:h={height}:show=0 -c:a copy -c:s copy -map 0 -map -0:s:m:language:iso639-2 \"{outputFile}\"";

            RunFfmpeg(ffmpegArgs);

            return true;
        }

        private string RunFfmpeg(string ffmpegArgs)
        {
            string output = string.Empty;
            using (Process ffmpeg = new Process())
            {
                ffmpeg.StartInfo.FileName = $"\"{_ffmpegPath}\\ffmpeg\"";
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

        private static void DisplayHelp()
        {
            Utilities.ConsoleWithLog("");
            Utilities.ConsoleWithLog("BugRemover is a tool to remove bugs from video files.");
            Utilities.ConsoleWithLog("");
            Utilities.ConsoleWithLog("Usage:");
            Utilities.ConsoleWithLog("  BugRemover [-extract=frameCount] -ffmpegPath=path -startX=x -startY=y -width=w  -height=h -inFile=VideoFile");
            Utilities.ConsoleWithLog("");
            Utilities.ConsoleWithLog("Parameters:");
            Utilities.ConsoleWithLog("  -extract=frameCount\t(Optional) Extracts frameCount frames to assist in figure out the bug position parameters.");
            Utilities.ConsoleWithLog("  -ffmpegPath=path\tThe path of the ffmpeg executable. The path only, not the file name.");
            Utilities.ConsoleWithLog("  -startX=x\t\tDefines the upper left x-axis position of the square with the bug.");
            Utilities.ConsoleWithLog("  -startY=y\t\tDefines the upper left y-axis position of the square with the bug.");
            Utilities.ConsoleWithLog("  -width=w\t\tDefines the width of the square containing the bug.");
            Utilities.ConsoleWithLog("  -height=h\t\tDefines the height of the square containing the bug.");
            Utilities.ConsoleWithLog("  -inFile=VideoFile\tThe full path of the video file.");
            Utilities.ConsoleWithLog("");
        }

    }
}
