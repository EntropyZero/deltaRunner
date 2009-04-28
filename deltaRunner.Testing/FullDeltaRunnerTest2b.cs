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
	public class FullDeltaRunnerTest2b : TestFixtureBase
	{
		new public static string connectionString = "user id=test;password=test;Initial Catalog=DeltaRunner_FullTest2;Data Source=(local);pooling=false;";
		public static string connectionStringMaster = "user id=test;password=test;Initial Catalog=master;Data Source=(local);";
		public static string deltaPath = new DirectoryInfo("../../TestFiles/FullTest2/Delta2/").FullName;
		public static string databaseCreateFilename = new DirectoryInfo("../../TestFiles/FullTest2/CreateDatabase.sql").FullName;
		public static string databasePopulateFilename = new DirectoryInfo("../../TestFiles/FullTest2/PopulateDatabase.sql").FullName;
		public static string databaseRemoveFilename = new DirectoryInfo("../../TestFiles/FullTest2/RemoveDatabase.sql").FullName;

		[SetUp]
		public void SetUp()
		{
			RunSqlScript(new StreamReader(databaseCreateFilename), connectionStringMaster);
		}
		
		[TearDown]
		public void TearDown()
		{
			RunSqlScript(new StreamReader(databaseRemoveFilename), connectionStringMaster);
		}

		[Test]
		public void ApplyDeltasToExistingDatabase_WithRollback()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
            DeltaRunnerBase deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true);
			deltaRunner.PrepareForDeltaRunner();
			ExecuteNonQuery("insert into dr_deltaversion ([latestdelta], [hash]) VALUES ('00001', 'sample hash')", connectionString);
			Assert.AreEqual("00001", deltaRunner.GetLatestVersion());
			deltaRunner.ApplyDeltas(true);
			Assert.AreEqual("00001", deltaRunner.GetLatestVersion());
		}
	}
}