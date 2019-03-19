using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LogWatcher
{
    public enum LogLevel
    {
        Debug,
        Trace,
        Info,
        Verbose
    }

    /// <summary>
    /// A different watcher class came first and a rolex is a better watch so this one stayed.
    /// </summary>
    class Rolex : IDisposable
    {
        public readonly string computerName;
        public readonly char driveLetter;

        private StreamReader _sr;
        private AutoResetEvent _wh;
        private FileSystemWatcher _fsw;
        private FileStream _fs;
        private Task<string> _readLineAsync;
        
        // important because we combine multiline logs
        private string _logLine;
        
        // End Of Steam -- for now...
        private bool _eos;

        private string _path;

        public Rolex(string computerName, char driveLetter)
        {
            this.computerName = computerName;
            this.driveLetter = driveLetter;
        }

        public void Init(LogLevel level)
        {
            // Use the file share if possible
            _path = "\\\\" + computerName + "\\nysadatastore\\logs\\" + level + ".log";
            if (!File.Exists(_path))
            {
                Console.WriteLine("No nysadatastore folder found: " +_path);
                _path = "\\\\" + computerName + "\\" + driveLetter + "$\\nysadatastore\\logs\\" + level + ".log";
            }


            Console.WriteLine("Using UNC path: " + _path);

                

            if (!File.Exists(_path))
            {
                Console.Error.WriteLine("Couldn't find nysadatastore logs on computer: " + computerName + ", drive: " +
                                        driveLetter);
                _eos = true;
                return;
            }

            _wh = new AutoResetEvent(false);
            _fsw = new FileSystemWatcher(".");
            _fsw.Filter = "file-to-read";
            _fsw.EnableRaisingEvents = true;
            _fsw.Changed += (s, e) => _wh.Set();
            _fs = new FileStream(_path,
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _sr = new StreamReader(_fs);
            _readLineAsync = _sr.ReadLineAsync();
        }

        public void Dispose()
        {
            if (_sr != null) _sr.Dispose();
            if (_wh != null) _wh.Dispose();
            if (_fsw != null) _fsw.Dispose();
            if (_fs != null) _fs.Dispose();
        }

        public static void ConfluentStreams(Rolex[] watches, DateTime from, int maxHistorical, LogLevel level)
        {
            foreach (Rolex rolex in watches)
            {
                rolex.Init(level);
            }

            // read to the end and print in sorted order
            foreach (Log log in ReapLogs(watches, from).OrderBy(log => log.time).Tail(maxHistorical))
            {
                Console.WriteLine(log.logLine);
            }

            // This is only sorted in that it prints as soon as new logs becomes available
            StreamLogs(watches, from);
        }

        /// <summary>
        /// This moves the stream reader to the end of all the logs
        /// This code is duplicated from StreamLogs. Be sure to update in both locations
        /// </summary>
        private static IEnumerable<Log> ReapLogs(Rolex[] watches, DateTime from)
        {
            while (!watches.All(r => r._eos))
            {
                foreach (Rolex r in watches.Where(r => r._sr != null))
                {
                    if (r._readLineAsync.IsCompleted)
                    {
                        r._eos = false;
                        var line = r._readLineAsync.Result;
                        r._readLineAsync = r._sr.ReadLineAsync();


                        if /*end of current stream*/ (line == null)
                        {
                            r._eos = true;

                            if (r._logLine != null)
                            {
                                // release the previous line
                                var log = new Log(r._logLine);
                                //Console.WriteLine(log.logLine);
                                if (log.time > @from)
                                {
                                    log.logLine = r.computerName + " " + log.logLine;
                                    yield return log;
                                }

                                // clear
                                r._logLine = null;
                            }
                        }
                        else if /*beginning*/ (Regex.IsMatch(line, @"^\d{4}-\d\d-\d\d"))
                        {
                            if (r._logLine != null)
                            {
                                // release the previous line
                                var log = new Log(r._logLine);
                                //Console.WriteLine(log.logLine);
                                if (log.time > @from)
                                {
                                    log.logLine = r.computerName + " " + log.logLine;
                                    yield return log;
                                }
                            }

                            // hold just in case there's more
                            r._logLine = line;
                        }
                        else if /*most likely a multi-line log*/ (r._logLine != null)
                        {
                            r._logLine += "\n" + line;
                        }
                        else /*an unexpected line in the logs*/
                        {
                            r._logLine = line;
                        }
                    }
                }
            }
        }

        private static void StreamLogs(Rolex[] watches, DateTime from)
        {
            while (true)
            {
                foreach (Rolex r in watches.Where(r => r._sr != null))
                {
                    if (r._readLineAsync.IsCompleted)
                    {
                        r._eos = false;
                        var line = r._readLineAsync.Result;
                        r._readLineAsync = r._sr.ReadLineAsync();

                        if (line == null)
                        {
                            r._eos = true;

                            if (r._logLine != null)
                            {
                                // release the previous line
                                var log = new Log(r._logLine);
                                //Console.WriteLine(log.logLine);
                                if (log.time > @from)
                                    Console.WriteLine(r.computerName + " " + log.logLine);

                                // we already grabbed the next line so save it
                                r._logLine = null;
                            }
                        }
                        else if (Regex.IsMatch(line, @"^\d{4}-\d\d-\d\d"))
                        {
                            if (r._logLine != null)
                            {
                                // release the previous line
                                var log = new Log(r._logLine);
                                //Console.WriteLine(log.logLine);
                                if (log.time > @from)
                                    Console.WriteLine(r.computerName + " " + log.logLine);
                            }

                            r._logLine = line;
                        }
                        else
                        {
                            r._logLine += "\n" + line;
                        }
                    }
                }
                if (watches.All(r => r._eos))
                    Thread.Sleep(100);
            }
        }
    }
}
