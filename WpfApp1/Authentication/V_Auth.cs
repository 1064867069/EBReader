using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Kerberos.Authentication
{
    //认证应用服务器
    class V_Auth
    {
        //与V认证，成功返回共用秘钥
        public static string TS5 = "";
        public static string Auth(string tgs_auth,string myid, string myip, TcpClient client)
        {
            string[] pauth = tgs_auth.Split("%".ToCharArray()), key = pauth[0].Split(",".ToCharArray());
            string idv = pauth[1], ts4 = pauth[2], ts5;//TGS的id，票据发放时间戳
            string Ticketv = AuthTool.ModTick(pauth[4]);//票据
            string txt, head, athctor;
            byte[] hbytes;
            int kn = Convert.ToInt32(key[0]), kde = Convert.ToInt32(key[1]);//RSA秘钥
            int lft = Convert.ToInt32(pauth[3]), ipend = AuthTool.GetIpLast(myip);
            int i, n;

            RSA.RSA krsa = new RSA.RSA(kn, kde);
            //IPAddress addr = IPAddress.Parse(AuthTool.tgsnet);
            //IPEndPoint ep = new IPEndPoint(addr, 7000);
            //TcpClient client = new TcpClient();
            //client.Connect(ep);
            NetworkStream ns = client.GetStream();
            StreamWriter nsw;
            StreamReader nsr;
            DateTime now;//用来记log

            athctor = AuthTool.GetAthctor(myid, myip, krsa);
            txt = AuthTool.GetTxt(Ticketv, athctor);
            hbytes = AuthTool.GetHead(txt.Length, myip,AuthTool.c_v,AuthTool.vnet);

            nsw = new StreamWriter(ns);
            nsr = new StreamReader(ns);
            foreach (byte b in hbytes)
            {
                nsw.Write(Convert.ToString(b, 16).PadLeft(2, '0').ToUpper());
            }
            nsw.Write(txt);
            nsw.Flush();

            int len;
            string lenstr, recvstr;
            byte[] recv;

            while (true)
            {
                for (i = 0; i < 4; i++)
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
                if (hbytes[0] < 160 || hbytes[0] > 191 || hbytes[3] != ipend)//需改进
                {
                    Console.WriteLine("hbytes[0] = " + hbytes[0] + "\nhbytes[3] = " + hbytes[3]);
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
                pauth = krsa.RSADe(recvstr).Split("%".ToCharArray());
                if (!Auth_V(TS5, pauth[1]))//认证AS失败
                {
                    Console.WriteLine("V发来的时间不对！");
                    
                    Console.WriteLine("TS5 = " + pauth[1]);
                    continue;
                }
                break;
            }

            return pauth[0];
        }

        static bool Auth_V(string ts, string pts)//通过时间戳认证应用服务器
        {
            DateTime dts = DateTime.Parse(ts), pdts = DateTime.Parse(pts);
            TimeSpan span = pdts.Subtract(dts);
            int sec = (int)span.TotalSeconds;
            if (sec == 1)
                return true;
            else
                return false;
        }
    }
}
