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
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text;

namespace EntropyZero.deltaRunner
{
    public abstract class DeltaRunnerBase
    {
    	#region Events

        public event ScriptExecution OnScriptExecution;
        public event ScriptExecution OnAfterScriptExecution;
        public event ScriptExecution OnSkipScriptExecution;

        protected abstract IDbConnection CreateConnection();

        #endregion

        #region Protected Member Variables

        protected string connectionString = null;
        protected string masterConnectionString = null;
        protected bool dropDbOnPreDeltaChange = false;
        protected string dbName = string.Empty;
        protected string deltaPath = null;
        protected bool verbose = false;
        protected DeltaRunnerType type = DeltaRunnerType.SqlServer;
        protected bool runInDevelopmentMode = false;
        protected bool useTransactions = true;
		private int commandTimeout = 12000;

        #endregion

        #region Private Member Variables

        private Queue sqlFilePreDeltaQueue = new Queue();
        private Queue sqlFilePostDeltaQueue = new Queue();

        #endregion

        #region Constructors

        public DeltaRunnerBase(string connectionString, string deltaPath)
        {
            this.connectionString = connectionString;
            this.deltaPath = deltaPath;
        }

        public DeltaRunnerBase(string connectionString, string deltaPath, bool verbose)
            : this(connectionString, deltaPath)
        {
            this.verbose = verbose;
        }

		public DeltaRunnerBase(string connectionString, string deltaPath, bool verbose, bool shouldRunPostDeltasAllTheTime)
            : this(connectionString, deltaPath, verbose)
		{
			ShouldRunPostDeltasAllTheTime = shouldRunPostDeltasAllTheTime;
		}

    	public DeltaRunnerBase(string connectionString, string deltaPath, bool dropDbOnPreDeltaChange,
                               string masterConnectionString, string dbName, bool verbose)
            : this(connectionString, deltaPath, verbose)
        {
            this.dropDbOnPreDeltaChange = dropDbOnPreDeltaChange;
            this.masterConnectionString = masterConnectionString;
            this.dbName = dbName;
        }

        #endregion

        #region Public Abstract Properties

        public abstract string PrepareScriptResourceName { get; }
        public abstract string RemoveScriptResourceName { get; }
        public abstract string DeltaVersionTableName { get; set; }
        public abstract string DeltaVersionColumnName { get; set; }
        public abstract string DeltaFilenameColumnName { get; set; }
        public abstract string DeltaVersionHashColumnName { get; set; }

        #endregion

        #region Public Properties

		public bool ShouldRunPostDeltasAllTheTime { get; set; }

		public int CommandTimeout
		{
			get { return commandTimeout; }
			set { commandTimeout = value; }
		}
		
		public bool RunInDevelopmentMode
        {
            get { return runInDevelopmentMode; }
            set { runInDevelopmentMode = value; }
        }

        public bool UseTransactions
        {
            get { return useTransactions; }
            set { useTransactions = value; }
        }

        #endregion

        #region Public Abstract Methods

        public abstract string GetLatestVersion();

        public abstract string GetDbName();
        public abstract bool TableExists(string tableName);
        public abstract bool ColumnExists(string tableName, string columnName);

        #endregion

        #region Public Methods

        public void PrepareForDeltaRunner()
        {
            DateTime before = DateTime.Now;
            ConsoleWrite("PrepareForDeltaRunner Started");
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(PrepareScriptResourceName))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    string cmd = sr.ReadToEnd();
                    RunSqlScript(string.Format(cmd, DeltaVersionTableName, DeltaVersionColumnName));
                    ConsoleWrite("PrepareForDeltaRunner Complete : " + (DateTime.Now - before));
                }
            }
        }

        public void RemoveDeltaRunner()
        {
            DateTime before = DateTime.Now;
            ConsoleWrite("RemoveDeltaRunner Started");
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(RemoveScriptResourceName))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    string cmd = sr.ReadToEnd();
                    RunSqlScript(string.Format(cmd, DeltaVersionTableName, DeltaVersionColumnName));
                    ConsoleWrite("RemoveDeltaRunner Complete : " + (DateTime.Now - before));
                }
            }
        }


        public void AddSqlFile(FileInfo file, SqlFileExecutionOption option)
        {
            AddSqlFile(file, option, true, null);
        }

        public void AddSqlFile(FileInfo file, SqlFileExecutionOption option, string category)
        {
            AddSqlFile(file, option, true, category);
        }

        public void AddSqlFile(FileInfo file, SqlFileExecutionOption option, bool useTransaction)
        {
            AddSqlFile(file, option, useTransaction, null);
        }

        public void AddSqlFile(FileInfo file, SqlFileExecutionOption option, bool useTransaction, string category)
        {
            DeltaFile deltaFile = new DeltaFile(file, option, useTransaction, category);
            switch (option)
            {
                case SqlFileExecutionOption.ExecuteAfterDeltas:
                    sqlFilePostDeltaQueue.Enqueue(deltaFile);
                    break;
                case SqlFileExecutionOption.ExecuteBeforeDeltas:
                    sqlFilePreDeltaQueue.Enqueue(deltaFile);
                    break;
            }
        }

        public bool ApplyDeltas()
        {
            return ApplyDeltas(false);
        }

        public bool ApplyDeltas(bool ignoreExceptions)
        {
            RunningState runningState = new RunningState(deltaPath);

            DoesDeltaFolderExist(runningState);
            CheckThatdeltaRunnerTablesAreSetUp();
            InitalizeDBName();

            runningState.HashProvider = new DeltaHashProvider(this);
            DataTable deltaTable = GetDeltaTable();
            runningState.HashProvider.CacheDeltaRunnerHashCodes(deltaTable.Select());

            using (IDbConnection conn = CreateConnection())
            {
                conn.Open();
                IDbTransaction tran = null;
                if (UseTransactions)
                {
                    tran = conn.BeginTransaction(IsolationLevel.Serializable);
                }

                try
                {
                    LoadFilesIntoListOrderedCorrectlyAndCheckIfModified(runningState); 
                    runningState.LatestVersion = GetLatestVersion();
                    DetermineWhichDeltasToRunAndQueueThem(runningState);
                    if(runningState.ShouldDropAndRecreate)
                    {
                        DropAndRecreate();
                        return (ApplyDeltas(ignoreExceptions));
                    }

                    DeletePostDeltasThatNeedToBeReRun(runningState, conn, tran);
                    IfInDevelopmentModeDeleteVersionsThatNeedToBeReRun(runningState, conn, tran);
                    RunTheQueuedDeltas(runningState, conn, ref tran);
                    
                    if (runningState.SchemaModified)
                        ConsoleWrite("Deltas have been applied.");

                    if (tran != null)
                    {
                        tran.Commit();
                    }

                }
                catch (SqlException ex)
                {
                    Console.Out.WriteLine("SqlException Occured	-->	{0}", ex);
                    if (tran != null)
                    {
                        tran.Rollback();
                    }
                    if (!ignoreExceptions)
                    {
                        throw;
                    }

                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine("deltaRunner Exception Occured --> {0}", ex);
                    if (tran != null)
                    {
                        tran.Rollback();
                    }
                    if (!ignoreExceptions)
                    {
                        throw;
                    }
                }
                finally
                {
                    if (conn.State != ConnectionState.Closed)
                    {
                        conn.Close();
                    }
                }
            }
            return runningState.SchemaModified;
        }

        private static void DoesDeltaFolderExist(RunningState runningState)
        {
            if (!runningState.DeltaFolder.Exists)
            {
                throw new DirectoryNotFoundException("Delta folder was not found.");
            }
        }

        private void InitalizeDBName()
        {
            if (dbName == string.Empty)
            {
                dbName = GetDbName();
            }
        }

        #endregion

        #region Private Methods

        private void CheckThatdeltaRunnerTablesAreSetUp()
        {
            if (!ColumnExists(DeltaVersionTableName, DeltaVersionColumnName))
            {
                throw new Exception(string.Format("Required column '{0}' was not found in table '{1}'",
                                                  DeltaVersionColumnName,
                                                  DeltaVersionTableName));
            }

            if (!ColumnExists(DeltaVersionTableName, DeltaVersionHashColumnName))
            {
                throw new Exception(string.Format("Required column '{0}' was not found in table '{1}'",
                                                  DeltaVersionHashColumnName,
                                                  DeltaVersionTableName));
            }
        }

        private void LoadFilesIntoListOrderedCorrectlyAndCheckIfModified(RunningState runningState)
        {
            FileInfo[] sqlFiles = runningState.DeltaFolder.GetFiles("*.sql");
            sqlFiles = SortByFilename(sqlFiles);

            ArrayList files = new ArrayList();
            foreach (object fileQueue in sqlFilePreDeltaQueue)
            {
                DeltaFile deltaFile = (DeltaFile) fileQueue;
                deltaFile.CalculateHash();
                deltaFile.IsModified = !runningState.HashProvider.CheckDeltaHash(deltaFile.Filename, deltaFile.Hash);
                files.Add(deltaFile);
            }

            foreach (FileInfo info in sqlFiles)
            {
                DeltaFile deltaFile = new DeltaFile(info);
                deltaFile.CalculateHash();
                deltaFile.IsModified = !runningState.HashProvider.CheckDeltaHash(deltaFile.Filename, deltaFile.Hash);
                files.Add(deltaFile);
            }

            foreach (object fileQueue in sqlFilePostDeltaQueue)
            {
                DeltaFile deltaFile = (DeltaFile) fileQueue;
                deltaFile.CalculateHash();
                deltaFile.IsModified = !runningState.HashProvider.CheckDeltaHash(deltaFile.Filename, deltaFile.Hash);
                files.Add(deltaFile);
            }

            runningState.DeltaFiles = (DeltaFile[])files.ToArray(typeof(DeltaFile));
        }

        private void DetermineWhichDeltasToRunAndQueueThem(RunningState runningState) 
        {
            foreach (DeltaFile deltaFile in runningState.DeltaFiles)
            {
                if (runningState.QueuedFiles != null ||
                    (RunInDevelopmentMode && deltaFile.IsModified) ||
                    IsDeltaBiggerThen(deltaFile, runningState.LatestVersion) || runningState.LatestVersion == "0" || (ShouldRunPostDeltasAllTheTime && deltaFile.Version == "-1"))
                {
                    if (deltaFile.Version == "-2" && !runningState.HashProvider.IsNewDelta(deltaFile.Filename))
                    {
                        if (dropDbOnPreDeltaChange && masterConnectionString.Length > 0)
                        {
                            runningState.ShouldDropAndRecreate = true;
                            break;
                        }
                        else
                        {
                            ConsoleWrite("A preDelta file has changed (" + deltaFile.Filename +
                                         ").  Please drop and recreate you database for this change to be implemented.");
                            continue;
                        }
                    }
                    if (IsDeltaBiggerThen(deltaFile, "0") && IsDeltaLessThenOrEqual(deltaFile, runningState.LatestVersion))
                    {
                        runningState.LatestVersion = deltaFile.Version;
                        runningState.ReRunDelta = true;
                    }
                    if (runningState.QueuedFiles == null)
                    {
                        runningState.QueuedFiles = new ArrayList();
                    }
                    runningState.QueuedFiles.Add(deltaFile);

                    Hashtable categories = runningState.Categories;
                    if (deltaFile.Version != "-1" && deltaFile.MaySkipCategories != null &&
                        deltaFile.MaySkipCategories.Count > 0)
                    {
                        foreach (string maySkipCategory in deltaFile.MaySkipCategories)
                        {
                            if (categories[maySkipCategory] == null)
                            {
                                categories[maySkipCategory] = 1;
                            }
                            else
                            {
                                categories[maySkipCategory] = (int) categories[maySkipCategory] + 1;
                            }
                        }
                    }

                    if (deltaFile.Version == "-1")
                    {
                        runningState.PostDeltaFilenamesToDelete.AppendFormat("'{0}',", deltaFile.Filename);
                    }
                    else
                    {
                        runningState.CategoryCounter++;
                    }
                }
            }
        }

        private void IfInDevelopmentModeDeleteVersionsThatNeedToBeReRun(RunningState runningState, IDbConnection conn, IDbTransaction tran)
        {
            ArrayList queuedFiles = runningState.QueuedFiles;
            if (RunInDevelopmentMode && queuedFiles != null)
            {
                DeltaFile firstFile = (DeltaFile) queuedFiles[0];
                if (firstFile.Version == "-2")
                {
                    DeleteVersionTable(conn, tran);
                }
                else if (runningState.ReRunDelta)
                {
                    DeleteNewerDeltas(conn, runningState.LatestVersion, tran);
                }
            }
        }

        private void DeletePostDeltasThatNeedToBeReRun(RunningState runningState, IDbConnection conn, IDbTransaction tran)
        {
            StringBuilder sbPostFilename = runningState.PostDeltaFilenamesToDelete;
            if (sbPostFilename.Length > 0)
            {
                sbPostFilename = sbPostFilename.Remove(sbPostFilename.Length - 1, 1);
                DeletePostDeltas(conn, sbPostFilename, tran);
            }
        }

        private void RunTheQueuedDeltas(RunningState runningState, IDbConnection conn, ref IDbTransaction tran)
        {
            ArrayList queuedFiles = runningState.QueuedFiles;
            if (queuedFiles == null)
                return;

            string fileContents = string.Empty;
            string filePath = string.Empty;

            try
            {
                DeltaFile[] myFiles = (DeltaFile[]) queuedFiles.ToArray(typeof (DeltaFile));

                foreach (DeltaFile deltaFile in myFiles)
                {
                    DateTime start = DateTime.Now;

                    if (deltaFile.Version != "-1" || ShouldRunPostDeltasAllTheTime || deltaFile.IsModified ||
                        (deltaFile.Category == null || runningState.Categories[deltaFile.Category] == null || (int) runningState.Categories[deltaFile.Category] != runningState.CategoryCounter))
                    {
                        if (deltaFile.IsModified && deltaFile.Category != null)
                        {
                            runningState.Categories[deltaFile.Category] = null;
                        }

                        if (OnScriptExecution != null)
                        {
                            OnScriptExecution(new ScriptExecutionArgs(deltaFile));
                        }

                        runningState.SchemaModified = true;
                        filePath = deltaFile.File.FullName;
                        fileContents = GetFileContext(deltaFile.File);

                        if (UseTransactions == false || deltaFile.UseTransaction)
                        {
                            ConsoleWrite("Running Delta : {0}", deltaFile.Filename);

                            ExecuteNonQueryWithDBReset(fileContents, conn, tran);
                            if (RunInDevelopmentMode)
                            {
                                // In DEV mode, we run twice to ensure our deltas are repeatable.
                                ExecuteNonQueryWithDBReset(fileContents, conn, tran);
                            }
                        }
                        else
                        {
                            ConsoleWrite("Commiting Transaction for a non transactional delta.");
                            tran.Commit();
                            ConsoleWrite("Running Delta : {0}", deltaFile.Filename);
                            ExecuteNonQuery(fileContents);
                            if (RunInDevelopmentMode)
                            {
                                // In DEV mode, we run twice to ensure our deltas are repeatable.
                                ExecuteNonQuery(fileContents);
                            }

                            ConsoleWrite("Starting new Transaction.");
                            conn.Close();
                            conn.Open();
                            tran = conn.BeginTransaction(IsolationLevel.Serializable);
                        }

                        AddDeltaToTrackingTable(conn, deltaFile, tran);

                        if (OnAfterScriptExecution != null)
                        {
                            OnAfterScriptExecution(new ScriptExecutionArgs(deltaFile, DateTime.Now - start));
                        }
                    }
                    else
                    {
                        ConsoleWrite("Skipping Delta : {0}", deltaFile.Filename);
                        if (OnSkipScriptExecution != null)
                        {
                            OnSkipScriptExecution(new ScriptExecutionArgs(deltaFile, DateTime.Now - start));
                        }

                        AddDeltaToTrackingTable(conn, deltaFile, tran);
                    }
                }
            }
            catch(SqlException)
            {
                Console.Out.WriteLine("Delta File Name		-->	{0}", filePath);
                Console.Out.WriteLine("Error Prone SQL		-->	{0}", fileContents);
                throw;
            }
        }

        private void RunSqlScript(string sql)
        {
            using (StringReader reader = new StringReader(sql))
            {
                RunSqlScript(reader);
            }
        }

        private void DropAndRecreate()
        {
            ConsoleWrite("------------");
            ConsoleWrite("A Pre Delta file change was detected.  Dropping and recreating the database.");
            ConsoleWrite("");
            CreateDatabase(dbName, masterConnectionString, true, type);
            PrepareForDeltaRunner();
            ConsoleWrite("------------");
        }

        private string GetFileContext(FileInfo info)
        {
            using (FileStream deltaStream = info.OpenRead())
            {
                using (StreamReader deltaReader = new StreamReader(deltaStream))
                {
                    //string endXmlTag = "</deltarunner>";
                    string deltaText = deltaReader.ReadToEnd();
                    return deltaText;
                    //int locOfEndXml = deltaText.ToLower().IndexOf(endXmlTag);
                    //if (locOfEndXml > 0)
                    //{
                    //    deltaText = deltaText.Substring(locOfEndXml + endXmlTag.Length);
                    //    return deltaText;
                    //}
                    //else
                    //{
                    //    return deltaText;
                    //}
                }
            }
        }

        #endregion

        #region Public Helpers

        public FileInfo[] SortByFilename(FileInfo[] files)
        {
            Array.Sort(files, new FileInfoComparer());
            return files;
        }

        public bool IsDeltaBiggerThen(DeltaFile deltaFile, string latestVersion)
        {
            if (deltaFile.Version == "-1")
            {
                if (latestVersion == "-2")
                    return true;
                if (latestVersion == "-1")
                    return false;
                return false;
            }
            if (deltaFile.Version == "-2")
            {
                return false;
            }
            if (latestVersion == "-1" || latestVersion == "-2")
                return true;
            return String.Compare(deltaFile.Version, latestVersion) > 0;
        }

        public bool IsDeltaLessThenOrEqual(DeltaFile deltaFile, string latestVersion)
        {
            if (deltaFile.Version == "-1")
            {
                if (latestVersion == "-2")
                    return false;
                if (latestVersion == "-1")
                    return true;
                return true;
            }
            if (deltaFile.Version == "-2")
            {
                return true;
            }
            if (latestVersion == "-1" || latestVersion == "-2")
                return false;

            return String.Compare(deltaFile.Version, latestVersion, true) <= 0;
        }

        #endregion

        #region Protected Abstract Methods

        protected abstract void RunSqlScript(StringReader reader);
        protected abstract DataTable ExecuteDataTable(string sql);
        protected abstract void ExecuteNonQuery(string sql);
        protected abstract void ExecuteNonQueryWithDBReset(string sql, IDbConnection conn, IDbTransaction tran);
        protected abstract void UseCurrentDB(IDbConnection conn, IDbTransaction tran);
        protected abstract DataTable GetDeltaTable();
        protected abstract void AddDeltaToTrackingTable(IDbConnection conn, DeltaFile deltaFile, IDbTransaction tran);
        protected abstract void DeleteNewerDeltas(IDbConnection conn, string latestVersion, IDbTransaction tran);
        protected abstract void DeleteVersionTable(IDbConnection conn, IDbTransaction tran);
        protected abstract void DeletePostDeltas(IDbConnection conn, StringBuilder sbPostFilename, IDbTransaction tran);
        protected abstract void InternalDropDatabase(string databaseName, string connectionStringMaster);

        protected abstract void InternalCreateDatabase(string databaseName, string connectionStringMaster,
                                                       bool deleteDatabaseFirst);

        #endregion

        #region Protected Methods

        protected string[] SplitSqlString(string sql)
        {
            ArrayList arrSql = new ArrayList();
            using (StringReader reader = new StringReader(sql))
            {
                string line;
                StringBuilder cmd = new StringBuilder();
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Trim().ToLower().Equals("go"))
                    {
                        arrSql.Add(cmd.ToString());
                        cmd.Length = 0;
                    }
                    else
                    {
                        cmd.Append(line);
                        cmd.Append(Environment.NewLine);
                    }
                }
                if (cmd.ToString().Trim().Length > 0)
                {
                    arrSql.Add(cmd.ToString());
                }
                return (string[]) arrSql.ToArray(typeof (string));
            }
        }

        #endregion

        #region Internal Methods

        internal void ConsoleWrite(string message)
        {
            if (verbose)
            {
                Console.Out.WriteLine(message);
            }
        }

        internal void ConsoleWrite(string format, params string[] args)
        {
            ConsoleWrite(string.Format(format, args));
        }

        #endregion

        #region SqlDeltaRunner Static Methods

        public static void DropDatabase(string databaseName, string connectionStringMaster)
        {
            DeltaRunnerFactory.CreateDeltaRunner(null, null).InternalDropDatabase(databaseName, connectionStringMaster);
        }

        public static void CreateDatabase(string databaseName, string connectionStringMaster)
        {
            CreateDatabase(databaseName, connectionStringMaster, false);
        }

        public static void CreateDatabase(string databaseName, string connectionStringMaster, bool deleteDatabaseFirst)
        {
            DeltaRunnerFactory.CreateDeltaRunner(null, null).InternalCreateDatabase(databaseName, connectionStringMaster,
                                                                                    deleteDatabaseFirst);
        }

        public static void DropDatabase(string databaseName, string connectionStringMaster, DeltaRunnerType type)
        {
            DeltaRunnerFactory.CreateDeltaRunner(null, null, type).InternalDropDatabase(databaseName,
                                                                                        connectionStringMaster);
        }

        public static void CreateDatabase(string databaseName, string connectionStringMaster, DeltaRunnerType type)
        {
            CreateDatabase(databaseName, connectionStringMaster, false, type);
        }

        public static void CreateDatabase(string databaseName, string connectionStringMaster, bool deleteDatabaseFirst,
                                          DeltaRunnerType type)
        {
            DeltaRunnerFactory.CreateDeltaRunner(null, null, type).InternalCreateDatabase(databaseName,
                                                                                          connectionStringMaster,
                                                                                          deleteDatabaseFirst);
        }

        #endregion
    }
}