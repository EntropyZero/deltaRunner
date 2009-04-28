/******************************************************************************
EntropyZero Consulting deltaRunner
Copyright (C)2006 EntropyZero Consulting, LLC

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
******************************************************************************/

using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace EntropyZero.deltaRunner
{
	public class DeltaRunnerConsole
	{
		private StringBuilder results = new StringBuilder();

		public NameValueCollection arguments = new NameValueCollection();
		public string connectionString = string.Empty;
	    public string masterConnectionString = string.Empty;

		public string Results
		{
			get { return results.ToString(); }
		}

		public void Start(string[] args)
		{
			ConsoleWrite("deltaRunner provided by EntropyZero Consulting");

			if (args == null || args.Length < 2)
			{
				WriteUsage();
				return;
			}

			for (int i = 0; i < args.Length; i = i + 2)
			{
				arguments.Add(args[i].Replace("-", ""), args[i + 1]);
			}

			if (!CheckForRequiredArguments(arguments))
			{
				return;
			}

            if (arguments["u"] != null)
            {
                connectionString = string.Format(@"user id={0};password={1};Initial Catalog={2};Data Source={3};",
                                                 arguments["u"],
                                                 arguments["p"],
                                                 arguments["d"],
                                                 arguments["s"]);

                masterConnectionString =
                    string.Format(@"user id={0};password={1};Initial Catalog=master;Data Source={2};",
                                  arguments["u"],
                                  arguments["p"],
                                  arguments["s"]);
            }
            else
            {
                connectionString = string.Format(@"Initial Catalog={0};Data Source={1};Integrated Security=SSPI;",
                                                 arguments["d"],
                                                 arguments["s"]);

                masterConnectionString =
                    string.Format(@"Initial Catalog=master;Data Source={0};Integrated Security=SSPI;",
                                  arguments["u"]);
            }

		    string deltaPath = arguments["delta"];

			DeltaRunnerBase deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true);

			string command = arguments["c"].ToUpper();
			switch (command)
			{
				case "PREPARE":
					deltaRunner.PrepareForDeltaRunner();
					break;
				case "APPLY":
					deltaRunner.ApplyDeltas();
					break;
                case "APPLYWITHFILES":
                    if (arguments["pre"] != null && arguments["pre"].Length > 0)
                    {
                        string[] beforeFiles = arguments["pre"].Split(',');
                        foreach (string file in beforeFiles)
                        {
                            deltaRunner.AddSqlFile(new FileInfo(file), SqlFileExecutionOption.ExecuteBeforeDeltas);
                        }
                    }

                    if (arguments["post"] != null && arguments["post"].Length > 0)
                    {
                        string[] afterFiles = arguments["post"].Split(',');
                        foreach (string file in afterFiles)
                        {
                            deltaRunner.AddSqlFile(new FileInfo(file), SqlFileExecutionOption.ExecuteAfterDeltas);
                        }
                    }

                    deltaRunner.ApplyDeltas();
					break;
                case "APPLYWITHFILESNOTRANSACTION":
                    if (arguments["pre"] != null && arguments["pre"].Length > 0)
                    {
                        string[] beforeFiles = arguments["pre"].Split(',');
                        foreach (string file in beforeFiles)
                        {
                            deltaRunner.AddSqlFile(new FileInfo(file), SqlFileExecutionOption.ExecuteBeforeDeltas, false);
                        }
                    }

                    if (arguments["post"] != null && arguments["post"].Length > 0)
                    {
                        string[] afterFiles = arguments["post"].Split(',');
                        foreach (string file in afterFiles)
                        {
                            deltaRunner.AddSqlFile(new FileInfo(file), SqlFileExecutionOption.ExecuteAfterDeltas, false);
                        }
                    }

                    deltaRunner.ApplyDeltas();
					break;
				case "REMOVE":
					deltaRunner.RemoveDeltaRunner();
					break;
                case "CREATE":
			        DeltaRunnerBase.CreateDatabase(arguments["d"], masterConnectionString);
					break;
                case "DROP":
                    DeltaRunnerBase.DropDatabase(arguments["d"], masterConnectionString);
					break;
				case "FOO":
					//no nothing
					break;
				default:
					ConsoleWrite("Command '{0}' was not recognized", command);
					break;
			}
		}

		private void ConsoleWrite(string message, params object[] args)
		{
			results.AppendFormat(message, args);
			results.AppendFormat(Environment.NewLine);

			Console.Out.WriteLine(message, args);
		}

		private bool CheckForRequiredArguments(NameValueCollection myArgs)
		{
			if (
				myArgs == null ||
					myArgs["c"] == null ||
					myArgs["s"] == null ||
					myArgs["d"] == null
				)
			{
				ConsoleWrite("Missing required arguments.");
				WriteUsage();
				return false;
			}

            if (myArgs["u"] != null && myArgs["p"] == null)
            {
                ConsoleWrite("Missing required arguments.");
                WriteUsage();
                return false;
            }

            string command = arguments["c"].ToUpper();
            
            if(command == "APPLY" && myArgs["delta"] == null)
            {
                ConsoleWrite("Missing required arguments.");
                WriteUsage();
                return false;
            }
			return true;
		}

		private void WriteUsage()
		{
			string usageText = @"
Usage:

  DataFreshUtil.exe 
    -c            Command 
    -s            Server 
    -d            Database 
    -u            Username (if not supplied integrated auth will be used)
    -p            Password
    [options]

Options:
    -delta        specify path on server where delta files are located 
    -pre          Script files to run before deltas
    -post         Script files to run after deltas

Commands:

  PREPARE         prepare the database for deltaRunner
  APPLY           apply delta files to database
  APPLYWITHFILES  apply delta files to database including pre and post deltas
  
  APPLYWITHFILESNOTRANSACTION  
                  apply delta files to database including pre and post deltas
                  without using transactions on the pre and post deltas

  REMOVE          remove the DataFresh elements from the database
  CREATE          create the database if it does not already exist
  DROP            drop the database
";

			ConsoleWrite(usageText);
		}

		public static DeltaRunnerConsole Execute(string command, string username, string password, string server, string database, string deltaPath, params string[] options)
		{
			string[] args = new string[]
				{
					"-c", command,
					"-u", username,
					"-p", password,
					"-s", server,
					"-d", database,
					"-delta", deltaPath,
				};

			if (options != null && options.Length > 1)
			{
				string argsString = string.Join("|", args);
				string optionsString = string.Join("|", options);
				args = string.Format(argsString + "|" + optionsString).Split('|');
			}

			DeltaRunnerConsole console = new DeltaRunnerConsole();
			console.Start(args);
			return console;
		}
	}
}