using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Kerberos.Authentication
{
    class AS_Auth
    {
        //string uname;//客户端id
        //string pswd;//密码（用户口令）
        //string asinet;//ASIP地址
        //int port;
        public static string Auth(string[] uinfo, int port, string tgsid)//与AS认证
        {
            string head, txt,lenstr,recvstr;//报头，正文，收到的报文
            string asnet = AuthTool.asnet;
            byte[] htemp,recv;
            int i, maxsize = AuthTool.bufsize, len, n;
            IPAddress addr = IPAddress.Parse(asnet);
            IPEndPoint ep = new IPEndPoint(addr, 9000);
            TcpClient client = new TcpClient();
            StringBuilder ask = new StringBuilder();
            NetworkStream ns;
            StreamWriter sw;
            StreamReader sr;

            client.Connect(ep);
            Console.WriteLine("AS连接成功！\n");
            ns = client.GetStream();
            sw = new StreamWriter(ns);
            sr = new StreamReader(ns);

            txt = GetText(uinfo[0], tgsid);
            //msg = new byte[txt.Length + 4];
            htemp = Gethead(txt.Length, asnet);
            for(i = 0;i < 4;i++)
            {
                //msg[i] = htemp[i];
                sw.Write(Convert.ToString(htemp[i], 16).PadLeft(2, '0').ToUpper());
            }
            //for(;i<msg.Length;i++)
            //{
            //    msg[i] = ttemp[i - 4];
            //}

            //string s = Encoding.ASCII.GetString(ttemp);
            sw.Write(txt);
            sw.Flush();

            //Console.WriteLine(s);

            recv = new byte[maxsize];
            while (true)
            {
                try
                {
                    for (i = 0; i < 4; i++)
                    {
                        if ((n = sr.Read()) <= '9')
                            n -= '0';
                        else
                            n -= ('A' - 10);
                        htemp[i] = (byte)(n * 16);
                        // Console.WriteLine("n = " + n);
                        if ((n = sr.Read()) <= '9')
                            n -= '0';
                        else
                            n -= ('A' - 10);
                        //Console.WriteLine("n = " + n);
                        htemp[i] = (byte)(n + htemp[i]);
                        //Console.WriteLine("htemp["+i+"]="+htemp[i]);
                    }
                    if (htemp[0] > 63 || htemp[3] != 125)//需改进
                    {
                        Console.WriteLine("htemp[0] = " + htemp[0] + "\nhtemp[3] = " + htemp[3]);
                        Console.WriteLine("接收方地址错误！\n");
                        continue;
                    }
                    lenstr = (Convert.ToString(htemp[0], 2).PadLeft(8, '0') + Convert.ToString(htemp[1], 2).PadLeft(8, '0')).Substring(3);
                    //Console.WriteLine("lenstr = " + lenstr);
                    len = Convert.ToInt32(lenstr, 2);
                    for (i = 0; i < len; i++)
                    {
                        recv[i] = (byte)sr.Read();
                    }
                    recvstr = Encoding.ASCII.GetString(recv, 0, len);
                    //Console.WriteLine(recvstr);
                    //FileStream fs = new FileStream("G:\\代码\\C#\\Kerberos\\Kerberos\\Temp.txt", FileMode.Open, FileAccess.Write);
                    //StreamWriter sw1 = new StreamWriter(fs);
                    //sw1.WriteLine(DES.Tool.txtDES(recvstr, false, uinfo[1]));
                    //sw1.Close();
                    //fs.Close();
                    break;
                }
                catch(IOException e)
                {
                    return null;
                }
            }

            //sw.WriteLine(s);
            sw.Flush();
            sw.Close();
            sr.Close();
            client.Close();
            //Console.WriteLine("收到的txt长度为" + recvstr.Length);
            if (recvstr.Equals("error"))
                return recvstr;
            return DES.Tool.txtDES(recvstr,false,uinfo[1]);
        }

        static string GetText(string uname, string tgsid)//获取正文
        {
            StringBuilder sb = new StringBuilder();
            string ts = DateTime.Now.ToString();
            sb.Append(uname);
            sb.Append("%");
            sb.Append(tgsid);
            sb.Append("%");
            sb.Append(ts);

            return sb.ToString();
        }

        public static byte[] Gethead(int tlen, string asip)//获取报头
        {
            byte[] result = new byte[4];
            string headbstr;
            int l1, l2;//AS与本机的ip地址最后一位
            string myip = AuthTool.GetInternalIP().ToString();//本机地址
            StringBuilder headbin = new StringBuilder();
            headbin.Append(AuthTool.c_as);//类型
            headbin.Append(Convert.ToString(tlen, 2).PadLeft(13, '0'));//长度
            //Console.WriteLine("tlen = " + tlen);
            //Console.WriteLine("bin of tlen = " + Convert.ToString(tlen, 2).PadLeft(13, '0'));

            l1 = Convert.ToInt32(myip.Split(".".ToCharArray(), 4)[3]);
            l2 = Convert.ToInt32(asip.Split(".".ToCharArray(), 4)[3]);
            //Console.WriteLine("l1 = " + l1);
            //Console.WriteLine("l2 = " + l2);
            headbin.Append(Convert.ToString(l1, 2).PadLeft(8,'0'));
            headbin.Append(Convert.ToString(l2, 2).PadLeft(8,'0'));

            headbstr = headbin.ToString();
            //Console.WriteLine(headbstr.Length);
            for(int i = 0;i < 4;i++)
            {
                result[i] = Convert.ToByte(headbstr.Substring(i * 8, 8), 2);
            }

            return result;
        }
    }
}
