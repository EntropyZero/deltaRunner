using System;
using System.Data;
using System.IO;
using System.Text;
using Mono.Data.SqliteClient;

namespace EntropyZero.deltaRunner
{
    public class SqliteDeltaRunner : DeltaRunnerBase
    {
        private string prepareScriptResourceName = "EntropyZero.deltaRunner.Resources.PrepareDeltaRunner.sqlite.sql";
        private string removeScriptResourceName = "EntropyZero.deltaRunner.Resources.RemoveDeltaRunner.sqlite.sql";
        private string deltaVersionTableName = "dr_DeltaVersion";
        private string deltaVersionColumnName = "LatestDelta";
        private string deltaFilenameColumnName = "Filename";
        private string deltaVersionHashColumnName = "Hash";

        public SqliteDeltaRunner(string connectionString, string deltaPath) : base(connectionString, deltaPath)
        {
            type = DeltaRunnerType.Sqlite;
        }

        public SqliteDeltaRunner(string connectionString, string deltaPath, bool verbose)
            : base(connectionString, deltaPath, verbose)
        {
            type = DeltaRunnerType.Sqlite;
        }

        public SqliteDeltaRunner(string connectionString, string deltaPath, bool dropDbOnPreDeltaChange,
                                 string masterConnectionString, string dbName, bool verbose)
            : base(connectionString, deltaPath, dropDbOnPreDeltaChange, masterConnectionString, dbName, verbose)
        {
            type = DeltaRunnerType.Sqlite;
        }

        protected override IDbConnection CreateConnection()
        {
            return new SqliteConnection(connectionString);
        }

        public override string PrepareScriptResourceName
        {
            get { return prepareScriptResourceName; }
        }

        public override string RemoveScriptResourceName
        {
            get { return removeScriptResourceName; }
        }

        public override string DeltaVersionTableName
        {
            get { return deltaVersionTableName; }
            set { deltaVersionTableName = value; }
        }

        public override string DeltaVersionColumnName
        {
            get { return deltaVersionColumnName; }
            set { deltaVersionColumnName = value; }
        }

        public override string DeltaFilenameColumnName
        {
            get { return deltaFilenameColumnName; }
            set { deltaFilenameColumnName = value; }
        }

        public override string DeltaVersionHashColumnName
        {
            get { return deltaVersionHashColumnName; }
            set { deltaVersionHashColumnName = value; }
        }

        public override bool TableExists(string tableName)
        {
            int tableCount =
                Int32.Parse(
                    ExecuteScalar(
                        string.Format(@"SELECT count(tbl_name) from sqlite_master where tbl_name = '{0}'", tableName)).
                        ToString());
            return (tableCount > 0);
        }

        public override bool ColumnExists(string tableName, string columnName)
        {
            DataTable tableDef = ExecuteDataTable(string.Format(@"SELECT tbl_name, sql from sqlite_master where tbl_name = '{0}'", tableName));
            if (tableDef.Rows.Count == 1 && tableDef.Rows[0][1].ToString().Contains(columnName))
                return true;
            else
                return false;
        }

        public override string GetDbName()
        {
            return connectionString;
        }

        public override string GetLatestVersion()
        {
            object scalar =
                ExecuteScalar(string.Format("SELECT MAX({0}) FROM [{1}]", DeltaVersionColumnName, DeltaVersionTableName));
            if (scalar == null || scalar is DBNull) return "0";
            return scalar.ToString();
        }

        protected override void AddDeltaToTrackingTable(IDbConnection conn, DeltaFile deltaFile, IDbTransaction tran)
        {
            string insertSQL = string.Format("INSERT INTO [{0}] ([{1}], [Hash], [Filename]) VALUES('{2}','{3}','{4}')",
                                             DeltaVersionTableName,
                                             DeltaVersionColumnName,
                                             deltaFile.Version,
                                             deltaFile.Hash,
                                             deltaFile.Filename);

            ExecuteNonQueryWithDBReset(insertSQL, conn, tran);
        }

        protected override void DeleteNewerDeltas(IDbConnection conn, string latestVersion, IDbTransaction tran)
        {
            ExecuteNonQueryWithDBReset(string.Format("DELETE FROM {0} WHERE {1} = '-1' OR {1} >= '{2}'",
                                                     DeltaVersionTableName,
                                                     DeltaVersionColumnName,
                                                     latestVersion), conn, tran);
        }

        protected override void DeleteVersionTable(IDbConnection conn, IDbTransaction tran)
        {
            ExecuteNonQueryWithDBReset(string.Format("DELETE FROM {0}",
                                         DeltaVersionTableName), conn, tran);
        }

        protected override void DeletePostDeltas(IDbConnection conn, StringBuilder sbPostFilename, IDbTransaction tran)
        {
            ExecuteNonQueryWithDBReset(string.Format("DELETE FROM {0} WHERE {1} IN ({2})",
                                         DeltaVersionTableName,
                                         DeltaFilenameColumnName,
                                         sbPostFilename.ToString()), conn, tran);
        }

        protected override DataTable GetDeltaTable()
        {
            return (ExecuteDataTable(string.Format("select [{1}], [{2}], [{3}] from [{0}]",
                                                                  DeltaVersionTableName,
                                                                  DeltaVersionColumnName,
                                                                  DeltaVersionHashColumnName,
                                                                  DeltaFilenameColumnName)));
        }

        protected override void RunSqlScript(StringReader reader)
        {
            Console.Out.WriteLine("connectionString = {0}", connectionString);
            using (SqliteConnection conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                SqliteTransaction tran = (SqliteTransaction) conn.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    string line;
                    StringBuilder cmd = new StringBuilder();
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Trim().ToLower() == "go")
                        {
                            ExecuteNonQuery(cmd.ToString(), (IDbConnection) conn, (IDbTransaction) tran);
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
                        ExecuteNonQuery(cmd.ToString(), conn, tran);
                    }
                    tran.Commit();
                }
                catch (Exception)
                {
                    tran.Rollback();
                }
                finally
                {
                    if (conn.State != ConnectionState.Closed)
                    {
                        conn.Close();
                    }
                }
            }
        }

        protected override DataTable ExecuteDataTable(string sql)
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

        protected override void ExecuteNonQuery(string sql)
        {
            using (SqliteConnection conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                try
                {
                    string[] sqlArray = SplitSqlString(sql);
                    foreach (string sqlString in sqlArray)
                    {
                        if (sqlString == string.Empty)
                        {
                            return;
                        }
                        string sqlstr = sqlString + " --dataProfilerIgnore";
                        SqliteCommand cmd = new SqliteCommand(sqlstr, conn);
                        cmd.CommandTimeout = 1200;
                        cmd.ExecuteNonQuery();
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
        }

        private void ExecuteNonQuery(string sql, IDbConnection conn, IDbTransaction tran)
        {
            string[] sqlArray = SplitSqlString(sql);
            foreach (string sqlString in sqlArray)
            {
                if (sqlString == string.Empty)
                {
                    return;
                }
                string sqlstr = sqlString + " --dataProfilerIgnore";
                SqliteCommand cmd = new SqliteCommand(sqlstr, (SqliteConnection) conn, (SqliteTransaction) tran);
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        protected override void UseCurrentDB(IDbConnection conn, IDbTransaction tran)
        {
            //Use is not supported in Sqlite
        }

        protected override void ExecuteNonQueryWithDBReset(string sql, IDbConnection conn, IDbTransaction tran)
        {
            UseCurrentDB(conn, tran);
            string[] sqlArray = SplitSqlString(sql);
            foreach (string sqlString in sqlArray)
            {
                if (sqlString == string.Empty)
                {
                    return;
                }
                string sqlstr = sqlString + " --dataProfilerIgnore";
                SqliteCommand cmd = new SqliteCommand(sqlstr, (SqliteConnection)conn, (SqliteTransaction)tran);
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        protected override void InternalDropDatabase(string databaseName, string connectionStringMaster)
        {
//            string fileName = connectionStringMaster.Substring(connectionStringMaster.IndexOf("file:") + 5);
//            File.Delete(fileName);
            string connectionStringholder = connectionString;
            connectionString = connectionStringMaster;

            DataTable tables = ExecuteDataTable("Select * from sqlite_master");
            foreach (DataRow row in tables.Rows)
            {
                if (row["tbl_name"].ToString() != "sqlite_sequence")
                    ExecuteNonQuery(string.Format("Drop Table IF EXISTS {0}", row["tbl_name"]));
            }

            connectionString = connectionStringholder;
        }

        protected override void InternalCreateDatabase(string databaseName, string connectionStringMaster,
                                                       bool deleteDatabaseFirst)
        {
            if(deleteDatabaseFirst)
            {
                DropDatabase(databaseName, connectionStringMaster, DeltaRunnerType.Sqlite);
            }
            using (SqliteConnection conn = new SqliteConnection(connectionStringMaster))
            {
                conn.Open();
                try
                {
                    string[] sqlArray = SplitSqlString("select * from sqlite_master");
                    foreach (string sqlString in sqlArray)
                    {
                        if (sqlString == string.Empty)
                        {
                            return;
                        }
                        string sqlstr = sqlString + " --dataProfilerIgnore";
                        SqliteCommand cmd = new SqliteCommand(sqlstr, conn);
                        cmd.CommandTimeout = 1200;
                        cmd.ExecuteNonQuery();
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
        }

        private object ExecuteScalar(string sql)
        {
            using (SqliteConnection conn = new SqliteConnection(connectionString))
            {
                object retVal = null;
                try
                {
                    sql = sql + " --dataProfilerIgnore";
                    SqliteCommand cmd = new SqliteCommand(sql, conn);
                    cmd.CommandTimeout = 1200;
                    conn.Open();
                    retVal = cmd.ExecuteScalar();
                }
                finally
                {
                    if (conn.State != ConnectionState.Closed)
                    {
                        conn.Close();
                    }
                }
                return retVal;
            }
        }
    }
}