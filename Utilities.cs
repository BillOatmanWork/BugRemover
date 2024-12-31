namespace BugRemover
{
    public static class Utilities
    {
        public static string LogName = "BugRemover";
        public static bool noLog;

        /// <summary>
        /// Delete any existing log file
        /// </summary>
        public static void CleanLog()
        {
            File.Delete($"{LogName}.log");
        }

        /// <summary>
        /// Write the supplied strong to the console and optionally to a log file
        /// </summary>
        /// <param name="text"></param>
        public static void ConsoleWithLog(string text)
        {
            Console.WriteLine(text);

            if (noLog == false)
            {
                using (StreamWriter file = File.AppendText($"{LogName}.log"))
                {
                    file.Write(text + Environment.NewLine);
                }
            }
        }
    }
}
