﻿using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text.RegularExpressions;

namespace Postal.Test.Client
{
    class Program
    {
        const string Usage =
@"You can type any of the following commands into the prompt below.
get <name>[,<name>,<name>...]⏎ will get one or more values stored against that name
set <name>:<value>[,<name>:<value>,<name>:<value>,...]⏎ will store or overwrite one or more stored values";

        static readonly Regex _commandRegex = new Regex(@"(?<command>get|set)\s+((?<names>[^:^,]+)\s*((\:\s*(?<values>[^:^,]+))?(,\s*)?)+)+", RegexOptions.Compiled | RegexOptions.Singleline);

        enum Command
        {
            get,
            set,
            exit
        }

        static void Main(string[] args)
        {
            var exit = false;
            using (var clientPipe = new NamedPipeClientStream(PipeDetails.Name))
            {
                try
                {
                    clientPipe.Connect();
                }
                catch (Exception)
                {
                    Console.WriteLine("Could not connect to server pipe, please start the server and then try again");
                    return;
                }

                while (!exit)
                {
                    Console.WriteLine(Usage);
                    Console.Write(">");
                    var input = Console.ReadLine();
                    var match = _commandRegex.Match(input);
                    if (match.Success)
                    {
                        var command = (Command)Enum.Parse(typeof(Command), match.Groups["command"].Value);
                        var names = (from Capture capture in match.Groups["names"].Captures select capture.Value).ToArray();
                        var values = (from Capture capture in match.Groups["values"].Captures select capture.Value).ToArray();

                        switch (command)
                        {
                            case Command.get:
                                {
                                    var response = Messages.GetStrings.Send(clientPipe, names);
                                    if (response.Result)
                                        Console.WriteLine("Values for keys: {0} are: {1}", string.Join(", ", names), string.Join(", ", response.Values));
                                    else
                                        Console.WriteLine("There was an error fetching values for one or more keys: {0}", response.Message);
                                }
                                break;

                            case Command.set:
                                {
                                    var response = Messages.SetStrings.Send(clientPipe, names, values);
                                    if (response.Result)
                                        Console.WriteLine("Successfully set values for keys: {0}", string.Join(", ", names));
                                    else
                                        Console.WriteLine("There was an error setting values for keys: {0}, error was: {1}",
                                            string.Join(", ", names),
                                            response.Message);
                                }
                                break;
                        }
                    }
                }
            }
        }
    }
}
