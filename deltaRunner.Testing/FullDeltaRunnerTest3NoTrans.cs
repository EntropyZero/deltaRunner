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
	public class FullDeltaRunnerTest3NoTrans : TestFixtureBase
	{
		new public static string connectionString = "user id=test;password=test;Initial Catalog=DeltaRunner_FullTest3;Data Source=(local);pooling=false;";
		public static string connectionStringMaster = "user id=test;password=test;Initial Catalog=master;Data Source=(local);";
		public static string deltaPath = new DirectoryInfo("../../TestFiles/FullTest3NoTrans/Delta/").FullName;
		public static string databaseCreateFilename = new DirectoryInfo("../../TestFiles/FullTest3NoTrans/CreateDatabase.sql").FullName;
		public static string databasePopulateFilename = new DirectoryInfo("../../TestFiles/FullTest3NoTrans/PopulateDatabase.sql").FullName;
		public static string databaseRemoveFilename = new DirectoryInfo("../../TestFiles/FullTest3NoTrans/RemoveDatabase.sql").FullName;
		public static string roleFilename = new DirectoryInfo("../../TestFiles/FullTest3NoTrans/addRole.sql").FullName;

		[SetUp]
		public void SetUp()
		{
			RunSqlScript(new StreamReader(databaseCreateFilename), connectionStringMaster);
		}

		[Test]
		public void Execute_CheckingForModifiedDeltas_WithChanges_WithAdditionalSqlFiles()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
			deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true);

			deltaRunner.AddSqlFile(new FileInfo(roleFilename), SqlFileExecutionOption.ExecuteAfterDeltas, false);

			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			Assert.IsTrue(deltaRunner.TableExists("Book"));
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

		}

		[Test]
		public void Execute_CheckingForModifiedDeltas_WithChanges_WithAdditionalSqlFiles_NoTrans()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = null;
			deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true);

			deltaRunner.AddSqlFile(new FileInfo(roleFilename), SqlFileExecutionOption.ExecuteAfterDeltas, false);
            		    deltaRunner.UseTransactions = false;
			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			Assert.IsTrue(deltaRunner.TableExists("Book"));
			deltaRunner.ApplyDeltas();
			Assert.AreEqual("00007", deltaRunner.GetLatestVersion());

		}

	}
}