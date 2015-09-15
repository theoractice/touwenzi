using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace TWZD.Data
{
    public interface DbTableDesc
    {
        string GetCreateCmd();
        string GetInsertCmd(Int64 id);
    }

    public class TWZDData
    {
        public StrokeOrder strokeOrder;
        public Phrase phrase;
        public UserConfig userConfig;
    }

    public struct StrokeOrder : DbTableDesc
    {
        public string 汉字;
        public string 笔顺数据;
        public string 备注;

        public string GetCreateCmd()
        {
            return "CREATE TABLE StrokeOrder("
                + "汉字 TEXT NOT NULL, "
                + "笔顺数据 TEXT NOT NULL, "
                + "备注 TEXT NOT NULL"
                + ")";
        }

        public string GetInsertCmd(Int64 id)
        {
            string str = "";

            foreach (FieldInfo info in typeof(StrokeOrder).GetFields())
            {
                str += info.Name + ", ";
            }

            str = str.Remove(str.Length - 2);

            str = "insert into StrokeOrder(" + str + ") values('"
                + 汉字.ToString() + "','"
                + 笔顺数据.ToString() + "','"
                + 备注.ToString()
                + "')";

            return str;
        }
    }

    public struct Phrase : DbTableDesc
    {
        public string 词语;
        public string 注音;
        public string 释义;
        public string 单字位置;

        public string GetCreateCmd()
        {
            return "CREATE TABLE Phrase("
                + "词语 TEXT NOT NULL, "
                + "注音 TEXT NOT NULL, "
                + "释义 TEXT NOT NULL, "
                + "单字位置 TEXT NOT NULL"
                + ")";
        }

        public string GetInsertCmd(Int64 id)
        {
            string str = "";

            foreach (FieldInfo info in typeof(Phrase).GetFields())
            {
                str += info.Name + ", ";
            }

            str = str.Remove(str.Length - 2);

            str = "insert into Phrase(" + str + ") values('"
                + 词语.ToString() + "','"
                + 注音.ToString() + "','"
                + 释义.ToString() + "','"
                + 单字位置.ToString()
                + "')";

            return str;
        }
    }

    public struct UserConfig : DbTableDesc
    {
        public string 工作时间;
        public string 休息时间;
        public string 显示时间;
        public string 提示透明度;
        public string 文字透明度;
        public string 体感灵敏度;
        public string 摄像机编号;
        public string 小窗显示;

        public string GetCreateCmd()
        {
            return "CREATE TABLE UserConfig("
                + "工作时间 TEXT NOT NULL, "
                + "休息时间 TEXT NOT NULL, "
                + "显示时间 TEXT NOT NULL, "
                + "提示透明度 TEXT NOT NULL, "
                + "文字透明度 TEXT NOT NULL, "
                + "体感灵敏度 TEXT NOT NULL, "
                + "摄像机编号 TEXT NOT NULL, "
                + "小窗显示 TEXT NOT NULL"
                + ")";
        }

        public string GetInsertCmd(Int64 id)
        {
            string str = "";

            foreach (FieldInfo info in typeof(UserConfig).GetFields())
            {
                str += info.Name + ", ";
            }

            str = str.Remove(str.Length - 2);

            str = "insert into UserConfig(" + str + ") values('"
                + 工作时间.ToString() + "','"
                + 休息时间.ToString() + "','"
                + 显示时间.ToString() + "','"
                + 提示透明度.ToString() + "','"
                + 文字透明度.ToString() + "','"
                + 体感灵敏度.ToString() + "','"
                + 摄像机编号.ToString() + "','"
                + 小窗显示.ToString()
                + "')";

            return str;
        }
    }
}
