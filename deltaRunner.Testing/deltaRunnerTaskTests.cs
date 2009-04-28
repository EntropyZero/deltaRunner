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
using EntropyZero.deltaRunner.Tasks;
using NUnit.Framework;

namespace EntropyZero.deltaRunner.Testing
{
	[TestFixture]
	public class deltaRunnerTaskTests : TestFixtureBase
	{
		public static string deltaPath = new DirectoryInfo("../../TestFiles/Delta1/").FullName;

		internal DeltaRunnerBase deltaRunner = null;

		[SetUp]
		public void SetUp()
		{
			DeltaRunnerFactory.CreateDeltaRunner("DeltaRunner", ConnectionStringMaster, false);
            deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(ConnectionString, deltaPath, true);
			deltaRunner.PrepareForDeltaRunner();
		}

		[Test]
		public void NAntTaskPrepare()
		{
			deltaRunner.RemoveDeltaRunner();
			Assert.IsFalse(deltaRunner.TableExists("dr_DeltaVersion"));

			deltaRunnerTask testTask = new deltaRunnerTask();
			testTask.ConnectionString = ConnectionString;
			testTask.Action = "PREPARE";
			testTask.DeltaPath = deltaPath;
			testTask.ExecuteTaskImpl();

			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));
		}

		[Test]
		public void NAntTaskRemove()
		{
			deltaRunner.RemoveDeltaRunner();
			Assert.IsFalse(deltaRunner.TableExists("dr_DeltaVersion"));

			deltaRunnerTask testTask = new deltaRunnerTask();
			testTask.ConnectionString = ConnectionString;
			testTask.Action = "PREPARE";
			testTask.DeltaPath = deltaPath;
			testTask.ExecuteTaskImpl();

			Assert.IsTrue(deltaRunner.TableExists("dr_DeltaVersion"));

			deltaRunnerTask testTask2 = new deltaRunnerTask();
			testTask2.ConnectionString = ConnectionString;
			testTask2.Action = "REMOVE";
			testTask2.DeltaPath = deltaPath;
			testTask2.ExecuteTaskImpl();

			Assert.IsFalse(deltaRunner.TableExists("dr_DeltaVersion"));
		}

		[Test]
		public void NAntTaskApply()
		{
			deltaRunner.RemoveDeltaRunner();
			Assert.IsFalse(deltaRunner.TableExists("dr_DeltaVersion"));

			deltaRunnerTask testTask = new deltaRunnerTask();
			testTask.ConnectionString = ConnectionString;
			testTask.Action = "PREPARE";
			testTask.DeltaPath = deltaPath;
			testTask.ExecuteTaskImpl();

			Assert.AreEqual("0", deltaRunner.GetLatestVersion());

			deltaRunnerTask testTask2 = new deltaRunnerTask();
			testTask2.ConnectionString = ConnectionString;
			testTask2.Action = "APPLY";
			testTask2.DeltaPath = deltaPath;
			testTask2.ExecuteTaskImpl();

			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());
		}
		
		[Test]
		public void NAntTaskApplyWithFilesBefore()
		{
			deltaRunner.RemoveDeltaRunner();
			Assert.IsFalse(deltaRunner.TableExists("dr_DeltaVersion"));
			if(TableExists("Table2_Static"))
			{
				ExecuteNonQuery("Drop Table [Table2_Static]");
			}

			deltaRunnerTask testTask = new deltaRunnerTask();
			testTask.ConnectionString = ConnectionString;
			testTask.Action = "PREPARE";
			testTask.DeltaPath = deltaPath;
			testTask.BeforeScriptFiles = new FileInfo("../../TestFiles/SampleStaticFile1.sql").FullName;
			testTask.ExecuteTaskImpl();

			Assert.AreEqual("0", deltaRunner.GetLatestVersion());

			deltaRunnerTask testTask2 = new deltaRunnerTask();
			testTask2.ConnectionString = ConnectionString;
			testTask2.Action = "APPLYWITHFILES";
			testTask2.DeltaPath = deltaPath;
			testTask2.BeforeScriptFiles = new FileInfo("../../TestFiles/SampleStaticFile1.sql").FullName;
			testTask2.ExecuteTaskImpl();

			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());
			Assert.AreEqual(true, TableExists("Table2_Static"));
		}

		[Test]
		public void NAntTaskApplyWithFilesAfter()
		{
			deltaRunner.RemoveDeltaRunner();
			Assert.IsFalse(deltaRunner.TableExists("dr_DeltaVersion"));
			if(TableExists("Table2_Static"))
			{
				ExecuteNonQuery("Drop Table [Table2_Static]");
			}

			deltaRunnerTask testTask = new deltaRunnerTask();
			testTask.ConnectionString = ConnectionString;
			testTask.Action = "PREPARE";
			testTask.DeltaPath = deltaPath;
			testTask.AfterScriptFiles = new FileInfo("../../TestFiles/SampleStaticFile1.sql").FullName;
			testTask.ExecuteTaskImpl();

			Assert.AreEqual("0", deltaRunner.GetLatestVersion());

			deltaRunnerTask testTask2 = new deltaRunnerTask();
			testTask2.ConnectionString = ConnectionString;
			testTask2.Action = "APPLYWITHFILES";
			testTask2.DeltaPath = deltaPath;
			testTask2.AfterScriptFiles = new FileInfo("../../TestFiles/SampleStaticFile1.sql").FullName;
			testTask2.ExecuteTaskImpl();

			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());
			Assert.AreEqual(true, TableExists("Table2_Static"));
		}
		
		[Test]
		public void NAntTaskApplyWithTwoFilesBefore()
		{
			deltaRunner.RemoveDeltaRunner();
			Assert.IsFalse(deltaRunner.TableExists("dr_DeltaVersion"));
			if(TableExists("Table2_Static"))
			{
				ExecuteNonQuery("Drop Table [Table2_Static]");
			}

			deltaRunnerTask testTask = new deltaRunnerTask();
			testTask.ConnectionString = ConnectionString;
			testTask.Action = "PREPARE";
			testTask.DeltaPath = deltaPath;
			testTask.BeforeScriptFiles =
				string.Format("{0},{1}", new FileInfo("../../TestFiles/SampleStaticFile2.sql").FullName,
				              new FileInfo("../../TestFiles/SampleStaticFile1.sql").FullName);
			testTask.ExecuteTaskImpl();

			Assert.AreEqual("0", deltaRunner.GetLatestVersion());

			deltaRunnerTask testTask2 = new deltaRunnerTask();
			testTask2.ConnectionString = ConnectionString;
			testTask2.Action = "APPLYWITHFILES";
			testTask2.DeltaPath = deltaPath;
			testTask2.BeforeScriptFiles =
				string.Format("{0},{1}", new FileInfo("../../TestFiles/SampleStaticFile2.sql").FullName,
				new FileInfo("../../TestFiles/SampleStaticFile1.sql").FullName);
			testTask2.ExecuteTaskImpl();

			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());
			Assert.AreEqual(true, TableExists("Table2_Static"));
		}

		[Test]
		public void NAntTaskApply_ForcesPrepare()
		{
			deltaRunner.RemoveDeltaRunner();
			Assert.IsFalse(deltaRunner.TableExists("dr_DeltaVersion"));

			deltaRunnerTask testTask = new deltaRunnerTask();
			testTask.ConnectionString = ConnectionString;
			testTask.Action = "APPLY";
			testTask.DeltaPath = deltaPath;
			testTask.ExecuteTaskImpl();

			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());

			deltaRunnerTask testTask2 = new deltaRunnerTask();
			testTask2.ConnectionString = ConnectionString;
			testTask2.Action = "APPLY";
			testTask2.DeltaPath = deltaPath;
			testTask2.ExecuteTaskImpl();

			Assert.AreEqual("00003", deltaRunner.GetLatestVersion());
		}
	}
}