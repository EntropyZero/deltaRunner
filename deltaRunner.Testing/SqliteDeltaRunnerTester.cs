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

namespace EntropyZero.deltaRunner.Testing
{
	[TestFixture]
	public class SqliteDeltaRunnerTester : SqliteTestFixtureBase
	{
        public static string ConnectionStringSqlite = "URI=file:DeltaRunner.db";

		public static string deltaPath = new DirectoryInfo("../../SqliteTestFiles/Delta1/").FullName;
		public static string deltaPath2 = new DirectoryInfo("../../SqliteTestFiles/Delta2_BadDelta/").FullName;

		internal DeltaRunnerBase deltaRunner = null;

		[SetUp]
		public void SetUp()
		{
            DeltaRunnerBase.CreateDatabase("DeltaRunner", ConnectionStringMaster, false, DeltaRunnerType.Sqlite);
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(ConnectionStringSqlite, deltaPath, true, DeltaRunnerType.Sqlite) as SqliteDeltaRunner;
			deltaRunner.AddSqlFile(new FileInfo("../../SqliteTestFiles/SqliteDatabase.sql"), SqlFileExecutionOption.ExecuteBeforeDeltas, false);
			deltaRunner.PrepareForDeltaRunner();
		}

	    [Test]
	    public void TestFactory()
	    {
	        SqliteDeltaRunner dr = DeltaRunnerFactory.CreateDeltaRunner(ConnectionString, deltaPath, true, ConnectionStringMaster, "Sample", true, DeltaRunnerType.Sqlite) as SqliteDeltaRunner;
            Assert.AreEqual(typeof(SqliteDeltaRunner), dr.GetType());
	    }

		[Test]
		public void PrepareForDeltaRunner()
		{
            Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			deltaRunner.RemoveDeltaRunner();
			Assert.IsFalse(deltaRunner.TableExists("dr_DeltaVersion"));
			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
		}

		[Test]
		public void RemoveDeltaRunner()
		{
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			deltaRunner.RemoveDeltaRunner();
			Assert.IsFalse(deltaRunner.TableExists("dr_DeltaVersion"));
			deltaRunner.RemoveDeltaRunner();
			Assert.IsFalse(deltaRunner.TableExists("dr_DeltaVersion"));
		}

		[Test]
		public void UpdateDatabase()
		{
			deltaRunner.RemoveDeltaRunner();
			deltaRunner.PrepareForDeltaRunner();
			Assert.AreEqual("0", deltaRunner.GetLatestVersion());
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());
		}

		[Test]
		[Ignore("Sqlite does not allow the dropping of a column in an alter table"), ExpectedException(typeof(System.Exception), "Required column 'Hash' was not found in table 'dr_DeltaVersion'")]
		public void EnsureRequiredColumnsExist_Hash()
		{
			deltaRunner.RemoveDeltaRunner();
			deltaRunner.PrepareForDeltaRunner();
			Assert.AreEqual("0", deltaRunner.GetLatestVersion());
			ExecuteNonQuery("ALTER TABLE dr_DeltaVersion DROP COLUMN Hash");
			deltaRunner.ApplyDeltas();
		}

		[Test]
        [Ignore("Sqlite does not allow the dropping of a column in an alter table"), ExpectedException(typeof(System.Exception), "Required column 'LatestDelta' was not found in table 'dr_DeltaVersion'")]
		public void EnsureRequiredColumnsExist_LatestDelta()
		{
			deltaRunner.RemoveDeltaRunner();
			deltaRunner.PrepareForDeltaRunner();
			Assert.AreEqual("0", deltaRunner.GetLatestVersion());
			ExecuteNonQuery("ALTER TABLE dr_DeltaVersion DROP COLUMN LatestDelta");
			deltaRunner.ApplyDeltas();
		}

		[Test]
		public void UpdateDatabase_TableStoresHashCodes()
		{
			deltaRunner.RemoveDeltaRunner();
			deltaRunner.PrepareForDeltaRunner();
			Assert.AreEqual("0", deltaRunner.GetLatestVersion());
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());
		    object poo = ExecuteScalar("SELECT COUNT(*) FROM dr_DeltaVersion");
			Assert.AreEqual(4, ExecuteScalar("SELECT COUNT(*) FROM dr_DeltaVersion"));
			Assert.IsTrue(DBNull.Value != ExecuteScalar("SELECT [Hash] FROM dr_DeltaVersion WHERE LatestDelta = 1"));
			Assert.IsTrue(DBNull.Value != ExecuteScalar("SELECT [Hash] FROM dr_DeltaVersion WHERE LatestDelta = 2"));
			Assert.IsTrue(DBNull.Value != ExecuteScalar("SELECT [Hash] FROM dr_DeltaVersion WHERE LatestDelta = 3"));
		}

		[Test]
		public void BadDeltaRollBackTransaction()
		{
			deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(ConnectionString, deltaPath2, false, DeltaRunnerType.Sqlite);
			deltaRunner.RemoveDeltaRunner();
			deltaRunner.PrepareForDeltaRunner();
			Assert.AreEqual("0", deltaRunner.GetLatestVersion());
			deltaRunner.ApplyDeltas(true);
			Assert.AreEqual("0", deltaRunner.GetLatestVersion());
		}

		[Test]
		public void ExecuteOnlyNewerDeltas()
		{
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(ConnectionString, deltaPath2, true, DeltaRunnerType.Sqlite);
			deltaRunner.RemoveDeltaRunner();
			deltaRunner.PrepareForDeltaRunner();
			ExecuteNonQuery("insert into dr_deltaversion ([latestdelta], [hash], [filename]) VALUES ('00001', 'xstD0qrfGt7um9h/rqKnLQ==', '00001.sql')");
			ExecuteNonQuery("insert into dr_deltaversion ([latestdelta], [hash], [filename]) VALUES ('00002', '6eOnIXx84dCOru/ArcGkog==', '00002.sql')");
			Assert.AreEqual("00002", deltaRunner.GetLatestVersion());
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());
		}

		[Test]
		public void ApplyDeltasReturnBooleanResult()
		{
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(ConnectionString, deltaPath2, true, DeltaRunnerType.Sqlite);
			deltaRunner.RemoveDeltaRunner();
			deltaRunner.PrepareForDeltaRunner();
			ExecuteNonQuery("insert into dr_deltaversion ([latestdelta], [hash], [filename]) VALUES ('00001', 'xstD0qrfGt7um9h/rqKnLQ==', '00001.sql')");
			ExecuteNonQuery("insert into dr_deltaversion ([latestdelta], [hash], [filename]) VALUES ('00002', '6eOnIXx84dCOru/ArcGkog==', '00002.sql')");
			Assert.AreEqual("00002", deltaRunner.GetLatestVersion());
			Assert.IsTrue(deltaRunner.ApplyDeltas());
			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());
			Assert.IsFalse(deltaRunner.ApplyDeltas());
			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());
		}

		[Test, ExpectedException(typeof (DirectoryNotFoundException), "Delta folder was not found.")]
		public void CheckForBadFolder()
		{
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(ConnectionString, new DirectoryInfo(@"c:\temp\asoidasoidaoisdjaoisjdaoisjdasd").FullName, true, DeltaRunnerType.Sqlite);
			deltaRunner.ApplyDeltas();
		}

		[Test]
		public void LatestVersionIsZeroInNewDB()
		{
			deltaRunner.RemoveDeltaRunner();
			deltaRunner.PrepareForDeltaRunner();
			Assert.AreEqual("0", deltaRunner.GetLatestVersion());
		}

		[Test]
		public void OverrideDeltaTableColumn()
		{
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(ConnectionString, deltaPath, false, DeltaRunnerType.Sqlite);
			deltaRunner.DeltaVersionTableName = "Config";
			deltaRunner.DeltaVersionColumnName = "Delta";
			deltaRunner.RemoveDeltaRunner();
			Assert.IsFalse(deltaRunner.TableExists("Config"));
			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("Config"));
			deltaRunner.ApplyDeltas();
		}

		[Test]
		public void ApplyDeltaWhenColumnNull()
		{
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(ConnectionString, deltaPath, false, DeltaRunnerType.Sqlite);
			deltaRunner.RemoveDeltaRunner();
			deltaRunner.PrepareForDeltaRunner();
			ExecuteNonQuery(string.Format("update {0} set {1} = null", deltaRunner.DeltaVersionTableName, deltaRunner.DeltaVersionColumnName));
			Assert.AreEqual("0", deltaRunner.GetLatestVersion());
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());
		}

		[Test]
		public void UpdateDatabase_IncludeStaticSqlFiles()
		{
			deltaRunner.RemoveDeltaRunner();
			deltaRunner.PrepareForDeltaRunner();
			Assert.AreEqual("0", deltaRunner.GetLatestVersion());

			deltaRunner.AddSqlFile(new FileInfo(Path.Combine(deltaPath, "..\\SampleStaticFile1.sql")), SqlFileExecutionOption.ExecuteAfterDeltas);
			deltaRunner.AddSqlFile(new FileInfo(Path.Combine(deltaPath, "..\\SampleStaticFile2.sql")), SqlFileExecutionOption.ExecuteAfterDeltas);
			deltaRunner.AddSqlFile(new FileInfo(Path.Combine(deltaPath, "..\\SampleStaticFile3.sql")), SqlFileExecutionOption.ExecuteAfterDeltas);
			
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());

			Assert.AreEqual(true, TableExists("Table2_Static"));
		}

		[Test, Ignore("the sqlite version does not update itself")]
		public void deltaRunnerUpdatesItself()
		{
			string oldVersionSql = @"
						CREATE TABLE if not exists [dr_DeltaVersion]
						(
							[LatestDelta] int
						)";
			
			deltaRunner.RemoveDeltaRunner();
			ExecuteNonQuery(oldVersionSql);
			
			Assert.AreEqual(false, deltaRunner.ColumnExists("dr_DeltaVersion", "Hash"));
			Assert.AreEqual(false, deltaRunner.ColumnExists("dr_DeltaVersion", "Filename"));
			
			deltaRunner.PrepareForDeltaRunner();
			
			Assert.AreEqual(true, deltaRunner.ColumnExists("dr_DeltaVersion", "Hash"));
			Assert.AreEqual(true, deltaRunner.ColumnExists("dr_DeltaVersion", "Filename"));
		}

        [Test, Ignore("the sqlite version does not update itself")]
		public void deltaRunnerUpdatesItself_OnlyExecuteNewerDeltas()
		{
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(ConnectionString, deltaPath2, true, DeltaRunnerType.Sqlite);
			
			string oldVersionSql = @"
					IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id(N'[dbo].[dr_DeltaVersion]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)
						CREATE TABLE [dbo].[dr_DeltaVersion]
						(
							[LatestDelta] int
							, [Hash] varchar(25)
						)";
			
			deltaRunner.RemoveDeltaRunner();
			
			ExecuteNonQuery(oldVersionSql);
			
			ExecuteNonQuery("INSERT INTO dr_DeltaVersion (LatestDelta, Hash) VALUES(1, 'blahblah')");
			ExecuteNonQuery("INSERT INTO dr_DeltaVersion (LatestDelta, Hash) VALUES(2, 'blahblah')");
			
			deltaRunner.PrepareForDeltaRunner();
			
			Assert.AreEqual("00002", deltaRunner.GetLatestVersion());
			
			deltaRunner.ApplyDeltas();
			
			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());
			
			deltaRunner.ApplyDeltas();
			deltaRunner.ApplyDeltas();
		}
		
		[Test]
		public void UpdateDatabase_IncludeStaticSqlFiles_PreDeltaFiles()
		{
			deltaRunner.RemoveDeltaRunner();
			deltaRunner.PrepareForDeltaRunner();
			Assert.AreEqual("0", deltaRunner.GetLatestVersion());

			deltaRunner.AddSqlFile(new FileInfo(Path.Combine(deltaPath, "..\\SampleStaticFile1.sql")), SqlFileExecutionOption.ExecuteBeforeDeltas);
			
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());

			Assert.AreEqual(true, TableExists("Table2_Static"));
		}
		
		[Test]
		public void UpdateDatabase_IncludeStaticSqlFiles_PreDeltaFiles_WithChangesToPreScript()
		{
			deltaRunner.RemoveDeltaRunner();
			deltaRunner.PrepareForDeltaRunner();
			Assert.AreEqual("0", deltaRunner.GetLatestVersion());

			deltaRunner.AddSqlFile(new FileInfo(Path.Combine(deltaPath, "..\\SampleStaticFile1.sql")), SqlFileExecutionOption.ExecuteBeforeDeltas);
			
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());

			Assert.AreEqual(true, TableExists("Table2_Static"));
		}

	    [Test]
        public void SortByFilename()
	    {
            FileInfo file1 = new FileInfo("c:\\temp\\001.sql");
            FileInfo file2 = new FileInfo("c:\\temp\\020.sql");
            FileInfo file3 = new FileInfo("c:\\temp\\002.sql");
            FileInfo[] files = new FileInfo[3]{file1, file2, file3};

	        FileInfo[] sortedFiles = deltaRunner.SortByFilename(files);
	     
            Assert.AreEqual("001.sql", sortedFiles[0].Name);
            Assert.AreEqual("002.sql", sortedFiles[1].Name);
            Assert.AreEqual("020.sql", sortedFiles[2].Name);
	    }
	}
}