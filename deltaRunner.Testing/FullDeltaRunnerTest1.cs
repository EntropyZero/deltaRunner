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
	public class FullDeltaRunnerTest1 : TestFixtureBase
	{
		public static new string connectionString = "user id=test;password=test;Initial Catalog=DeltaRunner_FullTest1;Data Source=(local);pooling=false;";
		public static string connectionStringMaster = "user id=test;password=test;Initial Catalog=master;Data Source=(local);";
		public static string deltaPath = new DirectoryInfo("../../TestFiles/FullTest1/Delta/").FullName;
		public static string databaseCreateFilename = new DirectoryInfo("../../TestFiles/FullTest1/CreateDatabase.sql").FullName;
		public static string databasePopulateFilename = new DirectoryInfo("../../TestFiles/FullTest1/PopulateDatabase.sql").FullName;
		public static string databaseRemoveFilename = new DirectoryInfo("../../TestFiles/FullTest1/RemoveDatabase.sql").FullName;

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
		public void Execute()
		{
			RunSqlScript(new StreamReader(databasePopulateFilename), connectionString);
			DeltaRunnerBase deltaRunner = null;
			deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true);
			deltaRunner.PrepareForDeltaRunner();
			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
			Assert.IsTrue(deltaRunner.TableExists("Book"));
			deltaRunner.ApplyDeltas();
			ExecuteScalar("SELECT TOP 1 YearPublished FROM Book", connectionString);
			ExecuteScalar("SELECT TOP 1 Hometown FROM Author", connectionString);
			Assert.AreEqual("00004", deltaRunner.GetLatestVersion());
		}

	}
}