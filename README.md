# BugRemover
BugRemover is a command line utility to remove bugs (logos) from video files. It is written in C# with executables available for multiple operating systems.
It uses [ffmpeg](https://ffmpeg.org) to make the magic happen.

## Installation
- Decompress the proper file for your operating system into a folder. 
- The ffmpeg executable must be available on your system.  It can be downloaded from [ffmpeg.org](https://ffmpeg.org/download.html)

## Parameters (Case Insensitive)
- -ffmpegpPath=Path to the ffmpeg executable.  Just the folder, the exe is assumed to be ffmpeg.exe or ffmpeg.
- -inFile=The video file the to have its bug removed.
- -startX=x Defines the upper left x-axis position of the square with the bug.
- -startY=y Defines the upper left y-axis position of the square with the bug.
- -width=w Defines the width of the square containing the bug.
- -height=h Defines the height of the square containing the bug.
- Optional: -extract=frameCount Extracts frameCount frames to assist in figuring out the bug position parameters. If this is specified, only the extraction happens.
  
## Note
BugRemover is CharityWare. If you like this program and find it of value, please consider making a donation to a local charity that benefits children such as Special Olympics. 
