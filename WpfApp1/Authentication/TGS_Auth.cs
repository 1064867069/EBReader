using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Kerberos.Authentication
{
    class TGS_Auth
    {
        static string TS2 = "";
        static string DeadLine = "";
     
        public static string Auth(string as_auth, string myid, string myip)//向TGS认证
        {
            string[] pauth = as_auth.Split("%".ToCharArray()), key = pauth[0].Split(",".ToCharArray());
            if(pauth.Length!=5||key.Length!=2||!pauth[1].Equals(AuthTool.idtgs))//信息数不对，头一个不是RSA，tgsid不对
            {
                return "2";//密码错误
            }
            string idtgs = pauth[1], ts2 = pauth[2];//TGS的id，票据发放时间戳
            string Tickettgs = AuthTool.ModTick(pauth[4]);//票据
            string txt, head, athctor;
            byte[] hbytes;
            int kn = Convert.ToInt32(key[0]), kde = Convert.ToInt32(key[1]);//RSA秘钥
            int lft = Convert.ToInt32(pauth[3]), ipend = AuthTool.GetIpLast(myip);
            int i,n;

            RSA.RSA krsa = new RSA.RSA(kn, kde);
            IPAddress addr = IPAddress.Parse(AuthTool.tgsnet);
            IPEndPoint ep = new IPEndPoint(addr, 8000);
            TcpClient client = new TcpClient();
            client.Connect(ep);
            NetworkStream ns = client.GetStream();
            StreamWriter nsw;
            StreamReader nsr;
            DateTime now;//用来记log

            //Console.WriteLine("发给TGS的票据长度：" + Tickettgs.Length);
            //foreach (string s in pauth)
            //{
            //    Console.WriteLine(s);
            //}

            ModTime(ts2, lft);

            //生成票据请求信息
            athctor = AuthTool.GetAthctor(myid, myip, krsa);
            txt = AuthTool.GetTxt(Tickettgs, athctor, AuthTool.idv);
            hbytes = AuthTool.GetHead(txt.Length, myip,AuthTool.c_tgs,AuthTool.tgsnet);

            //发送票据请求信息
            nsw = new StreamWriter(ns);
            nsr = new StreamReader(ns);
            foreach (byte b in hbytes)
            {
                nsw.Write(Convert.ToString(b, 16).PadLeft(2, '0').ToUpper());
            }
            nsw.Write(txt);
            nsw.Flush();
            //nsw.Close();

            int len;
            string lenstr,recvstr;
            byte[] recv;

            while (true)
            {
                for (i = 0; i < 4;i++)
                {
                    now = DateTime.Now;
                    n = nsr.Read();
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
                        Thread.Sleep(1000);
                        continue;
                    }

                    hbytes[i] = (byte)(n * 16);
                    // Console.WriteLine("n = " + n);
                    if ((n = nsr.Read()) <= '9')
                        n -= '0';
                    else
                        n -= ('A' - 10);
                    //Console.WriteLine("n = " + n);
                    hbytes[i] = (byte)(n + hbytes[i]);
                    //Console.WriteLine("htemp["+i+"]="+htemp[i]);
                }
                if (hbytes[0]<96||hbytes[0] > 127 || hbytes[3] != ipend)//需改进
                {
                    Console.WriteLine("hbytes[0] = " + hbytes[0] + "\nhbytes[2] = " + hbytes[2]);
                    Console.WriteLine("接收方地址错误！\n");
                    continue;
                }
                lenstr = (Convert.ToString(hbytes[0], 2).PadLeft(8, '0') + Convert.ToString(hbytes[1], 2).PadLeft(8, '0')).Substring(3);
                //Console.WriteLine("lenstr = " + lenstr);
                len = Convert.ToInt32(lenstr, 2);
                recv = new byte[len];
                for (i = 0; i < len; i++)
                {
                    recv[i] = (byte)nsr.Read();
                }
                recvstr = Encoding.ASCII.GetString(recv, 0, len);
                break;
            }
            client.Close();
            return krsa.RSADe(recvstr);
        }

        static void ModTime(string ts, int lft)//TGS有效截止时间修改
        {
            DateTime dts = DateTime.Parse(ts);
            dts.AddHours(lft);
            TS2 = ts;
            DeadLine = dts.ToString();
        }

        //static string GetAthctor(string id, string myip, RSA.RSA rsa)//生成认证符
        //{
        //    string result;
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(id);
        //    sb.Append("%");
        //    sb.Append(myip);
        //    sb.Append("%");
        //    sb.Append(DateTime.Now.ToString());
        //    result = rsa.RSAEn(sb.ToString());
        //    return result;
        //}

        //static string GetTxt(string ticket, string athctor, string id = "")//获取正文内容
        //{
        //    StringBuilder sb = new StringBuilder();
        //    if (id.Length > 0)
        //    {
        //        sb.Append(id + "%");
        //    }
        //    sb.Append(ticket + "%");
        //    sb.Append(athctor);
        //    return sb.ToString();
        //}

        //static byte[] GetHead(int len, string myip)//获取报头
        //{
        //    byte[] result = new byte[4];
        //    int l1, l2;
        //    string tgsip = AuthTool.tgsnet, headbstr;//TGS的IP地址，报头二进制形式字符串
        //    StringBuilder headbin = new StringBuilder();
        //    headbin.Append(AuthTool.c_tgs);//报文类型
        //    headbin.Append(Convert.ToString(len, 2).PadLeft(13, '0'));//报文长度

        //    l1 = Convert.ToInt32(myip.Split(".".ToCharArray())[3]);
        //    l2 = Convert.ToInt32(tgsip.Split(".".ToCharArray())[3]);


        //    headbin.Append(Convert.ToString(l1, 2).PadLeft(8, '0'));
        //    headbin.Append(Convert.ToString(l2, 2).PadLeft(8, '0'));

        //    headbstr = headbin.ToString();

        //    for (int i = 0; i < 4; i++)
        //    {
        //        result[i] = Convert.ToByte(headbstr.Substring(i * 8, 8), 2);
        //    }

        //    return result;
        //}
    }
}
