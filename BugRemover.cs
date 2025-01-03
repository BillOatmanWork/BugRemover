using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using OpenCvSharp;

namespace BugRemover
{

    // Switch to 
    public class BugRemover
    {
        private readonly string _workingDir = AppContext.BaseDirectory;
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
                        _ffmpegPath = arg.Substring(arg.IndexOf('=') + 1).Trim();
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

            string pixelFormat = GetVideoPixelFormat(inFile);
            double fps = GetVideoFPS(inFile);
            string framesDir = System.IO.Path.Combine(_workingDir, "frames");
            string maskedFramesDir = System.IO.Path.Combine(_workingDir, "maskedframes");

            if (!System.IO.Directory.Exists(framesDir))
                System.IO.Directory.CreateDirectory(framesDir);

            if (!System.IO.Directory.Exists(maskedFramesDir))
                System.IO.Directory.CreateDirectory(maskedFramesDir);

            string outputFile = $"{inFile.FullFileNameWithoutExtention()}_bugFree.mp4";

            Utilities.ConsoleWithLog("Extracting the original frames ...");
            //if (ExtractAllVideoFrames(inFile, framesDir) == false)
            //{
            //    Utilities.ConsoleWithLog("Error extracting frames.");
            //    return;
            //}

            Utilities.ConsoleWithLog("Inpainting the original frames ...");
            InpaintFrames(framesDir, maskedFramesDir, startX, startY, width, height);

            Utilities.ConsoleWithLog("Reassembling to create the new video ...");
            ReassembleVideo(maskedFramesDir, outputFile, fps, pixelFormat);

            Utilities.ConsoleWithLog("Cleaning up ...");
            DirectoryInfo directory = new DirectoryInfo(framesDir);
            directory.Delete(true);

            DirectoryInfo directory2 = new DirectoryInfo(maskedFramesDir);
            directory2.Delete(true);

            Utilities.ConsoleWithLog($"All done. {outputFile} created.");
        }

        private bool ExtractSomeVideoFrames(string videoFilePath, int frameCount)
        {
            string outputFile = $"{videoFilePath.FullFileNameWithoutExtention()}_%08d.png";
            string ffmpegArgs = $"-i \"{videoFilePath}\" -vf \"select='lte(n\\,{frameCount - 1})'\" -fps_mode vfr -q:v 2 \"{outputFile}\"";

            RunFfmpeg(ffmpegArgs);

            return true;
        }

        private bool ExtractAllVideoFrames(string videoFilePath, string outputDir)
        {
         //   string outputFile = $"{videoFilePath.FullFileNameWithoutExtention()}_extract_%08d.png";
            string ffmpegArgs = $"-i \"{videoFilePath}\" \"{outputDir}\"\\frame_%08d.png";

            RunFfmpeg(ffmpegArgs);

            return true;
        }

        private static void InpaintFrames(string inputDir, string outputDir, int startX, int startY, int width, int height)
        {
            if (!System.IO.Directory.Exists(outputDir))
                System.IO.Directory.CreateDirectory(outputDir);

            var files = System.IO.Directory.GetFiles(inputDir, "frame_*.png");

            foreach (var file in files)
            {
                //  Mat src = Cv2.ImRead(file, ImreadModes.Color);
                Mat src = Cv2.ImRead(file, ImreadModes.AnyDepth | ImreadModes.AnyColor);

                // Create a mask
                Mat mask = new Mat(src.Size(), MatType.CV_8UC1);
                Cv2.Rectangle(mask, new Rect(startX, startY, width, height), Scalar.White, -1);

                Mat dst = src.Clone();

                // Define gridSize as a local variable
                int gridSize = 50; // Adjust this value as needed

                // Iteratively inpaint smaller sections
                for (int y = startY; y < startY + height; y += gridSize)
                {
                    for (int x = startX; x < startX + width; x += gridSize)
                    {
                        Rect rect = new Rect(x, y, Math.Min(gridSize, startX + width - x), Math.Min(gridSize, startY + height - y));
                        Mat srcROI = new Mat(src, rect);
                        Mat maskROI = new Mat(mask, rect);
                        Mat dstROI = new Mat(dst, rect);

                        Cv2.Inpaint(srcROI, maskROI, dstROI, 3, InpaintMethod.NS);
                    }
                }

                // Save the result
                string outputFilePath = System.IO.Path.Combine(outputDir, System.IO.Path.GetFileName(file));
                Cv2.ImWrite(outputFilePath, dst);
            }
        }

        //private static void InpaintFrames(string inputDir, string outputDir, int startX, int startY, int width, int height)
        //{
        //    if (!System.IO.Directory.Exists(outputDir))
        //        System.IO.Directory.CreateDirectory(outputDir);

        //    var files = System.IO.Directory.GetFiles(inputDir, "frame_*.png");


        //    foreach (var file in files)
        //    {
        //            Mat src = Cv2.ImRead(file, ImreadModes.Color);

        //            // Create a mask
        //            Mat mask = new Mat(src.Size(), MatType.CV_8UC1);
        //      //  Mat mask2 = new Mat(src.Size(), MatType.CV_8UC1);
        //        Cv2.Rectangle(mask, new Rect(startX, startY, width, height), Scalar.White, -1);

        //        //string outputFilePathMask = System.IO.Path.Combine("c:\\TestData", System.IO.Path.GetFileName(file));
        //        //Cv2.ImWrite(outputFilePathMask, mask);

        //        // Inpaint the image
        //        Mat dst = new Mat();
        //            //  Cv2.Inpaint(src, mask, dst, 5, InpaintMethod.Telea);
        //            Cv2.Inpaint(src, mask, dst, 3, InpaintMethod.NS);

        //            // Save the result
        //            string outputFilePath = System.IO.Path.Combine(outputDir, System.IO.Path.GetFileName(file));
        //            Cv2.ImWrite(outputFilePath, dst);
        //        }
        //}

        private void ReassembleVideo(string inputDir, string outputVideo, double frameRate, string pixelFormat)
        { 
            string ffmpegArgs = $"-framerate {frameRate} -i \"{inputDir}/frame_%08d.png\" -c:v libx264 -pix_fmt {pixelFormat} \"{outputVideo}\"";
            using (Process ffmpeg = new Process())
            {
                ffmpeg.StartInfo.FileName = $"\"{_ffmpegPath}\\ffmpeg\"";
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

        private double GetVideoFPS(string videoFilePath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(_ffmpegPath, "ffprobe"),
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

        private string GetVideoPixelFormat(string videoFilePath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(_ffmpegPath, "ffprobe"),
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
