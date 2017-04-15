using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Script.Serialization;
using System.Diagnostics;

namespace HNPatent
{
    public partial class index : System.Web.UI.Page
    {
        LogManager loger;
        LogManager logger;

        public static string dic = "C:\\test\\";

        public int datacount = 0;

        public int totalcount = 300;

        private HttpContext _curContext;
        public HttpContext curContext
        {
            get { return _curContext; }
            set { _curContext = value; }
        }

        /// <summary>
        /// 主运行方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                Stopwatch st = new Stopwatch(); //计时
                st.Start();

                string FileName = HttpContext.Current.Server.MapPath("~/HNPatent.xls"); ;
                FileStream fs = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);

                //初始化请求
                curContext = HttpContext.Current;
                curContext.Response.AddHeader("Content-Disposition", "attachment; filename=" + FileName);
                curContext.Response.ContentType = "application/ms-excel";

                //添加表头
                curContext.Response.Write("专利名称" + "\t" + "申请专利号" + "\t" + "申请日期" + "\t" + "申请（专利权）人" + "\t" + "主分类号" + "\t" + "分类号" + "\n");


                if (!Directory.Exists(dic)) Directory.CreateDirectory(dic);
                loger = new LogManager(Path.Combine(dic, "logok.txt"));
                logger = new LogManager(Path.Combine(dic, "logger.txt"));
               
                //要查询的字符
                string searchStr = "test";

                int count = 1;

                ///下面开始进行post抓取
                ///循环抓取
                while (true)
                {
                    HttpHelper http = new HttpHelper();
                    HttpItem item = new HttpItem()
                    {

                        URL = "http://****/url.aspx",//URL     必需项   
                        Encoding = Encoding.UTF8,//URL     可选项 默认为Get  
                        Method = "POST",//URL     可选项 默认为Get  
                        Referer = "",//来源URL     可选项  
                        Cookie = "ASP.NET_SessionId=*******",
                        Postdata = "{requestModule:'PatentSearch',userId:'',patentSearchConfig:{Query:'" + searchStr + "',TreeQuery:'',Database:'wgzl,syxx,fmzl',Action:'Search',Page:'"+count+"',PageSize:"+totalcount+",GUID:'',Sortby:'',AddOnes:'',DelOnes:'',RemoveOnes:'',TrsField:'',SmartSearch:''}}",//Post数据     可选项GET时不需要写
                        //PostDataType = PostDataType.String,
                        Timeout = 100000,//连接超时时间     可选项默认为100000   
                        ReadWriteTimeout = 30000,//写入Post数据超时时间     可选项默认为30000  
                        UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:31.0) Gecko/20100101 Firefox/31.0",//用户的浏览器类型，版本，操作系统     可选项有默认值  
                        ContentType = "application/x-www-form-urlencoded; charset=UTF-8",//返回类型    可选项有默认值  
                        Allowautoredirect = true,
                        Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                        ResultCookieType = ResultCookieType.CookieCollection,

                    };
                    HttpResult result = http.GetHtml(item);

                    string html = result.Html; // 得到需要的整个页面

                    Hashtable hash = JSON.Decode(html) as Hashtable;    //得到当前请求的所有数据
                    if (hash == null) break;
                    OperatData(hash);

                    Hashtable option = hash["Option"] as Hashtable;

                    int Surplus = int.Parse(option["Count"].ToString()) - totalcount * count;
                    if (Surplus < 300) totalcount = int.Parse(option["Count"].ToString()) - totalcount * count;
                    else totalcount = 300;
                    count++;
                }
                loger.WriteLog("共有{0}页数据,最后一页{1}条数据", count, totalcount);

                loger.WriteLog("共有{0}条数据", datacount);

                st.Stop();

                loger.WriteLog("共耗时{0}ms。", st.ElapsedMilliseconds);
                //写完之后，必须结束请求，否则会写入失败
                curContext.Response.End();
                
                Response.Write("数据获取成功！");

            }
            catch (Exception ex)
            {
                //出现异常，写入日志进行保存
                logger.WriteLog("出错了，原因：{0}",ex.Message);
            }
        }

        /// <summary>
        /// 对数据进行操作并写入文件当中
        /// </summary>
        /// <param name="data">要处理的数据</param>
        public void OperatData(Hashtable data)
        {
            //开始处理数据
            Hashtable clsdata = data["Option"] as Hashtable;
            
            //得到当前请求到数据的数组
            ArrayList arraylist = clsdata["PatentList"] as ArrayList;

           //处理ArrayList数据，拿出所有需要的，直接写入Excel表中
            ///数据
            /// TI : 专利名
            /// SIC : 主分类号
            /// SICN ： 分类号（待定）
            /// PA ： 申请 （专利）人
            /// AD：申请日
            /// AN ： 申请(专利)号
            for(int i = 0; i < arraylist.Count; i++)
            {
                Hashtable ht = arraylist[i] as Hashtable;
                if (ht == null) return;

                string pa = Regex.Replace(ht["PA"].ToString(), "^<[\\s\\S]*?>", "");
                pa = pa.Replace("<font color='red'>", "");
                curContext.Response.Write(ht["TI"] + "\t" + ht["AN"] + "\t" + ht["AD"] + "\t" + pa.Replace("</font>", "") + "\t" + ht["SIC"] + "\t" + ht["SICN"] + "\n");
                datacount++;
            }
            
        }
    }
}