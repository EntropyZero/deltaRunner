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

using System.IO;
using NUnit.Framework;

namespace EntropyZero.deltaRunner.Testing
{
	[TestFixture]
	public class deltaRunnerConsoleTester : TestFixtureBase
	{
        public static string deltaPath = new DirectoryInfo("../../TestFiles/Delta1/").FullName;
		
		internal DeltaRunnerBase deltaRunner = null;

		[SetUp]
		public void SetUp()
		{
            DeltaRunnerBase.CreateDatabase("DeltaRunner", ConnectionStringMaster, false);
			deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(ConnectionString, deltaPath, true);
			deltaRunner.AddSqlFile(new FileInfo("../../TestFiles/Database.sql"), SqlFileExecutionOption.ExecuteBeforeDeltas, false);
			deltaRunner.PrepareForDeltaRunner();
		}

		[Test]
		public void FOOCommand()
		{
			DeltaRunnerConsole.Execute("FOO", "test", "test", "(local)", "DeltaRunner", @"..\..\TestFiles\Delta1");
		}

		[Test]
		public void BadCommand()
		{
			DeltaRunnerConsole console = DeltaRunnerConsole.Execute("BADCOMMAND", "test", "test", "(local)", "DeltaRunner", @"..\..\TestFiles\Delta1");
			Assert.IsTrue(console.Results.IndexOf("Command 'BADCOMMAND' was not recognized") > -1);
		}

		[Test]
		public void CheckUsage()
		{
			DeltaRunnerConsole console = new DeltaRunnerConsole();
			console.Start(new string[]{""});
			Assert.IsTrue(console.Results.IndexOf(@"deltaRunner provided by EntropyZero Consulting") > -1);
			Assert.IsTrue(console.Results.IndexOf(@"Usage:") > -1);
		}

		[Test]
		public void DontProvideDeltaPath()
		{
			DeltaRunnerConsole console = new DeltaRunnerConsole();
		    console.Start(new string[] {"-c", "APPLY", "-s", "(local)", "-d", "deltarunner", "-u", "test", "-p", "test"});
			Assert.IsTrue(console.Results.IndexOf(@"deltaRunner provided by EntropyZero Consulting") > -1);
			Assert.IsTrue(console.Results.IndexOf(@"Usage:") > -1);
		}

		[Test]
		public void PrepareCommand()
		{
			DeltaRunnerConsole.Execute("REMOVE", "test", "test", "(local)", "DeltaRunner", @"..\..\TestFiles\Delta1");
			Assert.IsFalse(TableExists("dr_DeltaVersion"));
			DeltaRunnerConsole.Execute("PREPARE", "test", "test", "(local)", "DeltaRunner", @"..\..\TestFiles\Delta1");
			Assert.IsTrue(TableExists("dr_DeltaVersion"));
		}

		[Test]
		public void RemoveCommand()
		{
			DeltaRunnerConsole.Execute("PREPARE", "test", "test", "(local)", "DeltaRunner", @"..\..\TestFiles\Delta1");
			Assert.IsTrue(TableExists("dr_DeltaVersion"));
			DeltaRunnerConsole.Execute("REMOVE", "test", "test", "(local)", "DeltaRunner", @"..\..\TestFiles\Delta1");
			Assert.IsFalse(TableExists("dr_DeltaVersion"));
		}

        [Test]
        public void CreateDatabase()
        {
            DeltaRunnerConsole console = new DeltaRunnerConsole();
            console.Start(new string[] { "-c", "CREATE", "-s", "(local)", "-d", "Fred", "-u", "test", "-p", "test" });
            Assert.IsTrue(console.Results.IndexOf(@"deltaRunner provided by EntropyZero Consulting") > -1);
            Assert.IsTrue(console.Results.IndexOf(@"Usage:") == -1);
        }

        [Test]
        public void DropDatabase()
        {
            DeltaRunnerConsole console = new DeltaRunnerConsole();
            console.Start(new string[] { "-c", "DROP", "-s", "(local)", "-d", "Fred", "-u", "test", "-p", "test" });
            Assert.IsTrue(console.Results.IndexOf(@"deltaRunner provided by EntropyZero Consulting") > -1);
            Assert.IsTrue(console.Results.IndexOf(@"Usage:") == -1);
        }

        [Test]
        public void UpdateDatabase_IncludeStaticSqlFiles()
        {
            if (TableExists("Table2_Static"))
                ExecuteNonQuery("Drop Table Table2_Static");
            Assert.AreEqual(false, TableExists("Table2_Static"));
            
            DeltaRunnerBase deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(ConnectionString, deltaPath, true);
            deltaRunner.RemoveDeltaRunner();
            deltaRunner.PrepareForDeltaRunner();
            Assert.AreEqual("0", deltaRunner.GetLatestVersion());

            string postDeltas = Path.Combine(deltaPath, "..\\SampleStaticFile1.sql");
            postDeltas += ",";
            postDeltas += Path.Combine(deltaPath, "..\\SampleStaticFile2.sql");
            postDeltas += ",";
            postDeltas += Path.Combine(deltaPath, "..\\SampleStaticFile3.sql");
            
            
            DeltaRunnerConsole console = new DeltaRunnerConsole();
            console.Start(new string[] { "-c", "APPLYWITHFILES", "-s", "(local)", "-d", "DeltaRunner", "-u", "test", "-p", "test", "-delta", deltaPath, "-post", postDeltas});

            Assert.AreEqual("00003", deltaRunner.GetLatestVersion());
            Assert.AreEqual(true, TableExists("Table2_Static"));
        }
	}
}