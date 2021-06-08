using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kerberos.Authentication;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;
namespace EBook_Reader
{
    //窗体需要用到的其他函数
    class WindTool
    {
        public static TcpClient client;
        public static string myip, myid, des_key;
        public static void Auth(object state)//进行Kerberos认证
        {
            myid = myip = des_key = null;
            LogState logs = state as LogState;
            myip = AuthTool.GetInternalIP().ToString();
            string[] uinf = logs.uinf;
            client = logs.client;
            logs.as_auth = AS_Auth.Auth(uinf, AuthTool.v_port, AuthTool.idtgs);
            if (logs.as_auth.Equals("error"))
            {
                return;
            }
            logs.tgs_auth = TGS_Auth.Auth(logs.as_auth, uinf[0], myip);
            logs.judge_enble = true;
            if (logs.tgs_auth.Length == 1)
                return;
            logs.v_auth = V_Auth.Auth(logs.tgs_auth, uinf[0], myip, client);
            des_key = logs.v_auth;
            return;
        }

        //public static void Timming(object state)//计时
        //{
        //    LogState logs = state as LogState;
        //    Stopwatch stp = new Stopwatch();
        //    stp.Start();
        //    while (stp.ElapsedMilliseconds < 10000)
        //    {
        //        if (logs.v_auth != null)
        //            break;
        //        //logBox.Text = Convert.ToString(stp.ElapsedMilliseconds);
        //        //Console.WriteLine(stp.ElapsedMilliseconds);
        //    }
        //}


        public static byte[] GetHead(int len, string type)
        {
            byte[] result = new byte[4];
            int l1, l2;
            string headbstr;//TGS的IP地址，报头二进制形式字符串
            StringBuilder headbin = new StringBuilder();
            headbin.Append(type);//报文类型
            headbin.Append(Convert.ToString(len, 2).PadLeft(13, '0'));//报文长度

            l1 = Convert.ToInt32(myip.Split(".".ToCharArray())[3]);
            l2 = Convert.ToInt32(AuthTool.vnet.Split(".".ToCharArray())[3]);


            headbin.Append(Convert.ToString(l1, 2).PadLeft(8, '0'));
            headbin.Append(Convert.ToString(l2, 2).PadLeft(8, '0'));

            headbstr = headbin.ToString();

            for (int i = 0; i < 4; i++)
            {
                result[i] = Convert.ToByte(headbstr.Substring(i * 8, 8), 2);
            }

            return result;
        }

        //通过关键词和作者名获取相关的所有书本信息
        //返回格式为"名字,作者，页数#名字,作者,页数#....."
        public static string[] GetBookInfo(string kw, string author)
        {
            if (des_key == null)
                return null;
            Console.WriteLine("开始发送请求！");
            int i, len, n, ipend = Convert.ToInt32(myip.Split(".".ToCharArray())[3]);
            string req, recvstr, destr, lenstr,liststr;
            string[] preply;
            byte[] head, recv;
            NetworkStream ns = client.GetStream();
            req = myid + "%I%" + kw + "%" + author;
            req = DES.Tool.txtDES(req, true, des_key);
            head = GetHead(req.Length, AuthTool.s_c_v);

            StreamWriter sw = new StreamWriter(ns);
            foreach (byte b in head)
            {
                sw.Write(Convert.ToString(b, 16).PadLeft(2, '0').ToUpper());
            }
            sw.Write(req);
            sw.Flush();

            Console.WriteLine("书籍信息请求发送完毕！");

            StreamReader sr = new StreamReader(ns);

            while (true)
            {
                for (i = 0; i < 4; i++)
                {
                    n = sr.Read();
                    if (n <= '9' && n >= '0')
                    {
                        n -= '0';
                        //i++;
                        //Console.WriteLine(now.ToString() + "   读到一个数据");
                    }
                    else if (n <= 'E' && n >= 'A')
                    {
                        n -= ('A' - 10);
                        //i++;
                        //Console.WriteLine(now.ToString() + "   读到一个数据");
                    }
                    else
                    {
                        //Console.WriteLine(now.ToString() + "   读到一个数据");
                        //Thread.Sleep(1000);
                        continue;
                    }

                    head[i] = (byte)(n * 16);
                    // Console.WriteLine("n = " + n);
                    if ((n = sr.Read()) <= '9')
                        n -= '0';
                    else
                        n -= ('A' - 10);
                    //Console.WriteLine("n = " + n);
                    head[i] = (byte)(n + head[i]);
                    //Console.WriteLine("htemp["+i+"]="+htemp[i]);
                }
                if (head[0] < 224 || head[3] != ipend)//需改进
                {
                    Console.WriteLine("head[0] = " + head[0] + "\nhead[3] = " + head[3]);
                    Console.WriteLine("接收方地址错误！\n");
                    continue;
                }
                break;
            }

            lenstr = (Convert.ToString(head[0], 2).PadLeft(8, '0') + Convert.ToString(head[1], 2).PadLeft(8, '0')).Substring(3);
            //Console.WriteLine("lenstr = " + lenstr);
            len = Convert.ToInt32(lenstr, 2);
            recv = new byte[len];
            for (i = 0; i < len; i++)
            {
                recv[i] = (byte)sr.Read();
            }
            recvstr = Encoding.ASCII.GetString(recv);
            destr = DES.Tool.txtDES(recvstr, false, des_key);
            preply = destr.Split("%".ToCharArray());

            Console.WriteLine("书籍信息回复接收完毕！");

            liststr = "书名,作者,页数#" + preply[2];

            if (preply.Length != 3||preply[2].Length==0)
                return null;
            if (!preply[0].Equals(myid)||!preply[1].Equals("I")||preply[2].Contains("error"))
                return null;
            return preply[2].Split("#".ToCharArray());
        }

        public static string[] del_bspace(string s)//去掉所有空格符并分割
        {
            string[] temp = s.Split(" ".ToCharArray());
            List<string> result = new List<string>();

            foreach (string str in temp)
            {
                if (str.Length > 0)
                    result.Add(str);
            }
            return result.ToArray();

        }
        //public void 
    }
}
