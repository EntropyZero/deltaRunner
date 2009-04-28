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
	public class DeltaHashProviderTester : TestFixtureBase
	{
		[Test]
		public void GetMD5Hash_EnsureHashIsConstant()
		{
			FileInfo tempFile = new FileInfo(Path.GetTempFileName());
			StreamWriter sw = new StreamWriter(tempFile.OpenWrite());
			sw.WriteLine("test test");
			sw.Close();

			string hashFirstRun = DeltaHashProvider.GetMD5Hash(tempFile);
			Assert.AreEqual(hashFirstRun, DeltaHashProvider.GetMD5Hash(tempFile));
		}

		[Test]
		public void GetMD5Hash_EnsureSmallTextChangeModifiesHash()
		{
			FileInfo tempFile = new FileInfo(Path.GetTempFileName());
			StreamWriter sw = new StreamWriter(tempFile.OpenWrite());
			sw.WriteLine("test tes");
			sw.Close();

			string hashFirstRun = DeltaHashProvider.GetMD5Hash(tempFile);
			Assert.AreEqual(hashFirstRun, DeltaHashProvider.GetMD5Hash(tempFile));
		}

		[Test]
		public void AddDeltaHash()
		{
			DeltaHashProvider hash = new DeltaHashProvider(DeltaRunnerFactory.CreateDeltaRunner(null, null));

			hash.AddDeltaHash("1", "hash001");
			hash.AddDeltaHash("2", "hash002");
			hash.AddDeltaHash("3", "hash003");
			hash.AddDeltaHash("4", "hash004");
			hash.AddDeltaHash("5", "hash005");

			Assert.AreEqual(true, hash.HashCodes.ContainsKey("1"));
			Assert.AreEqual(true, hash.HashCodes.ContainsKey("2"));
			Assert.AreEqual(true, hash.HashCodes.ContainsKey("3"));
			Assert.AreEqual(true, hash.HashCodes.ContainsKey("4"));
			Assert.AreEqual(true, hash.HashCodes.ContainsKey("5"));
		}

		[Test]
		public void CheckDeltaHash()
		{
            DeltaHashProvider hash = new DeltaHashProvider(DeltaRunnerFactory.CreateDeltaRunner(null, null));

			hash.AddDeltaHash("1", "hash001");
			hash.AddDeltaHash("2", "hash002");
			hash.AddDeltaHash("3", "hash003");
			hash.AddDeltaHash("4", "hash004");
			hash.AddDeltaHash("5", "hash005");

			Assert.AreEqual(true, hash.CheckDeltaHash("1", "hash001"));
			Assert.AreEqual(true, hash.CheckDeltaHash("2", "hash002"));
			Assert.AreEqual(true, hash.CheckDeltaHash("3", "hash003"));
			Assert.AreEqual(true, hash.CheckDeltaHash("4", "hash004"));
			Assert.AreEqual(true, hash.CheckDeltaHash("5", "hash005"));
		}

		[Test]
		public void GetMD5Hash_CheckForMOdifications()
		{
			FileInfo tempFile001 = new FileInfo(Path.Combine(Path.GetTempPath(), "001.sql"));
			StreamWriter sw = new StreamWriter(tempFile001.OpenWrite());
			sw.WriteLine("test test 111");
			sw.Close();

			FileInfo tempFile002 = new FileInfo(Path.Combine(Path.GetTempPath(), "002.sql"));
			StreamWriter sw2 = new StreamWriter(tempFile002.OpenWrite());
			sw2.WriteLine("test test 222");
			sw2.Close();

            DeltaHashProvider hashProvider = new DeltaHashProvider(DeltaRunnerFactory.CreateDeltaRunner(null, null));
			hashProvider.AddDeltaHash(tempFile001.Name, DeltaHashProvider.GetMD5Hash(tempFile001));
			hashProvider.AddDeltaHash(tempFile002.Name, DeltaHashProvider.GetMD5Hash(tempFile002));

			Assert.AreEqual(null, hashProvider.CheckForModifications(new FileInfo[]{tempFile001, tempFile002}));
		}

		[Test]
		public void GetMD5Hash_CheckForModifications_TimeTracker()
		{
			FileInfo[] files = new FileInfo[3];

            DeltaHashProvider hashProvider = new DeltaHashProvider(DeltaRunnerFactory.CreateDeltaRunner(null, null));

			int i = 0;
			foreach (FileInfo file in new DirectoryInfo("../../TestFiles/FullTest3_TimeTracker/").GetFiles())
			{
				files[i++] = file;
				string hashCode = DeltaHashProvider.GetMD5Hash(file);
				Console.Out.WriteLine("hashCode = {0}", hashCode);
				hashProvider.AddDeltaHash(file.Name, hashCode);
			}
			
			Assert.AreEqual(null, hashProvider.CheckForModifications(files));
		}
		
		[Test]
		public void GetMD5Hash_CheckForMOdifications_WithModificationsToSecondFile()
		{
			FileInfo tempFile001 = new FileInfo(Path.Combine(Path.GetTempPath(), "001.sql"));
			using(StreamWriter sw = new StreamWriter(tempFile001.OpenWrite()))
			{
				sw.WriteLine("test test 111");
				sw.Close();
			}

			FileInfo tempFile002 = new FileInfo(Path.Combine(Path.GetTempPath(), "002.sql"));
			using(StreamWriter sw2 = new StreamWriter(tempFile002.OpenWrite()))
			{
				sw2.WriteLine("test test 222");
				sw2.Close();
			}

            DeltaHashProvider hashProvider = new DeltaHashProvider(DeltaRunnerFactory.CreateDeltaRunner(null, null));
			hashProvider.AddDeltaHash(tempFile001.Name, DeltaHashProvider.GetMD5Hash(tempFile001));
			hashProvider.AddDeltaHash(tempFile002.Name, DeltaHashProvider.GetMD5Hash(tempFile002));

			using(StreamWriter sw3 = new StreamWriter(tempFile002.Open(FileMode.OpenOrCreate, FileAccess.Write)))
			{
				sw3.WriteLine("test test changes to 222");
				sw3.Close();
			}

			Assert.AreEqual(tempFile002, hashProvider.CheckForModifications(new FileInfo[]{tempFile001, tempFile002}));
		}

		[Test]
		public void GetMD5Hash_CheckForMOdifications_WithModificationsToRandomFile()
		{
            DeltaHashProvider hashProvider = new DeltaHashProvider(DeltaRunnerFactory.CreateDeltaRunner(null, null));

			FileInfo[] files = new FileInfo[100];

			Console.Out.WriteLine("Creating [{0}] Temp Deltas", files.Length);

			for (int i = 0; i < files.Length; i++)
			{
				FileInfo tempFile = new FileInfo(Path.Combine(Path.GetTempPath(), string.Format("00{0}.sql", i)));
				using(StreamWriter sw = new StreamWriter(tempFile.OpenWrite()))
				{
					sw.WriteLine("test test {0}", i);
					sw.Close();
					hashProvider.AddDeltaHash(tempFile.Name, DeltaHashProvider.GetMD5Hash(tempFile));
					files[i] = tempFile;
				}
			}

			Random r = new Random(DateTime.Now.Millisecond);
			int randomFileIndex = r.Next(1,files.Length-1);
			FileInfo randomTempFile = files[randomFileIndex];

			Console.Out.WriteLine("Random Temp File = {0} : {1}", randomFileIndex, randomTempFile.FullName);

			using(StreamWriter sw2 = new StreamWriter(randomTempFile.Open(FileMode.OpenOrCreate, FileAccess.Write)))
			{
				Console.Out.WriteLine("Modifying Delta");
				sw2.WriteLine("changes to this delta!!");
				sw2.Close();
			}

			Assert.AreEqual(randomTempFile, hashProvider.CheckForModifications(files));
		}

		[Test]
		public void EnsureHashDoesntIncludeBadChars()
		{
            DeltaHashProvider hashProvider = new DeltaHashProvider(DeltaRunnerFactory.CreateDeltaRunner(null, null));

			FileInfo[] files = new FileInfo[100];

			for (int i = 0; i < files.Length; i++)
			{
				FileInfo tempFile = new FileInfo(Path.Combine(Path.GetTempPath(), string.Format("00{0}.sql", i)));
				using(StreamWriter sw = new StreamWriter(tempFile.OpenWrite()))
				{
					sw.WriteLine("test test {0}", i);
					sw.Close();

					string hash = DeltaHashProvider.GetMD5Hash(tempFile);

					string fileContents = hashProvider.GetFileContext(tempFile);
					Assert.AreEqual(-1, hash.IndexOf('\r'), string.Format("Contents: {0}", fileContents));
					Assert.AreEqual(-1, hash.IndexOf('|'), string.Format("Contents: {0}", fileContents));
				}
			}
		}
	}
}