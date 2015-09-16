using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace TWZD.Data
{
    public class SQLiteMgr
    {
        string _dbFilePath, _dbFileName, _dbFileFullName, _dbName;

        SQLiteConnection _conn;
        Type _dbDesc;

        public SQLiteMgr(string filePath, string fileName, Type dbDescClass)
        {
            _dbFilePath = filePath;
            _dbFileName = fileName;
            _dbDesc = dbDescClass;
            _dbName = _dbDesc.Name;
            _dbFileFullName = _dbFilePath + "\\" + _dbFileName + ".db";

            _conn = new SQLiteConnection();

            if (File.Exists(_dbFileFullName))
            {
                Open();
            }
        }

        public void Open()
        {
            string connStr = string.Format("Data Source={0}", _dbFileFullName);
            _conn.ConnectionString = connStr;
            //_conn.SetPassword("FB4201A0C89EA0C01B03F1FD52FA6625");

            try
            {
                _conn.Open();
            }
            catch (Exception ex)
            {
                _conn.Close();
                MessageBox.Show(ex.Message, "初始化到数据库的连接失败");
            }
        }

        public void Close()
        {
            try
            {
                _conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "数据库关闭失败");
                return;
            }
        }

        public void ResetDB()
        {
            DropDB();
            CreateDB();
        }

        public void DropDB()
        {
            if (_conn.State == ConnectionState.Open)
            {
                _conn.Close();
            }
            File.Delete(_dbFileFullName);
        }

        public void CreateDB()
        {
            SQLiteConnection.CreateFile(_dbFileFullName);
            Open();
            //_conn.ChangePassword("FB4201A0C89EA0C01B03F1FD52FA6625");

            foreach (FieldInfo info in _dbDesc.GetFields())
            {
                CreateTable(info.FieldType);
            }
        }

        private void CreateTable(Type type)
        {
            object loader = Activator.CreateInstance(type);
            if (loader is DbTableDesc)
            {
                ExecuteSQL((loader as DbTableDesc).GetCreateCmd());
            }
            else
            {
                MessageBox.Show("错误的数据库定义类型");
            }
        }

        public DataTable SelectFromTable(string table, string variable, string value)
        {
            SQLiteCommand cmd = new SQLiteCommand(
                string.Format("select * from {0} where {1} = @value",
                table, 
                variable),
                _conn);

            SQLiteParameter param = new SQLiteParameter();
            param.ParameterName = "@value";
            param.DbType = DbType.String;
            param.Value = value;

            cmd.Parameters.Add(param);

            SQLiteDataAdapter dbReader = new SQLiteDataAdapter();
            dbReader.SelectCommand = cmd;

            DataTable ds = new DataTable();
            try
            {
                dbReader.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "查询失败");
                return ds;
            }
        }

        public void AddToDB<T>(T item)
            where T : DbTableDesc
        {
            ExecuteSQL((string)item.GetInsertCmd(0));
        }

        public void ResetTable<T>(T type)
            where T : Type
        {
            ExecuteSQL("DROP TABLE " + type.Name);
            CreateTable(type);
        }

        private int ExecuteSQL(string cmdText)
        {
            SQLiteCommand cmd = new SQLiteCommand(cmdText, _conn);
            cmd.CommandText = cmdText;

            try
            {
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "无法进行数据库操作");
                return -1;
            }
        }
    }
}
