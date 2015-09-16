using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace TWZD.Data
{
    public class SQLiteMgr
    {
        string _dbFileFullName, _dbName;
        SQLiteConnection _conn;
        Type _dbInfo;

        public SQLiteMgr(string filePath, string fileName, Type dbInfo)
        {
            _dbInfo = dbInfo;
            _dbName = _dbInfo.Name;
            _dbFileFullName = string.Format("{0}/{1}.db", filePath, fileName);

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
                throw new Exception("初始化到数据库的连接失败", ex);
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
                throw new Exception("数据库关闭失败", ex);
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

            foreach (FieldInfo info in _dbInfo.GetFields())
            {
                CreateTable(info.FieldType);
            }
        }

        private void CreateTable(Type type)
        {
            object loader = Activator.CreateInstance(type);
            if (loader is DbTableDesc)
            {
                ExecuteSQL((loader as DbTableDesc).GetCreateCommand());
            }
            else
            {
                throw new Exception("错误的数据库定义类型");
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
                throw new Exception("查询失败", ex);
            }
        }

        public void AddToDB<T>(T item)
            where T : DbTableDesc
        {
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(
                string.Format("select * from {0} where 0 = 1", item.GetType().Name), _conn);
            SQLiteCommandBuilder cmdGen = new SQLiteCommandBuilder(adapter);
            SQLiteCommand cmd = cmdGen.GetInsertCommand(true);

            foreach (FieldInfo info in item.GetType().GetFields())
            {
                cmd.Parameters.Add(new SQLiteParameter(
                    string.Format("@{0}", info.Name),
                    info.GetValue(item)));
            }

            ExecuteSQL(cmd);
        }

        public void ResetTable<T>(T type)
            where T : Type
        {
            ExecuteSQL("DROP TABLE " + type.Name);
            CreateTable(type);
        }

        private int ExecuteSQL(SQLiteCommand cmd)
        {
            try
            {
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("无法进行数据库操作", ex);
            }
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
                throw new Exception("无法进行数据库操作，请检查SQL命令格式", ex);
            }
        }
    }
}
