using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Zavolokas.ImageProcessing.Inpainting;

namespace BugRemover
{
    public class BugRemover
    {
        public string workingDir = AppContext.BaseDirectory;

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

            string ffmpegPath = string.Empty;
            string inFile = string.Empty;
            int framesToExtract = 0;
            bool found = false;
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
                        ffmpegPath = arg.Substring(arg.IndexOf('=') + 1).Trim();
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

            if (doExtract)
            {
                Utilities.ConsoleWithLog($"Extracting {framesToExtract} frames.");

                if (string.IsNullOrEmpty(inFile) || string.IsNullOrEmpty(ffmpegPath))
                {
                    Utilities.ConsoleWithLog("Input file and ffmpeg path must be specified.");
                    DisplayHelp();
                    return;
                }

                if (ExtractSomeVideoFrames(inFile, ffmpegPath, framesToExtract))
                    Utilities.ConsoleWithLog($"{framesToExtract} frames extracted successfully.");
                else
                    Utilities.ConsoleWithLog("Error extracting frames.");

                return;
            }

            string pixelFormat = GetVideoPixelFormat(ffmpegPath, inFile);

            if (ExtractAllVideoFrames(inFile, ffmpegPath) == false)
            {
                Utilities.ConsoleWithLog("Error extracting frames.");
                return;
            }
        }

        private static bool ExtractSomeVideoFrames(string videoFilePath, string ffmpegPath, int frameCount)
        {
            //  string ffmpegArgs = $"-i \"{videoFilePath}\" -c copy -map 0:v -map 0:a \"{intermediateFilePath}\"";
            string outputFile = $"{videoFilePath.FullFileNameWithoutExtention()}_%08d.png";
            string ffmpegArgs = $"-i \"{videoFilePath}\" -vf \"select='lte(n\\,{frameCount - 1})'\" -fps_mode vfr -q:v 2 \"{outputFile}\"";

            // Set up the process to run FFmpeg
            using (Process ffmpeg = new Process())
            {
                ffmpeg.StartInfo.FileName = $"\"{ffmpegPath}\\ffmpeg\"";
                ffmpeg.StartInfo.Arguments = ffmpegArgs;
                ffmpeg.StartInfo.RedirectStandardError = true;
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.CreateNoWindow = true;

                // Start the process
                ffmpeg.Start();

                // Read the output
                string output = ffmpeg.StandardError.ReadToEnd();
                ffmpeg.WaitForExit();
            }

            return true;
        }

        private static bool ExtractAllVideoFrames(string videoFilePath, string ffmpegPath)
        {
            //  string ffmpegArgs = $"-i \"{videoFilePath}\" -c copy -map 0:v -map 0:a \"{intermediateFilePath}\"";
            string outputFile = $"{videoFilePath.FullFileNameWithoutExtention()}_extract_%08d.png";
            string ffmpegArgs = $"-i \"{videoFilePath}\" \"{outputFile}\"";

            // Set up the process to run FFmpeg
            using (Process ffmpeg = new Process())
            {
                ffmpeg.StartInfo.FileName = $"\"{ffmpegPath}\\ffmpeg\"";
                ffmpeg.StartInfo.Arguments = ffmpegArgs;
                ffmpeg.StartInfo.RedirectStandardError = true;
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.CreateNoWindow = true;

                // Start the process
                ffmpeg.Start();

                // Read the output
                string output = ffmpeg.StandardError.ReadToEnd();
                ffmpeg.WaitForExit();
            }

            return true;
        }

        static void InpaintFrames(string inputDir, string outputDir, int startX, int startY, int width, int height)
        {
            if (!System.IO.Directory.Exists(outputDir))
                System.IO.Directory.CreateDirectory(outputDir);

            var files = System.IO.Directory.GetFiles(inputDir, "frame_*.png");

            foreach (var file in files)
            {
                using (var image = new Bitmap(file))
                {
                    var mask = CreateMask(image.Width, image.Height, startX, startY, width, height);
                    var inpaintedImage = Inpaint(image, mask);

                    string outputFilePath = System.IO.Path.Combine(outputDir, System.IO.Path.GetFileName(file));
                    inpaintedImage.Save(outputFilePath);
                }
            }
        }

        static Bitmap CreateMask(int imgWidth, int imgHeight, int startX, int startY, int width, int height)
        {
            var mask = new Bitmap(imgWidth, imgHeight);
            using (Graphics g = Graphics.FromImage(mask))
            {
                g.FillRectangle(Brushes.Black, 0, 0, imgWidth, imgHeight);
                g.FillRectangle(Brushes.White, startX, startY, width, height);
            }
            return mask;
        }

        static Bitmap Inpaint(Bitmap image, Bitmap mask)
        {
            var src = BitmapConverter.ToMat(image);
            var maskMat = BitmapConverter.ToMat(mask);
            var inpaintedMat = new Mat();

            Cv2.Inpaint(src, maskMat, inpaintedMat, 3, InpaintMethod.Telea);

            return BitmapConverter.ToBitmap(inpaintedMat);
        }

        static void ReassembleVideo(string ffmpegPath, string inputDir, string outputVideo, double frameRate, string pixelFormat)
        { 
            string ffmpegArgs = $"ffmpeg -framerate {frameRate} -i {inputDir}/frame_%08d.png -c:v libx264 -pix_fmt {pixelFormat} {outputVideo}";
            using (Process ffmpeg = new Process())
            {
                ffmpeg.StartInfo.FileName = $"\"{ffmpegPath}\\ffmpeg\"";
                ffmpeg.StartInfo.Arguments = ffmpegArgs;
                ffmpeg.StartInfo.RedirectStandardError = true;
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.CreateNoWindow = true;

                // Start the process
                ffmpeg.Start();

                // Read the output
                string output = ffmpeg.StandardError.ReadToEnd();
                ffmpeg.WaitForExit();
            }
        }

        public static double GetVideoFPS(string ffmpegPath, string videoFilePath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(ffmpegPath, "ffprobe"),
                    Arguments = $"-v error -select_streams v:0 -show_entries stream=r_frame_rate -of default=noprint_wrappers=1:nokey=1 \"{videoFilePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            // Parse the output to get the FPS
            string[] parts = output.Trim().Split('/');

            double.TryParse(parts[0], out double numerator);
            double.TryParse(parts[1], out double denominator);

            double fps = numerator / denominator;
            return fps;
        }

        public static string GetVideoPixelFormat(string ffmpegPath, string videoFilePath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(ffmpegPath, "ffprobe"),
                    Arguments = $"-v error -select_streams v:0 -show_entries stream=pix_fmt -of default=noprint_wrappers=1:nokey=1 \"{videoFilePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            // Parse the output to get the pixel format
            string pixelFormat = output.Trim();
            return pixelFormat;
        }

        private static void DisplayHelp()
        {
            Utilities.ConsoleWithLog("BugRemover is a tool to remove bugs from video files.");
            Utilities.ConsoleWithLog("");
            Utilities.ConsoleWithLog("Usage:");
            Utilities.ConsoleWithLog("  BugRemover -extract <inputfile> <outputfile>");
            Utilities.ConsoleWithLog("");
            Utilities.ConsoleWithLog("Options:");
            Utilities.ConsoleWithLog("  -extract  Extracts the video from the input file and saves it to the output file.");
            Utilities.ConsoleWithLog("");
        }

    }
}
