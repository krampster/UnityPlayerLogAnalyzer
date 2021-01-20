using System;

namespace UnityPlayerLogToYaml
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Please provide a path to a unity log file as the first parameter.");
                System.Environment.Exit(-1);
            }

            MainProcessor main = new MainProcessor();
            Console.WriteLine("Converting {0}", args[0]);
            main.Convert(args[0]);
        }
    }
}
