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
using System.Collections;
using System.Data;
using System.IO;
using System.Security.Cryptography;

namespace EntropyZero.deltaRunner
{
	public class DeltaHashProvider
	{
		public Hashtable HashCodes = new Hashtable();
		public static MD5 MD5Crypto = null;
	    private DeltaRunnerBase deltaRunner;

        public DeltaHashProvider(DeltaRunnerBase deltaRunner)
        {
            this.deltaRunner = deltaRunner;
        }

        public static string GetMD5Hash(FileInfo fileInfo)
		{
			if(MD5Crypto == null)
			{
				MD5Crypto = MD5.Create();
			}
			using (FileStream fs = fileInfo.OpenRead())
			{
				byte[] output = MD5Crypto.ComputeHash(fs);
				string hashString = Convert.ToBase64String(output);
				return hashString;
			}
		}

		public void CacheDeltaRunnerHashCodes(DataRow[] deltaRows)
		{
			foreach (DataRow deltaRow in deltaRows)
			{
				AddDeltaHash(deltaRow[2].ToString(), deltaRow[1].ToString());
			}
		}

		public void AddDeltaHash(string filename, string hashCode)
		{
			if (!HashCodes.ContainsKey(filename))
			{
				HashCodes.Add(filename, hashCode);
			}
		}
		
		public bool IsNewDelta(object delta)
		{
			return(!HashCodes.ContainsKey(delta));
		}

		public bool CheckDeltaHash(object delta, string hashCode)
		{
			if (IsNewDelta(delta))
			{
                deltaRunner.ConsoleWrite("New Delta Detected : {0}", delta.ToString());
				return false;
			}
			string knownHash = HashCodes[delta].ToString();
			bool hashCodesMatch = knownHash == hashCode;
			if(!hashCodesMatch && deltaRunner.RunInDevelopmentMode)
			{
                deltaRunner.ConsoleWrite("Modified Delta Detected : {0}", delta.ToString());
			}
			return hashCodesMatch;
		}

		public FileInfo CheckForModifications(FileInfo[] files)
		{
			foreach (FileInfo fileInfo in files)
			{
				string hashText = GetMD5Hash(fileInfo);
				if (!CheckDeltaHash(fileInfo.Name, hashText))
				{
					return fileInfo;
				}
			}
			return null;
		}

		public string GetFileContext(FileInfo info)
		{
			using (FileStream deltaStream = info.OpenRead())
			{
				StreamReader deltaReader = new StreamReader(deltaStream);
				return deltaReader.ReadToEnd();
			}
		}
	}
}