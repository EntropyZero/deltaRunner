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
using System.Text;

namespace EntropyZero.deltaRunner
{
	public class SqlDeltaRunner : DeltaRunnerBase
	{
		#region Member Variables

		private string prepareScriptResourceName = "EntropyZero.deltaRunner.Resources.PrepareDeltaRunner.sql";
		private string removeScriptResourceName = "EntropyZero.deltaRunner.Resources.RemoveDeltaRunner.sql";
        private string deltaVersionTableName = "dr_DeltaVersion";
        private string deltaVersionColumnName = "LatestDelta";
        private string deltaFilenameColumnName = "Filename";
        private string deltaVersionHashColumnName = "Hash";

		#endregion

	    #region Constructors

	    public SqlDeltaRunner(string connectionString, string deltaPath) : base(connectionString, deltaPath)
	    {
	    }

        public SqlDeltaRunner(string connectionString, string deltaPath, bool verbose) : base(connectionString, deltaPath, verbose)
        {
        }

		public SqlDeltaRunner(string connectionString, string deltaPath, bool verbose, bool shouldRunPostDeltasAllTheTime)
			: base(connectionString, deltaPath, verbose, shouldRunPostDeltasAllTheTime)
		{
		}

		public SqlDeltaRunner(string connectionString, string deltaPath, bool dropDbOnPreDeltaChange,
                               string masterConnectionString, string dbName, bool verbose) : base(connectionString, deltaPath, dropDbOnPreDeltaChange, masterConnectionString, dbName, verbose)
        {
        }

	    #endregion

	    #region Public Override Properties

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

	    #endregion

	    #region Public Override Methods

	    public override bool TableExists(string tableName)
	    {
	        int tableCount =
	            Int32.Parse(
	                ExecuteScalar(
	                    string.Format("SELECT COUNT(TABLE_NAME) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{0}'", tableName)).
	                    ToString());
	        return (tableCount > 0);
	    }

	    public override bool ColumnExists(string tableName, string columnName)
	    {
	        int tableCount =
	            Int32.Parse(
	                ExecuteScalar(
	                    string.Format("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='{0}' AND COLUMN_NAME = '{1}' --dataProfilerIgnore",
	                                  tableName, columnName)).ToString());
	        return (tableCount > 0);
	    }
		
	    public override string GetDbName()
	    {
	        return(ExecuteScalar("select DB_NAME()").ToString());
	    }

	    public override string GetLatestVersion()
	    {
	        object scalar = ExecuteScalar(string.Format("SELECT ISNULL(MAX({0}),'0') FROM [{1}]", DeltaVersionColumnName, DeltaVersionTableName));
	        if (scalar == null || scalar is DBNull) return "0";
	        return scalar.ToString();
	    }

	    #endregion


	    #region Protected Override Methods

	    protected override IDbConnection CreateConnection()
	    {
	        return new SqlConnection(connectionString);
	    }

	    protected override void DeleteVersionTable(IDbConnection conn, IDbTransaction tran)
	    {
	        ExecuteNonQueryWithDBReset(string.Format("DELETE FROM {0}",
	                                                 DeltaVersionTableName), conn, tran);
	    }

	    protected override void DeleteNewerDeltas(IDbConnection conn, string latestVersion, IDbTransaction tran)
	    {
	        ExecuteNonQueryWithDBReset(string.Format("DELETE FROM {0} WHERE {1} = '-1' OR {1} >= '{2}'",
	                                                 DeltaVersionTableName,
	                                                 DeltaVersionColumnName,
	                                                 latestVersion), conn, tran);
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

	    protected override DataTable GetDeltaTable()
        {
            return (ExecuteDataTable(string.Format("select [{1}], [{2}], [{3}] from [{0}]",
                                                                  DeltaVersionTableName,
                                                                  DeltaVersionColumnName,
                                                                  DeltaVersionHashColumnName,
                                                                  DeltaFilenameColumnName)));
        }

	    protected override void DeletePostDeltas(IDbConnection conn, StringBuilder sbPostFilename, IDbTransaction tran)
	    {
	        ExecuteNonQueryWithDBReset(string.Format("DELETE FROM {0} WHERE {1} IN ({2})",
	                                                 DeltaVersionTableName,
	                                                 DeltaFilenameColumnName,
	                                                 sbPostFilename.ToString()), conn, tran);
	    }

	    protected override DataTable ExecuteDataTable(string sql)
	    {
	        using (SqlConnection conn = new SqlConnection(connectionString))
	        {
	            sql = sql + " --dataProfilerIgnore";
	            SqlCommand cmd = new SqlCommand(sql, conn);
	            cmd.CommandTimeout = CommandTimeout;
	            conn.Open();
	            SqlDataAdapter da = new SqlDataAdapter(cmd);
	            DataSet ds = new DataSet();
	            da.Fill(ds);
	            return ds.Tables[0];
	        }
	    }

	    protected override void UseCurrentDB(IDbConnection conn, IDbTransaction tran)
	    {
	        SqlCommand cmd = new SqlCommand(string.Format("USE [{0}]", dbName), (SqlConnection)conn, (SqlTransaction)tran);
			cmd.CommandTimeout = CommandTimeout;
	        cmd.ExecuteNonQuery();

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
	            SqlCommand cmd = new SqlCommand(sqlstr, (SqlConnection)conn, (SqlTransaction)tran);
				cmd.CommandTimeout = CommandTimeout;
	            cmd.ExecuteNonQuery();
	        }
	    }

	    protected override void InternalDropDatabase(string databaseName, string connectionStringMaster)
	    {
	        string dropSql =
	            @"
					IF EXISTS (Select * from sysdatabases where name = '{0}')
					BEGIN
	    					ALTER DATABASE [{0}] 
							SET SINGLE_USER 
							WITH ROLLBACK IMMEDIATE

							DROP DATABASE [{0}] 
					END; --dataProfilerIgnore";
			
	        using (SqlConnection conn = new SqlConnection(connectionStringMaster))
	        {
	            conn.Open();
				
	            SqlCommand cmdDrop = new SqlCommand(string.Format(dropSql, databaseName), conn);
				cmdDrop.CommandTimeout = CommandTimeout;
	            cmdDrop.ExecuteNonQuery();
	        }		
	    }

	    protected override void InternalCreateDatabase(string databaseName, string connectionStringMaster, bool deleteDatabaseFirst)
	    {
	        string createSql = @"
					IF NOT EXISTS (Select * from sysdatabases where name = '{0}')
					BEGIN
						CREATE DATABASE [{0}];
					END;  --dataProfilerIgnore";
			
	        if(deleteDatabaseFirst)
	        {
	            DropDatabase(databaseName, connectionStringMaster);
	        }
	        using (SqlConnection conn = new SqlConnection(connectionStringMaster))
	        {
	            conn.Open();
				
	            SqlCommand cmdCreate = new SqlCommand(string.Format(createSql, databaseName), conn);
				cmdCreate.CommandTimeout = CommandTimeout;
	            cmdCreate.ExecuteNonQuery();
	        }
	    }

	    protected override void RunSqlScript(StringReader reader)
	    {
	        using (SqlConnection conn = new SqlConnection(connectionString))
	        {
	            conn.Open();
	            SqlTransaction tran = conn.BeginTransaction(IsolationLevel.Serializable);

	            try
	            {
	                string line;
	                StringBuilder cmd = new StringBuilder();
	                while ((line = reader.ReadLine()) != null)
	                {
	                    if (line.Trim().ToLower() == "go")
	                    {
	                        ExecuteNonQuery(cmd.ToString(), conn, tran);
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
	        }
	    }

	    protected override void ExecuteNonQuery(string sql)
	    {
	        using (SqlConnection conn = new SqlConnection(connectionString))
	        {
	            conn.Open();
	            string[] sqlArray = SplitSqlString(sql);
	            foreach (string sqlString in sqlArray)
	            {
	                if (sqlString == string.Empty)
	                {
	                    return;
	                }
	                string sqlstr = sqlString + " --dataProfilerIgnore";
	                SqlCommand cmd = new SqlCommand(sqlstr, conn);
					cmd.CommandTimeout = CommandTimeout;
	                cmd.ExecuteNonQuery();
	            }
	        }
	    }

	    #endregion

	    #region Private Methods

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
				SqlCommand cmd = new SqlCommand(sqlstr, (SqlConnection)conn, (SqlTransaction)tran);
				cmd.CommandTimeout = CommandTimeout;
				cmd.ExecuteNonQuery();
			}
		}

        private object ExecuteScalar(string sql)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                sql = sql + " --dataProfilerIgnore";
                SqlCommand cmd = new SqlCommand(sql, conn);
				cmd.CommandTimeout = CommandTimeout;
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

	    #endregion
	}
}
