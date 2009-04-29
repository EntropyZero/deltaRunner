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
using System.IO;
using NUnit.Framework;
using ScriptExecution=EntropyZero.deltaRunner.ScriptExecution;
using ScriptExecutionArgs=EntropyZero.deltaRunner.ScriptExecutionArgs;

namespace EntropyZero.deltaRunner.Testing
{
	[TestFixture]
	public class AlwaysRunPostDeltas : TestFixtureBase
	{
		new public static string connectionString = "user id=test;password=test;Initial Catalog=DeltaRunner_FullTest4;Data Source=(local);pooling=false;";
		public static string connectionStringMaster = "user id=test;password=test;Initial Catalog=master;Data Source=(local);";
		public static string deltaPath = new DirectoryInfo("../../TestFiles/FullTest4/Delta/").FullName;
		public static string databaseCreateFilename = new DirectoryInfo("../../TestFiles/FullTest4/CreateDatabase.sql").FullName;
		public static string databasePopulateFilename = new DirectoryInfo("../../TestFiles/FullTest4/PopulateDatabase.sql").FullName;
		public static string databaseRemoveFilename = new DirectoryInfo("../../TestFiles/FullTest4/RemoveDatabase.sql").FullName;
		public static string postDeltaFilename = new DirectoryInfo("../../TestFiles/FullTest4/PostDelta.sql").FullName;
		private static bool calledpostDelta;

		[SetUp]
		public void SetUp()
		{
			RunSqlScript(new StreamReader(databaseCreateFilename), connectionStringMaster);
			calledpostDelta = false;
		}

        [TearDown]
        public void TearDown()
        {
            using (StreamWriter sw = new StreamWriter(Path.Combine(deltaPath, "00004.sql")))
            {
                sw.Write(@"/*
                <deltarunner>
                    <MaySkipCategories><Category>Setup</Category></MaySkipCategories>
                </deltarunner>*/
                GO");
                sw.Close();
            }
            using (StreamWriter sw = new StreamWriter(postDeltaFilename))
            {
                sw.Write(@"Select @@Version
GO
");
                sw.Close();
            }
        }

		[Test]
		public void Execute_InDevelopmentMode_CheckingForPostDeltaExecution_WithChanges()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
			deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true);
            deltaRunner.AddSqlFile(new FileInfo(postDeltaFilename), SqlFileExecutionOption.ExecuteAfterDeltas, "Setup");
			deltaRunner.PrepareForDeltaRunner();
			deltaRunner.ApplyDeltas();
			deltaRunner.ShouldRunPostDeltasAllTheTime = true;

			using (StreamWriter sw = new StreamWriter(Path.Combine(deltaPath, "00004.sql")))
			{
				sw.Write(@"/*
                <deltarunner>
	                <MaySkipCategories><Category>Setup</Category></MaySkipCategories>
                </deltarunner>                
                */");
                sw.WriteLine("INSERT INTO Author ([Lastname], [Firstname]) VALUES('TestLastName', 'TestFirstName')");
				sw.WriteLine("GO");
				sw.Close();
			}
			deltaRunner.OnAfterScriptExecution += new ScriptExecution(deltaRunnerMustRunDelta_OnAfterScriptExecution);
			deltaRunner.RunInDevelopmentMode = true;
			deltaRunner.ApplyDeltas();

			Assert.AreEqual(true, calledpostDelta);
		}

		[Test]
		public void Execute_InDevelopmentMode_CheckingForPostDeltaExecution_WithOutChanges()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
			deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true);
            deltaRunner.AddSqlFile(new FileInfo(postDeltaFilename), SqlFileExecutionOption.ExecuteAfterDeltas, "Setup");
			deltaRunner.PrepareForDeltaRunner();
			deltaRunner.ApplyDeltas();
			deltaRunner.ShouldRunPostDeltasAllTheTime = true;
			deltaRunner.OnAfterScriptExecution += new ScriptExecution(deltaRunnerMustRunDelta_OnAfterScriptExecution);
			deltaRunner.RunInDevelopmentMode = true;
			deltaRunner.ApplyDeltas();

			Assert.AreEqual(true, calledpostDelta);
		}
		
		[Test]
		public void Execute_InNotDevelopmentMode_CheckingForPostDeltaExecution_WithChanges()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
			deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true);
            deltaRunner.AddSqlFile(new FileInfo(postDeltaFilename), SqlFileExecutionOption.ExecuteAfterDeltas, "Setup");
			deltaRunner.PrepareForDeltaRunner();
			deltaRunner.ApplyDeltas();
			deltaRunner.ShouldRunPostDeltasAllTheTime = true;

			using (StreamWriter sw = new StreamWriter(Path.Combine(deltaPath, "00004.sql")))
			{
				sw.Write(@"/*
                <deltarunner>
	                <MaySkipCategories><Category>Setup</Category></MaySkipCategories>
                </deltarunner>                
                */");
                sw.WriteLine("INSERT INTO Author ([Lastname], [Firstname]) VALUES('TestLastName', 'TestFirstName')");
				sw.WriteLine("GO");
				sw.Close();
			}
			deltaRunner.OnAfterScriptExecution += new ScriptExecution(deltaRunnerMustRunDelta_OnAfterScriptExecution);
			deltaRunner.RunInDevelopmentMode = false;
			deltaRunner.ApplyDeltas();

			Assert.AreEqual(true, calledpostDelta);
		}

		[Test]
		public void Execute_InNotDevelopmentMode_CheckingForPostDeltaExecution_WithOutChanges()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
			deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true);
            deltaRunner.AddSqlFile(new FileInfo(postDeltaFilename), SqlFileExecutionOption.ExecuteAfterDeltas, "Setup");
			deltaRunner.PrepareForDeltaRunner();
			deltaRunner.ApplyDeltas();
			deltaRunner.ShouldRunPostDeltasAllTheTime = true;
			deltaRunner.OnAfterScriptExecution += new ScriptExecution(deltaRunnerMustRunDelta_OnAfterScriptExecution);
			deltaRunner.RunInDevelopmentMode = false;
			deltaRunner.ApplyDeltas();

			Assert.AreEqual(true, calledpostDelta);
		}
		
		static void deltaRunnerMustRunDelta_OnAfterScriptExecution(ScriptExecutionArgs e)
        {
            if (e.Delta.File.FullName == new FileInfo(postDeltaFilename).FullName)
			{
				calledpostDelta = true;
			}
        }
	}
}