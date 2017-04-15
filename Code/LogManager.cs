using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HNPatent
{
    /// <summary>
    /// 日志记录类
    /// </summary>
    public class LogManager
    {
        private string filepath;
        public LogManager(string _filepath)
        {
            filepath = _filepath;
        }

        public void WriteLog(string msg)
        {
            WriteLog(msg, "");
        }

        public void WriteLog(string msg, params object[] arg)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText(filepath);
            try
            {
                sw.WriteLine(msg, arg);
                sw.WriteLine("");
            }
            catch
            { }
            finally
            {
                sw.Close();
            }
        }

    }
}