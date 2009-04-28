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
using System.IO;
using System.Text;
using Mono.Data.SqliteClient;

namespace EntropyZero.deltaRunner.Testing
{
    public class SqliteTestFixtureBase : TestFixtureBase
    {
        public new static string ConnectionStringMaster = "URI=file:DeltaRunner.db";
        public new static string ConnectionString = "URI=file:DeltaRunner.db";

        public new object ExecuteScalar(string sql)
        {
            return ExecuteScalar(sql, ConnectionString);
        }

        public new object ExecuteScalar(string sql, string connectionString)
        {
            using (SqliteConnection conn = new SqliteConnection(connectionString))
            {
                SqliteCommand cmd = new SqliteCommand(sql, conn);
                cmd.CommandTimeout = 1200;
                conn.Open();
                object retVal = cmd.ExecuteScalar();
                conn.Close();
                return retVal;
            }
        }

        public new void ExecuteNonQuery(string sql)
        {
            ExecuteNonQuery(sql, ConnectionString);
        }

        public new void ExecuteNonQuery(string sql, string connectionString)
        {
            using (SqliteConnection conn = new SqliteConnection(connectionString))
            {
                SqliteCommand cmd = new SqliteCommand(sql, conn);
                cmd.CommandTimeout = 1200;
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        protected new void RunSqlScript(StreamReader reader)
        {
            RunSqlScript(reader, ConnectionString);
        }

        protected new void RunSqlScript(StreamReader reader, string connectionString)
        {
            using (SqliteConnection conn = new SqliteConnection(connectionString))
            {
                conn.Open();

                try
                {
                    string line;
                    StringBuilder cmd = new StringBuilder();
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Trim().ToLower().Equals("go"))
                        {
                            ExecuteNonQuery(cmd.ToString(), conn, null);
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
                        ExecuteNonQuery(cmd.ToString(), conn, null);
                    }
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine("ex.ToString() = {0}", ex.ToString());
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        private void ExecuteNonQuery(string sql, SqliteConnection conn, SqliteTransaction tran)
        {
            string[] sqlArray = SplitSqlString(sql);
            foreach (string sqlString in sqlArray)
            {
                SqliteCommand cmd = new SqliteCommand(sqlString, conn, tran);
                cmd.CommandTimeout = 1200;
                cmd.ExecuteNonQuery();
            }
        }

        private string[] SplitSqlString(string sql)
        {
            ArrayList arrSql = new ArrayList();
            using(StringReader reader = new StringReader(sql))
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

        public new bool TableExists(string tableName)
        {
            int tableCount =
                Int32.Parse(
                    ExecuteScalar(
                        string.Format(@"SELECT count(tbl_name) from sqlite_master where tbl_name = '{0}'", tableName)).
                        ToString());
            return (tableCount > 0);
        }
    }
}