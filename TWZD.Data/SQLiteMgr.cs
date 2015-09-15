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
        string _dbFilePath, _dbFileName;
        string _dbFileFullName;
        string _dbName;


        SQLiteConnection _conn;
        SQLiteCommand _cmd;
        Type _dbDesc;
        SQLiteDataAdapter _SQLiteAdptr;

        public SQLiteMgr(string filePath, string fileName, Type dbDescClass)
        {
            _dbFilePath = filePath;
            _dbFileName = fileName;
            _dbDesc = dbDescClass;
            _dbName = _dbDesc.Name;
            _dbFileFullName = _dbFilePath + "\\" + _dbFileName + ".db";

            _conn = new SQLiteConnection();
            _cmd = new SQLiteCommand("", _conn);
            _SQLiteAdptr = new SQLiteDataAdapter();

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
                ExecuteSQLiteCommand((loader as DbTableDesc).GetCreateCmd());
            }
            else
            {
                MessageBox.Show("错误的数据库定义类型");
            }
        }

        private void ExecuteSQLiteCommand(string cmdText)
        {
            _cmd.CommandText = cmdText;

            try
            {
                _cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "无法进行数据库操作");
            }
        }

        public DataTable SelectFromTable(string table, string variable, string value)
        {
            _cmd.CommandText = "select * from " + table.Trim() + " where "
                + variable.Trim() + " = '" + value.Trim() + "'";

            _SQLiteAdptr = new SQLiteDataAdapter();
            _SQLiteAdptr.SelectCommand = _cmd;

            DataTable ds = new DataTable();
            try
            {
                _SQLiteAdptr.Fill(ds);
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
            ExecuteSQLiteCommand((string)item.GetInsertCmd(0));
        }

        public void ResetTable<T>(T type)
            where T : Type
        {
            ExecuteSQLiteCommand("DROP TABLE " + type.Name);
            CreateTable(type);
        }
    }
}
