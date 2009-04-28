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
	public class SkippingPostDeltas : TestFixtureBase
	{
		new public static string connectionString = "user id=test;password=test;Initial Catalog=DeltaRunner_FullTest4;Data Source=(local);pooling=false;";
		public static string connectionStringMaster = "user id=test;password=test;Initial Catalog=master;Data Source=(local);";
		public static string deltaPath = new DirectoryInfo("../../TestFiles/FullTest4/Delta/").FullName;
		public static string databaseCreateFilename = new DirectoryInfo("../../TestFiles/FullTest4/CreateDatabase.sql").FullName;
		public static string databasePopulateFilename = new DirectoryInfo("../../TestFiles/FullTest4/PopulateDatabase.sql").FullName;
		public static string databaseRemoveFilename = new DirectoryInfo("../../TestFiles/FullTest4/RemoveDatabase.sql").FullName;
		public static string postDeltaFilename = new DirectoryInfo("../../TestFiles/FullTest4/PostDelta.sql").FullName;

		[SetUp]
		public void SetUp()
		{
			RunSqlScript(new StreamReader(databaseCreateFilename), connectionStringMaster);
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
		public void Execute_CheckingForModifiedDeltas_WithChanges()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
			deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true);
            deltaRunner.AddSqlFile(new FileInfo(postDeltaFilename), SqlFileExecutionOption.ExecuteAfterDeltas, "Setup");
			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			Assert.IsTrue(deltaRunner.TableExists("Book"));
			deltaRunner.ApplyDeltas();
			ExecuteScalar("SELECT TOP 1 YearPublished FROM Book", connectionString);
			ExecuteScalar("SELECT TOP 1 Hometown FROM Author", connectionString);
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

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
            deltaRunner.OnAfterScriptExecution += new ScriptExecution(deltaRunner_OnAfterScriptExecution);

			Assert.AreEqual(0, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));

			deltaRunner.RunInDevelopmentMode = true;
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			Assert.AreEqual(2, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));
		}

		[Test]
		public void Execute_CheckingForModifiedDeltas_WithChanges_InBoth()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
			deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true);
            deltaRunner.AddSqlFile(new FileInfo(postDeltaFilename), SqlFileExecutionOption.ExecuteAfterDeltas, "Setup");
			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			Assert.IsTrue(deltaRunner.TableExists("Book"));
			deltaRunner.ApplyDeltas();
			ExecuteScalar("SELECT TOP 1 YearPublished FROM Book", connectionString);
			ExecuteScalar("SELECT TOP 1 Hometown FROM Author", connectionString);
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

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

            using (StreamWriter sw = new StreamWriter(postDeltaFilename))
			{
                sw.WriteLine("INSERT INTO Author ([Lastname], [Firstname]) VALUES('TestLastName', 'TestFirstName')");
				sw.WriteLine("GO");
                sw.WriteLine("INSERT INTO Author ([Lastname], [Firstname]) VALUES('TestLastName', 'TestFirstName')");
				sw.WriteLine("GO");
                sw.WriteLine("INSERT INTO Author ([Lastname], [Firstname]) VALUES('TestLastName', 'TestFirstName')");
				sw.WriteLine("GO");
				sw.Close();
			}
			Assert.AreEqual(0, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));

			deltaRunner.RunInDevelopmentMode = true;
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			Assert.AreEqual(8, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));
		}

        static void deltaRunner_OnAfterScriptExecution(ScriptExecutionArgs e)
        {
            if (e.Delta.File.FullName == new FileInfo(postDeltaFilename).FullName)
			{
				Assert.Fail("Post Delta file was improperly executed!");
			}
        }
	}
}