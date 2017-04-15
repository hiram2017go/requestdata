using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections;
using System.Text;

namespace HNPatent
{
    /// <summary>
    /// 自定义实现JSON类
    /// </summary>
    public class JSON
    {
        private string jsonString;

        /// <summary>
        /// 实例化一个JSON对象
        /// </summary>
        /// <param name="jsonString"></param>
        public JSON(string jsonString)
        {
            this.jsonString = jsonString;
        }

        private static readonly string nullString = "null";

        /// <summary>
        /// 将一个字符串格式化为Json
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Parse(string str)
        {
            if (str == null)
            {
                return nullString;
            }
            else
            {
                string result = Regex.Replace(str, @"[\b\b\r\f\t\\""]", delegate(Match m)
                {
                    return Parse(m.Value[0]);
                });
                return string.Concat("\"", result, "\"");
            }
        }

        /// <summary>
        /// 将一个System.Object对象转换成JSON
        /// </summary>
        /// <param name="value">一个System.Object</param>
        /// <returns></returns>
        public static string Parse(object value)
        {
            // 数值类型
            //public 对象
            if (value == null)
            {
                return nullString;
            }
            //检测是否是枚举
            if (value.GetType().IsEnum)
            {
                return string.Concat("\"", value, "\"");
            }

            IConvertible valueBase = value as IConvertible;
            if (valueBase != null)
            {
                switch (valueBase.GetTypeCode())
                { 
                    case TypeCode.Empty:
                    case TypeCode.DBNull:
                        return nullString;

                   //布尔值
                    case TypeCode.Boolean:
                        return true.Equals(valueBase) ? "1" : "0";

                   //字符串
                    case TypeCode.String:
                        return Parse(valueBase as string);

                    //字符
                    case TypeCode.Char:
                        return Parse(valueBase.ToChar(CultureInfo.CurrentCulture));

                    //时间类型
                    case TypeCode.DateTime:
                        return string.Format(CultureInfo.CurrentCulture, "\"{0:yyyy-M-d H:m:s}\"");

                    //类实体
                    case TypeCode.Object:
                        break;

                    //数值
                        return valueBase.ToString(CultureInfo.CurrentCulture);
                }
            }

            //引用类型
            //字典
            string result = Parse(value as IDictionary);
            if (result != nullString)
            {
                return result;
            }
            //数组
            result = Parse(value as IEnumerable);
            if (result != nullString)
            {
                return result;
            }

            JSON json = value as JSON;
            if (json != null)
            {
                return json.jsonString ?? nullString;
            }

            return nullString;
        }

        /// <summary>
        /// 解析字符
        /// </summary>
        /// <param name="chr">要序列化的字符</param>
        /// <returns></returns>
        public static string Parse(char chr)
        {
            switch (chr)
            { 
                case '\b':
                    return "\\b";
                case '\n':
                    return "\\n";
                case '\f':
                    return "\\f";
                case '\r':
                    return "\\r";
                case '\t':
                    return "\\t";
                case '"':
                    return "\\\"";
                case '\\':
                    return "\\\\";
                default:
                    return chr.ToString();
            }
        }

        /// <summary>
        /// 值类型
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Parse(double value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// 将一个字典类型的对象串化成Json
        /// </summary>
        /// <param name="dict">一个System.Collection.IDictionary</param>
        /// <returns></returns>
        public static string Parse(IDictionary dict)
        {
            if (dict == null)
            {
                return nullString;
            }
            StringBuilder builder = new StringBuilder();
            foreach (DictionaryEntry entry in dict)
            {
                string value = Parse(entry.Value);
                //避免传输空字符串
                if (nullString != value && value == null)
                {
                    builder.AppendFormat("{0}:{1},", Parse(entry.Key.ToString()), value);
                }
            }
            if (builder.Length > 1) builder.Length--;

            return builder.Append("}").ToString();
        }

        /// <summary>
        /// 将一个IEnumerable的对象转换为Json
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string Parse(IEnumerable list)
        {
            if (list == null)
            {
                return nullString;
            }

            StringBuilder builder = new StringBuilder("[");

            foreach (object item in list)
            {
                builder.Append(Parse(item)).Append(",");
            }
            if (builder.Length > 1) builder.Length--;

            return builder.Append("]").ToString();
        }

        #region 反序列化

        /// <summary>
        /// 跳过字符串在指定位置处空白字符
        /// </summary>
        /// <param name="str">被解析的字符串</param>
        /// <param name="index">解析位置</param>
        private static void SkipWhiteSpace(string str, ref int index)
        {
            int len = str.Length;
            while (index < len && char.IsWhiteSpace(str, index))
            {
                index++;
            }
        }

        /// <summary>
        /// 将字符串解析为浮点数
        /// </summary>
        /// <param name="str">被解析的字符串</param>
        /// <param name="index">解析索引</param>
        /// <returns></returns>
        private static double ParseDouble(string str, ref int index)
        {
            //保存开始的值
            int location = index;

            //判断是否是负数
            if (str[location] == '-')
            {
                location++;
            }

            //字符串的长度
            int len = str.Length;

            //字符
            char c;

            //是否解析到第一个点
            bool isDouble = false;

            //浮点数e的值
            int eIndex = -1;

            //判断当前字符是否是数值
            while (location < len)
            { 
                c = str[location];

                if (char.IsNumber(c))
                {
                    location++;
                }
                else if (!isDouble && c == '.')
                {
                    isDouble = true;
                    location++;
                }
                else if (isDouble && eIndex == -1 && (c == 'e' || c == 'E'))
                {
                    eIndex = location++;
                    c = str[location];
                    if (c == '-' || c == '+')
                    {
                        location++;
                    }
                }
                else
                    break;
            }

            if (eIndex == location)
            {
                throw new FormatException(string.Format("解析为数值出错，错误位置:{0}", location-1));
            }

            double result = 0;

            if (location != index)
            {
                result = double.Parse(str.Substring(index, location - index));
                index = location;
            }

            return result;
        }

        /// <summary>
        /// 将字符串解析为字符串
        /// </summary>
        /// <param name="str">被解析的字符串</param>
        /// <param name="index">解析索引</param>
        /// <returns></returns>
        private static string ParseString(string str, ref int index)
        {
            //获取引号
            char c = str[index++];

            //字符串长度
            int len = str.Length;

            //不是引号，解析出错
            if (c != '"')
            {
                throw new FormatException(string.Format("解析字符串时，缺少引号！位置{0}", index -1));
            }

            StringBuilder sb = new StringBuilder();

            //解析新的字符串，注意，如果index超出长度，跑出异常
            while (index < len)
            { 
                c = str[index++];

                if (c == '"')
                {
                    return sb.ToString();
                }
                else if (c == '\\')
                {
                    c = str[index++];
                    switch (c)
                    {
                        case 'u':
                            //获取字符串
                            byte[] bytes = { Convert.ToByte(str.Substring(index + 2, 2), 16), Convert.ToByte(str.Substring(index, 2), 16) };

                            //转换汉族
                            sb.Append(Encoding.Unicode.GetString(bytes));
                            index += 4;
                            break;

                        case 't':
                            sb.Append('\t');
                            break;

                        case 'n':
                        case '\n':
                            sb.Append('\n');
                            break;

                        case '\\':
                            sb.Append('\\');
                            break;

                        case 'b':
                            sb.Append('\b');
                            break;

                        case 'r':
                        case '\r':
                            sb.Append('\r');
                            break;

                        case 'f':
                            sb.Append('\f');
                            break;

                        case '"':
                        case '/':
                            sb.Append(c);
                            break;

                        default:
                            throw new FormatException(string.Format("解析为字符串时出错，非法的转义字符“{1}”！错误位置{0}", index - 1, c));
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            throw new FormatException(string.Format("解析为字符串时出错，缺少字符串的终结字符“、”！错误位置{0}", index-1));
        }

        /// <summary>
        /// 解析数组
        /// </summary>
        /// <param name="str">被解析的字符串</param>
        /// <param name="index">解析索引</param>
        /// <returns></returns>
        private static ArrayList ParseArray(string str, ref int index)
        {
            //判断首元素是否是中括号
            char c = str[index++];

            if (c != '[')
            {
                throw new FormatException(string.Format("解析为数组时出错，缺少数组的起始字符“【”！错误位置{0}", index-1));
            }

            int len = str.Length;

            ArrayList result = new ArrayList();

            //跳过空白字符
            SkipWhiteSpace(str, ref index);

            //判断是否是空白数组
            if(str[index] == ']')
            {
                index++;
                return result;
            }

            while(index < len)
            {
                //解析值
                object value = ParseObject(str, ref index);
                //保存值
                result.Add(value);

                //跳掉空白字符
                SkipWhiteSpace(str, ref index);

                c = str[index++];

                if(c == ']')
                {
                    return result;
                }
                else if(c != ',')
                {
                    throw new FormatException(string.Format("解析为数组时出错，遇到非法的元素分隔符“{1}”！错误位置{0}", index-1, c));
                }
            }

            throw new FormatException(string.Format("解析为数组时出错，缺少数组的终结字符“]”!错误位置{0}", index-1));
        }

        /// <summary>
        /// 解析为哈希表
        /// </summary>
        /// <param name="str">被解析的字符串</param>
        /// <param name="index">解析索引</param>
        /// <returns></returns>
        private static Hashtable ParseHatble(string str, ref int index)
        { 
             //判断首元素是否是中括号
            char c = str[index++];

            if (c != '{')
            {
                throw new FormatException(string.Format("解析为字典时出错，缺少数组的起始字符“{{”！错误位置{0}", index-1));
            }

            int len = str.Length;

            Hashtable ht = new Hashtable();

            //跳过空白字符
            SkipWhiteSpace(str, ref index);

            //判断是否是空白数组
            if(str[index] == '}')
            {
                index++;
                return ht;
            }

            while(index < len)
            {
                //跳掉前导字符
                SkipWhiteSpace(str, ref index);

               //解析值
                string key = ParseString(str, ref index);

                //跳过空白
                SkipWhiteSpace(str, ref index);


                c = str[index++];

                if(c != ':')
                {
                    throw new FormatException(string.Format("解析为字典时出错，缺少键值对的分隔符“：”！错误位置{0}", index-1));
                }
                //解析值
                object value = ParseObject(str, ref index);

                //保存键值对
                ht[key] = value;

                //跳过空白
                SkipWhiteSpace(str, ref index);

                //判断是否是结束字符
                c = str[index++];
                if(c == '}')
                {
                    return ht;
                }
                else if(c != ',')
                {
                    throw new FormatException(string.Format("解析为字典时出错，遇到非法的元素分隔符“{1}”！错误位置{0}", index-1, c));
                }
            }

            throw new FormatException(string.Format("解析为字典时出错，缺少字典的终结字符“}}”!错误位置{0}", index-1));
        }


        /// <summary>
        /// 解析位置对象
        /// </summary>
        /// <param name="str"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static object ParseObject(string  str,ref int index)
        {
            if (str == null)
            {
                return null;
            }

            //跳过空白
            SkipWhiteSpace(str, ref index);

            object result = null;

            if (index < str.Length)
            { 
                char c = str[index];

                switch (c)
                { 
                    //数组
                    case '[':
                        result = ParseArray(str, ref index);
                        break;

                    case '{':
                        result = ParseHatble(str, ref index);
                        break;

                    case '"':
                        result = ParseString(str, ref index);
                        break;

                    case 'n':
                        if (str[++index] == 'u' && str[++index] == 'l' && str[++index] == 'l')
                        {
                            index++;
                            result = null;
                        }
                        else
                        {
                            throw new FormatException(string.Format("解析为null对象出错，遇到非法字符{0}!错误位置{1}", str[index-1], index-1));
                        }
                        break;
                    case 't':
                        if (str[++index] == 'r' && str[++index] == 'u' && str[++index] == 'e')
                        {
                            index++;
                            result = true;
                        }
                        else
                        { 
                            throw new FormatException(string.Format("解析为布尔值为true时出错，遇到非法字符{0}!错误位置{1}", str[index-1], index-1));
                        }
                        break;
                    case 'f':
                        if (str[++index] == 'a' && str[++index] == 'l' && str[++index] == 's' && str[++index] == 'e')
                        {
                            index++;
                            result = false;
                        }
                        else
                        {
                            throw new FormatException(string.Format("解析为布尔值为false时出错，遇到非法字符{0}!错误位置{1}", str[index - 1], index - 1));
                        }
                        break;

                    default:
                        result = ParseDouble(str, ref index);
                        break;
                }
            }
            return result;
        }

        /// <summary>
        /// 将字符串反序列化为对象
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static object Decode(string str)
        {
            int index = 0;
            return ParseObject(str, ref index);
        }

        #endregion
    }
}