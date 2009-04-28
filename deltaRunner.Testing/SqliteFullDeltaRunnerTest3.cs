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
using System.Data;
using System.IO;
using Mono.Data.SqliteClient;
using NUnit.Framework;
using ScriptExecution=EntropyZero.deltaRunner.ScriptExecution;
using ScriptExecutionArgs=EntropyZero.deltaRunner.ScriptExecutionArgs;

namespace EntropyZero.deltaRunner.Testing
{
	[TestFixture]
	public class SqliteFullDeltaRunnerTest3 : SqliteTestFixtureBase
	{
        new public static string connectionString = "URI=file:DeltaRunner.db";
        public static string connectionStringMaster = "URI=file:DeltaRunner.db";
		public static string deltaPath = new DirectoryInfo("../../SqliteTestFiles/FullTest3/Delta/").FullName;
        public static string databaseCreateFilename = new DirectoryInfo("../../SqliteTestFiles/FullTest3/CreateDatabase.sql").FullName;
        public static string databasePopulateFilename = new DirectoryInfo("../../SqliteTestFiles/FullTest3/PopulateDatabase.sql").FullName;
        public static string databaseRemoveFilename = new DirectoryInfo("../../SqliteTestFiles/FullTest3/RemoveDatabase.sql").FullName;
        public static string staticFile1Filename = new DirectoryInfo("../../SqliteTestFiles/SampleStaticFile1.sql").FullName;
        public static string staticFile2Filename = new DirectoryInfo("../../SqliteTestFiles/SampleStaticFile2.sql").FullName;
        public static string staticFile3Filename = new DirectoryInfo("../../SqliteTestFiles/SampleStaticFile3.sql").FullName;

		[SetUp]
		public void SetUp()
		{
            DataTable tables = ExecuteDataTable("Select * from sqlite_master");
            foreach (DataRow row in tables.Rows)
            {
                if (row["tbl_name"].ToString() != "sqlite_sequence")
                    ExecuteNonQuery(string.Format("Drop Table IF EXISTS {0}", row["tbl_name"]));
            }
		}

        private DataTable ExecuteDataTable(string sql)
        {
            using (SqliteConnection conn = new SqliteConnection(connectionString))
            {
                sql = sql + " --dataProfilerIgnore";
                SqliteCommand cmd = new SqliteCommand(sql, conn);
                cmd.CommandTimeout = 1200;
                conn.Open();
                SqliteDataAdapter da = new SqliteDataAdapter(cmd);
                DataSet ds = new DataSet();
                da.Fill(ds);
                return ds.Tables[0];
            }
        }

		[TearDown]
		public void TearDown()
		{
			using (StreamWriter sw = new StreamWriter(Path.Combine(deltaPath, "00004.sql")))
			{
				sw.WriteLine("GO");
				sw.Close();
			}

			using (StreamWriter sw = new StreamWriter(Path.Combine(deltaPath, "00005.sql")))
			{
				sw.WriteLine("GO");
				sw.Close();
			}

			using (StreamWriter sw = new StreamWriter(staticFile2Filename))
			{
				sw.WriteLine("GO");
				sw.Close();
			}
		}

		[Test]
		public void Execute_CheckingForModifiedDeltas_WithChanges()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
			deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true, DeltaRunnerType.Sqlite);
			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			Assert.IsTrue(deltaRunner.TableExists("Book"));
			deltaRunner.ApplyDeltas();
			ExecuteScalar("SELECT YearPublished FROM Book", connectionString);
			ExecuteScalar("SELECT Hometown FROM Author", connectionString);
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			using (StreamWriter sw = new StreamWriter(Path.Combine(deltaPath, "00004.sql")))
			{
				sw.WriteLine("INSERT INTO Author ([Lastname], [Firstname]) VALUES('TestLastName', 'TestFirstName')");
				sw.WriteLine("GO");
				sw.Close();
			}

			Assert.AreEqual(0, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));

			deltaRunner.RunInDevelopmentMode = true;
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			Assert.AreEqual(2, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));
		}
		
		[Test]
		public void Execute_CheckingForModifiedDeltas_WithChanges_myTest()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true, DeltaRunnerType.Sqlite);
			deltaRunner.RunInDevelopmentMode = true;
			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			Assert.IsTrue(deltaRunner.TableExists("Book"));
			deltaRunner.ApplyDeltas();
			ExecuteScalar("SELECT YearPublished FROM Book", connectionString);
			ExecuteScalar("SELECT Hometown FROM Author", connectionString);
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			using (StreamWriter sw = new StreamWriter(Path.Combine(deltaPath, "00004.sql")))
			{
				sw.WriteLine("INSERT INTO Author ([Lastname], [Firstname]) VALUES('TestLastName', 'TestFirstName')");
				sw.WriteLine("GO");
				sw.Close();
			}

			Assert.AreEqual(0, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));

			deltaRunner = null;
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true, DeltaRunnerType.Sqlite);
			deltaRunner.RunInDevelopmentMode = true;
			deltaRunner.OnScriptExecution += new ScriptExecution(deltaRunner_OnScriptExecution);
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			Assert.AreEqual(2, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));
		}

		[Test]
		public void Execute_NOTCheckingForModifiedDeltas_WithChanges()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true, DeltaRunnerType.Sqlite);
			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			Assert.IsTrue(deltaRunner.TableExists("Book"));
		
			deltaRunner.ApplyDeltas();
			ExecuteScalar("SELECT YearPublished FROM Book", connectionString);
			ExecuteScalar("SELECT Hometown FROM Author", connectionString);
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			using (StreamWriter sw = new StreamWriter(Path.Combine(deltaPath, "00004.sql")))
			{
				sw.WriteLine("INSERT INTO Author ([Lastname], [Firstname]) VALUES('TestLastName', 'TestFirstName')");
				sw.WriteLine("GO");
				sw.Close();
			}

			Assert.AreEqual(0, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));

			deltaRunner.RunInDevelopmentMode = false;
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			Assert.AreEqual(0, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));
		}

		[Test]
		public void Execute_CheckingForModifiedDeltas_WithChanges_WithAdditionalSqlFiles()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true, DeltaRunnerType.Sqlite);

			deltaRunner.AddSqlFile(new FileInfo(staticFile1Filename), SqlFileExecutionOption.ExecuteAfterDeltas);

			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			Assert.IsTrue(deltaRunner.TableExists("Book"));
			deltaRunner.ApplyDeltas();
			ExecuteScalar("SELECT YearPublished FROM Book", connectionString);
			ExecuteScalar("SELECT Hometown FROM Author", connectionString);
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			using (StreamWriter sw = new StreamWriter(Path.Combine(deltaPath, "00004.sql")))
			{
				sw.WriteLine("INSERT INTO Author ([Lastname], [Firstname]) VALUES('TestLastName', 'TestFirstName')");
				sw.WriteLine("GO");
				sw.Close();
			}

			Assert.AreEqual(0, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));

			deltaRunner.RunInDevelopmentMode = true;
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			Assert.AreEqual(2, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));

			Assert.IsTrue(deltaRunner.TableExists("Table2_Static"));

            deltaRunner.OnScriptExecution += new ScriptExecution(deltaRunnerFailIfStaticFile_OnScriptExecution);
			deltaRunner.ApplyDeltas();
		}

		[Test]
		public void Execute_CheckingForModifiedDeltas_WithAdditionalSqlFiles_StaticFileChanges()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, false, DeltaRunnerType.Sqlite);
			deltaRunner.RunInDevelopmentMode = true;

			deltaRunner.AddSqlFile(new FileInfo(staticFile1Filename), SqlFileExecutionOption.ExecuteAfterDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile2Filename), SqlFileExecutionOption.ExecuteAfterDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile3Filename), SqlFileExecutionOption.ExecuteAfterDeltas);

			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			Assert.IsTrue(deltaRunner.TableExists("Book"));
			deltaRunner.ApplyDeltas();
			ExecuteScalar("SELECT YearPublished FROM Book", connectionString);
			ExecuteScalar("SELECT Hometown FROM Author", connectionString);
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			Assert.AreEqual(false, deltaRunner.TableExists("Table2_Static_Mod"));

			using (StreamWriter sw = new StreamWriter(staticFile2Filename))
			{
				sw.WriteLine("CREATE TABLE IF NOT EXISTS [Table2_Static_Mod]( [Table2_StaticId] int )");
				sw.WriteLine("GO");
				sw.Close();
			}
			
			// Second Run

            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, false, DeltaRunnerType.Sqlite);
			deltaRunner.RunInDevelopmentMode = true;

			deltaRunner.AddSqlFile(new FileInfo(staticFile1Filename), SqlFileExecutionOption.ExecuteAfterDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile2Filename), SqlFileExecutionOption.ExecuteAfterDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile3Filename), SqlFileExecutionOption.ExecuteAfterDeltas);

            deltaRunner.OnScriptExecution += new ScriptExecution(deltaRunner_OnScriptExecution);
            deltaRunner.OnAfterScriptExecution += new ScriptExecution(deltaRunner_OnAfterScriptExecution);
			
			deltaRunner.ApplyDeltas();
			
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			Assert.AreEqual(true, deltaRunner.TableExists("Table2_Static"));
			Assert.AreEqual(true, deltaRunner.TableExists("Table2_Static_Mod"));
		}

        private void deltaRunnerFailIfStaticFile_OnScriptExecution(ScriptExecutionArgs e)
		{
			if(e.Delta.File.FullName == staticFile1Filename)
			{
				Assert.Fail("Static file was improperly executed!");
			}
		}

        private void deltaRunner_OnScriptExecution(ScriptExecutionArgs e)
		{
			Console.Out.WriteLine("Script Name: {0}", e.Delta.Filename);
		}

        private void deltaRunner_OnAfterScriptExecution(ScriptExecutionArgs e)
		{
			Console.Out.WriteLine("Completed! [{1}] -- Script Name: {0}", e.Delta.Filename, e.Duration);
		}
		
		
		[Test]
		public void Execute_CheckingForModifiedDeltas_WithPreDeltaSqlFiles_StaticFileChanges()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true, DeltaRunnerType.Sqlite);
			deltaRunner.RunInDevelopmentMode = true;

			deltaRunner.AddSqlFile(new FileInfo(staticFile1Filename), SqlFileExecutionOption.ExecuteBeforeDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile2Filename), SqlFileExecutionOption.ExecuteBeforeDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile3Filename), SqlFileExecutionOption.ExecuteBeforeDeltas);

			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			Assert.IsTrue(deltaRunner.TableExists("Book"));
			deltaRunner.ApplyDeltas();
			ExecuteScalar("SELECT YearPublished FROM Book", connectionString);
			ExecuteScalar("SELECT Hometown FROM Author", connectionString);
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			Assert.AreEqual(false, deltaRunner.TableExists("Table2_Static_Mod"));

			using (StreamWriter sw = new StreamWriter(staticFile2Filename))
			{
				sw.WriteLine("CREATE TABLE IF NOT EXISTS [Table2_Static_Mod]( [Table2_StaticId] int )");
				sw.WriteLine("GO");
				sw.Close();
			}
			
			// Second Run

            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true, DeltaRunnerType.Sqlite);
			deltaRunner.RunInDevelopmentMode = true;

			deltaRunner.AddSqlFile(new FileInfo(staticFile1Filename), SqlFileExecutionOption.ExecuteBeforeDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile2Filename), SqlFileExecutionOption.ExecuteBeforeDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile3Filename), SqlFileExecutionOption.ExecuteBeforeDeltas);

            deltaRunner.OnScriptExecution += new ScriptExecution(deltaRunner_OnScriptExecution);
            deltaRunner.OnAfterScriptExecution += new ScriptExecution(deltaRunner_OnAfterScriptExecution);
			
			deltaRunner.ApplyDeltas();
			
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			Assert.AreEqual(true, deltaRunner.TableExists("Table2_Static"));
			Assert.AreEqual(false, deltaRunner.TableExists("Table2_Static_Mod"));
		}
		

		[Test]
		public void Execute_CheckingForModifiedDeltas_WithPreDeltaSqlFiles_StaticFileChanges_WithDropAndRecreate()
		{
			//RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true, connectionStringMaster, "DeltaRunner_FullTest3", true, DeltaRunnerType.Sqlite);
			deltaRunner.RunInDevelopmentMode = true;

			deltaRunner.AddSqlFile(new FileInfo(databasePopulateFilename), SqlFileExecutionOption.ExecuteBeforeDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile1Filename), SqlFileExecutionOption.ExecuteBeforeDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile2Filename), SqlFileExecutionOption.ExecuteBeforeDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile3Filename), SqlFileExecutionOption.ExecuteBeforeDeltas);

			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			deltaRunner.ApplyDeltas();
			Assert.IsTrue(deltaRunner.TableExists("Book"));
			ExecuteScalar("SELECT YearPublished FROM Book", connectionString);
			ExecuteScalar("SELECT Hometown FROM Author", connectionString);
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			Assert.AreEqual(false, deltaRunner.TableExists("Table2_Static_Mod"));

			using (StreamWriter sw = new StreamWriter(staticFile2Filename))
			{
				sw.WriteLine("CREATE TABLE IF NOT EXISTS [Table2_Static_Mod]( [Table2_StaticId] int )");
				sw.WriteLine("GO");
				sw.Close();
			}
			
			// Second Run

            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true, connectionStringMaster, "DeltaRunner_FullTest3", true, DeltaRunnerType.Sqlite);
			deltaRunner.RunInDevelopmentMode = true;

			deltaRunner.AddSqlFile(new FileInfo(databasePopulateFilename), SqlFileExecutionOption.ExecuteBeforeDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile1Filename), SqlFileExecutionOption.ExecuteBeforeDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile2Filename), SqlFileExecutionOption.ExecuteBeforeDeltas);
			deltaRunner.AddSqlFile(new FileInfo(staticFile3Filename), SqlFileExecutionOption.ExecuteBeforeDeltas);

            deltaRunner.OnScriptExecution += new ScriptExecution(deltaRunner_OnScriptExecution);
            deltaRunner.OnAfterScriptExecution += new ScriptExecution(deltaRunner_OnAfterScriptExecution);
			
			deltaRunner.ApplyDeltas();
			
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

			Assert.AreEqual(true, deltaRunner.TableExists("Table2_Static"));
			Assert.AreEqual(true, deltaRunner.TableExists("Table2_Static_Mod"));
		}
		
		[Test]
		public void CreateDatabase()
		{
            string connectionStringSampleDatabase = "URI=file:DeltaRunner2.db";

            DeltaRunnerBase.CreateDatabase("SampleDatabase", connectionStringMaster, true, DeltaRunnerType.Sqlite);
            DeltaRunnerBase.CreateDatabase("SampleDatabase", connectionStringMaster, DeltaRunnerType.Sqlite);
            DeltaRunnerBase.CreateDatabase("SampleDatabase", connectionStringMaster, true, DeltaRunnerType.Sqlite);
            DeltaRunnerBase.CreateDatabase("SampleDatabase", connectionStringMaster, DeltaRunnerType.Sqlite);

            DeltaRunnerBase deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionStringSampleDatabase, deltaPath, true, DeltaRunnerType.Sqlite);
			deltaRunner.RunInDevelopmentMode = true;
			deltaRunner.PrepareForDeltaRunner();
			deltaRunner.AddSqlFile(new FileInfo(databasePopulateFilename), SqlFileExecutionOption.ExecuteBeforeDeltas);
			deltaRunner.ApplyDeltas();
		}

		[Test]
		public void CreateDatabaseDoesNotDropIsAlreadyExists()
		{
            string connectionStringSampleDatabase = "URI=file:DeltaRunner2.db";

		    DeltaRunnerBase.CreateDatabase("SampleDatabase", connectionStringSampleDatabase, true, DeltaRunnerType.Sqlite);

            DeltaRunnerBase deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionStringSampleDatabase, deltaPath, true, DeltaRunnerType.Sqlite);
			deltaRunner.RunInDevelopmentMode = true;
			deltaRunner.PrepareForDeltaRunner();
			deltaRunner.AddSqlFile(new FileInfo(databasePopulateFilename), SqlFileExecutionOption.ExecuteBeforeDeltas, true);
			deltaRunner.ApplyDeltas();

            DeltaRunnerBase.CreateDatabase("SampleDatabase", connectionStringMaster, false, DeltaRunnerType.Sqlite);
			
			Assert.AreEqual(true, deltaRunner.TableExists("Book"));
			
		}
		
		[Test]
		public void CreateDatabase_WithoutDevMode()
		{
            string connectionStringSampleDatabase = "URI=file:DeltaRunner2.db";

            DeltaRunnerBase.CreateDatabase("SampleDatabase", connectionStringMaster, DeltaRunnerType.Sqlite);

            DeltaRunnerBase deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionStringSampleDatabase, deltaPath, true, DeltaRunnerType.Sqlite);
			deltaRunner.RunInDevelopmentMode = false;
			deltaRunner.PrepareForDeltaRunner();
			deltaRunner.AddSqlFile(new FileInfo(databasePopulateFilename), SqlFileExecutionOption.ExecuteBeforeDeltas);
			deltaRunner.ApplyDeltas();
		}
		
		[Test]
		public void DeltaRunOnlyOnceWhenNotInDevelopmentMode()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true, DeltaRunnerType.Sqlite);
			deltaRunner.PrepareForDeltaRunner();
			
			using (StreamWriter sw = new StreamWriter(Path.Combine(deltaPath, "00004.sql")))
			{
				sw.WriteLine("INSERT INTO Author ([Lastname], [Firstname]) VALUES('TestLastName', 'TestFirstName')");
				sw.WriteLine("GO");
				sw.Close();
			}
			
			deltaRunner.ApplyDeltas();

			Assert.AreEqual(1, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));
		}
		
		[Test]
		public void DeltaRunTwiceONLYInDevelopmentMode()
		{
			DeltaRunnerBase.DropDatabase("", connectionString, DeltaRunnerType.Sqlite);
            RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true, DeltaRunnerType.Sqlite);
			deltaRunner.RunInDevelopmentMode = true;
			deltaRunner.PrepareForDeltaRunner();
			
			using (StreamWriter sw = new StreamWriter(Path.Combine(deltaPath, "00004.sql")))
			{
				sw.WriteLine("INSERT INTO Author ([Lastname], [Firstname]) VALUES('TestLastName', 'TestFirstName')");
				sw.WriteLine("GO");
				sw.Close();
			}
			
			deltaRunner.ApplyDeltas();

			Assert.AreEqual(2, ExecuteScalar("SELECT Count(*) FROM Author WHERE [Lastname] = 'TestLastName'", connectionString));
		}
	}
}