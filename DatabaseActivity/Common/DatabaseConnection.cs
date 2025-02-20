﻿using Plugins.Shared.Library;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace DatabaseActivity
{
    public class DatabaseConnection : IDisposable
    {
        private DbConnection _connection;
        private DbCommand _command;
        private DbTransaction _transaction;
        private string _providerName;

        internal DatabaseConnection Initialize(string connectionString, string providerName)
        {
            _providerName = providerName;
            _connection = DbProviderFactories.GetFactory(providerName).CreateConnection();
            _connection.ConnectionString = connectionString;
            OpenConnection();
            return this;
        }

        internal virtual void BeginTransaction()
        {
            _transaction = _connection.BeginTransaction();
        }
        internal virtual DataTable ExecuteQuery(string sql, Dictionary<string, Tuple<object, ArgumentDirection>> parameters, int commandTimeout, CommandType commandType = CommandType.Text)
        {
            OpenConnection();
            SetupCommand(sql, parameters, commandTimeout, commandType);
            _command.Transaction = _transaction;
            DataTable dt = new DataTable();
            dt.Load(_command.ExecuteReader());
            return dt;
        }

        internal virtual int Execute(string sql, Dictionary<string, Tuple<object, ArgumentDirection>> parameters, int commandTimeout, CommandType commandType = CommandType.Text)
        {
            OpenConnection();
            SetupCommand(sql, parameters, commandTimeout, commandType);
            _command.Transaction = _transaction;
            var result = _command.ExecuteNonQuery();
            foreach (var param in _command.Parameters)
            {
                var dbParam = param as DbParameter;
                parameters[dbParam.ParameterName] = new Tuple<object, ArgumentDirection>(dbParam.Value, WokflowParameterDirectionToDbParameter(dbParam.Direction));
            }
            return result;
        }

        internal virtual int InsertDataTable(string tableName, DataTable dataTable)
        {
            try
            {
                DbDataAdapter dbDA = DbProviderFactories.GetFactory(_providerName).CreateDataAdapter();
                DbCommandBuilder cmdb = DbProviderFactories.GetFactory(_providerName).CreateCommandBuilder();
                cmdb.DataAdapter = dbDA;
                dbDA.ContinueUpdateOnError = false;

                dbDA.SelectCommand = _connection.CreateCommand();
                dbDA.SelectCommand.Transaction = _transaction;
                dbDA.SelectCommand.CommandType = CommandType.Text;
                dbDA.SelectCommand.CommandText = string.Format("select {0} from {1}", GetColumnNames(dataTable), tableName);

                dbDA.InsertCommand = cmdb.GetInsertCommand();
                dbDA.InsertCommand.Connection = _connection;
                dbDA.InsertCommand.Transaction = _transaction;

                foreach (DataRow row in dataTable.Rows)
                {
                    if (row.RowState == DataRowState.Unchanged)
                        row.SetAdded();
                }

                return dbDA.Update(dataTable);
            }
            catch(Exception e)
            {
                SharedObject.Instance.Output(SharedObject.OutputType.Error, "InsertDataTable Error", e.Message);
                return -1;
            }
        }

        internal virtual void Commit()
        {
            _transaction?.Commit();
        }

        internal virtual void Rollback()
        {
            _transaction?.Rollback();
        }

        public virtual void Dispose()
        {
            _command?.Dispose();
            _transaction?.Dispose();
            _connection?.Dispose();
        }

        private void OpenConnection()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }

        private void SetupCommand(string sql, Dictionary<string, Tuple<object, ArgumentDirection>> parameters, int commandTimeout, CommandType commandType = CommandType.Text)
        {
            if (_connection == null)
            {
                SharedObject.Instance.Output(SharedObject.OutputType.Error, "数据库连接为空!");
            }

            _command = _command ?? _connection.CreateCommand();

            var ceilVal = (int)Math.Ceiling((double)commandTimeout / 1000);

            if (ceilVal != 0)
            {
                _command.CommandTimeout = ceilVal;
            }

            _command.CommandType = commandType;
            _command.CommandText = sql;
            _command.Parameters.Clear();
            if (parameters == null)
            {
                return;
            }
            foreach (var param in parameters)
            {
                DbParameter dbParameter = _command.CreateParameter();
                dbParameter.ParameterName = param.Key;
                dbParameter.Direction = WokflowDbParameterToParameterDirection(param.Value.Item2);
                if (dbParameter.Direction.HasFlag(ParameterDirection.InputOutput) || dbParameter.Direction.HasFlag(ParameterDirection.Output))
                {
                    dbParameter.Size = -1;
                }
                dbParameter.Value = param.Value.Item1;
                _command.Parameters.Add(dbParameter);
            }
        }

        private string GetColumnNames(DataTable table)
        {
            if (table.Columns.Count < 1 || table.Rows.Count < 1)
            {
                return string.Empty;
            }

            var columns = new StringBuilder();
            foreach (DataColumn column in table.Columns)
            {
                columns.Append(column.ColumnName + ",");
            }
            columns = columns.Remove(columns.Length - 1, 1);

            return columns.ToString();
        }

        private static ParameterDirection WokflowDbParameterToParameterDirection(ArgumentDirection argumentDirection)
        {
            switch (argumentDirection)
            {
                case ArgumentDirection.In:
                    return ParameterDirection.Input;
                case ArgumentDirection.Out:
                    return ParameterDirection.Output;
                default:
                    return ParameterDirection.InputOutput;
            }
        }

        private static ArgumentDirection WokflowParameterDirectionToDbParameter(ParameterDirection parameterDirection)
        {
            switch (parameterDirection)
            {
                case ParameterDirection.Input:
                    return ArgumentDirection.In;
                case ParameterDirection.Output:
                    return ArgumentDirection.Out;
                case ParameterDirection.InputOutput:
                    return ArgumentDirection.InOut;
                default:
                    return ArgumentDirection.InOut;
                    //throw new ArgumentException(Resources.ParameterDirectionArgumentException);
            }
        }
    }
}
