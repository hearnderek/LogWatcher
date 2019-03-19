using System;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;

namespace LogWatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 0 && args[0] != "/?" && args[0] != "help" && args[0] != "--help")
            {

                var OnlyLogsAfter = DateTime.Now.AddHours(-1);

                LogLevel level;
                if (!(args.Length > 1 && LogLevel.TryParse(args[1], out level)))
                    level = LogLevel.Debug;
                //var 

                var watches = args[0].Split(',')
                    .Select(cla => // build our log file watchers
                    {
                        string[] split = cla.Split(':');
                        switch (split.Length)
                        {
                            case 1:
                                return new Rolex(split[0], 'c');
                            case 2:
                                return new Rolex(split[0], split[1][0]);
                            default:
                                throw new ArgumentException("Are you missing a comma?\nUnexpected extra ':' in string " + cla);
                        }
                    }).ToArray().ToArray();


                Rolex.ConfluentStreams(watches, OnlyLogsAfter, 2000, level);



            }
            else // Show Help msg
            {
                Console.WriteLine(
                    "Help for LogWatcher.exe --\n"+
                    "\n" +
                    "Streams the logs\n" +
                    "\n" +
                    "Example Usage:\n"+
                    "LogWatcher.exe <EngineMachine0>:<DriveLetter>,<EngineMachine1>:<DriveLetter>,<EngineMachineN>:DriveLetter>\n"+
                    "LogWatcher.exe chic-hdge-qadb:c,chic-jenk-db1:d");
            }
        }
    }
}
