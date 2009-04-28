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
using NAnt.Core;
using NAnt.Core.Attributes;

namespace EntropyZero.deltaRunner.Tasks
{
	[TaskName("deltaRunner")]
	public class deltaRunnerTask : Task
	{
		private string connectionString;
		private string deltaPath;
		private string beforeScriptFiles;
		private string afterScriptFiles;
		private string action;

		[TaskAttribute("connectionString", Required = true)]
		[StringValidator(AllowEmpty = false)]
		public string ConnectionString
		{
			get { return connectionString; }
			set { connectionString = value; }
		}

		[TaskAttribute("deltaPath", Required = true)]
		[StringValidator(AllowEmpty = false)]
		public string DeltaPath
		{
			get { return deltaPath; }
			set { deltaPath = value; }
		}
		
		[TaskAttribute("beforeScriptFiles")]
		public string BeforeScriptFiles
		{
			get { return beforeScriptFiles; }
			set { beforeScriptFiles = value; }
		}	
		
		[TaskAttribute("afterScriptFiles")]
		public string AfterScriptFiles
		{
			get { return afterScriptFiles; }
			set { afterScriptFiles = value; }
		}

		[TaskAttribute("action", Required = true)]
		[StringValidator(AllowEmpty = false)]
		public string Action
		{
			get { return action; }
			set { action = value; }
		}

		protected override void ExecuteTask()
		{
            DeltaRunnerBase deltaRunner;

			deltaRunner = DeltaRunnerFactory.CreateDeltaRunner(connectionString, deltaPath, true);

			switch (action.ToUpper())
			{
				case "PREPARE":
					deltaRunner.PrepareForDeltaRunner();
					break;
				case "REMOVE":
					deltaRunner.RemoveDeltaRunner();
					break;
				case "APPLY":
					deltaRunner.PrepareForDeltaRunner();
					deltaRunner.ApplyDeltas();
					break;
				case "APPLYWITHFILES":
					deltaRunner.PrepareForDeltaRunner();
					if(BeforeScriptFiles != null && BeforeScriptFiles.Length > 0)
					{
						string[] beforeFiles = BeforeScriptFiles.Split(',');
						foreach (string file in beforeFiles)
						{
							deltaRunner.AddSqlFile(new FileInfo(file), SqlFileExecutionOption.ExecuteBeforeDeltas);
						}
					}
					
					if(AfterScriptFiles != null && AfterScriptFiles.Length > 0)
					{
						string[] afterFiles = AfterScriptFiles.Split(',');
						foreach (string file in afterFiles)
						{
							deltaRunner.AddSqlFile(new FileInfo(file), SqlFileExecutionOption.ExecuteAfterDeltas);
						}
					}
					deltaRunner.ApplyDeltas();
					break;
			}
		}

		public void ExecuteTaskImpl()
		{
			ExecuteTask();
		}
	}
}