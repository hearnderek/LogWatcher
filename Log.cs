using System;

namespace LogWatcher
{
    public class Log
    {
        public string logLine;
        public DateTime time;

        public Log(string logLine)
        {
            this.logLine = logLine;
            string datetime = logLine.Substring(0, logLine.IndexOf(",")).Trim();
            if (!DateTime.TryParse(datetime, out time))
                Console.WriteLine(datetime);
        }   
    }
}
