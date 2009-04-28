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
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace EntropyZero.deltaRunner.Testing
{
	public class TestFixtureBase
	{
		public static string ConnectionStringMaster = "user id=test;password=test;Initial Catalog=master;Data Source=(local);pooling=false;";
		public static string ConnectionString = "user id=test;password=test;Initial Catalog=DeltaRunner;Data Source=(local);pooling=false;";

		public object ExecuteScalar(string sql)
		{
			return ExecuteScalar(sql, ConnectionString);
		}

		public object ExecuteScalar(string sql, string connectionString)
		{
			using (SqlConnection conn = new SqlConnection(connectionString))
			{
				SqlCommand cmd = new SqlCommand(sql, conn);
				cmd.CommandTimeout = 1200;
				conn.Open();
				return cmd.ExecuteScalar();
			}
		}

		public void ExecuteNonQuery(string sql)
		{
			ExecuteNonQuery(sql, ConnectionString);
		}

		public void ExecuteNonQuery(string sql, string connectionString)
		{
			using (SqlConnection conn = new SqlConnection(connectionString))
			{
				SqlCommand cmd = new SqlCommand(sql, conn);
				cmd.CommandTimeout = 1200;
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}

		protected void RunSqlScript(StreamReader reader)
		{
			RunSqlScript(reader, ConnectionString);
		}

		protected void RunSqlScript(StreamReader reader, string connectionString)
		{
			using (SqlConnection conn = new SqlConnection(connectionString))
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

		private void ExecuteNonQuery(string sql, SqlConnection conn, SqlTransaction tran)
		{
			string[] sqlArray = SplitSqlString(sql);
			foreach (string sqlString in sqlArray)
			{
				SqlCommand cmd = new SqlCommand(sqlString, conn, tran);
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

		public bool TableExists(string tableName)
		{
			int tableCount = Int32.Parse(ExecuteScalar(string.Format("SELECT COUNT(TABLE_NAME) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{0}'", tableName)).ToString());
			return (tableCount > 0);
		}
	}
}