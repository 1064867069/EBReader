using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kerberos.Authentication;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace EBook_Reader
{
    class Book
    {
        private string uname;//用户名
        private string name;//书名
        private string athor;//作者
        private string myip;
        private string des_key;//与应用服务器交互的秘钥
        private int page_count;//页数
        private Page plast;//上一页
        private Page pcur;//当前页
        private Page pnext;//下一页
        private NetworkStream binfstream;

        public Book(string[] binf, string key, NetworkStream ns)
        {
            uname = WindTool.myid;
            myip = WindTool.myip;
            name = binf[0];
            athor = binf[1];
            des_key = key;
            page_count = Convert.ToInt32(binf[2]);
            binfstream = ns;
            plast = pcur = pnext = null;
            if (page_count > 0)
                pcur = ReqPage(1);
            if (page_count > 1)
                pnext = ReqPage(2);
        }

        public string GetBName()//获取书名
        {
            return name;
        }

        public string GetCont()//获取当前页内容
        {
            return pcur.GetContent();
        }

        public Page GetCurPage()
        {
            return pcur;
        }

        public int GetCurPageNum()//获取当前页码
        {
            return pcur.GetPnum();
        }
        public int GetPCount()//获取页数
        {
            return page_count;
        }

        public string RepCont(string reply, int pnum)//从服务端回复中获取电子书当前页内容,若信息不正确返回null
        {
            string[] prep = reply.Split("%".ToCharArray());//C%书名%页码%内容
            int reppnum = Convert.ToInt32(prep[2]);
            if (prep.Length != 4||prep[3].Equals("error"))
                return null;
            if (prep[0].Equals("C") && prep[1].Equals(name) && pnum == reppnum)
                return prep[3];
            return null;
        }

        public Page ReqPage(int num)//获取第num页
        {
            StringBuilder sb = new StringBuilder();
            string req, lenstr, recvstr, destr, cont;
            byte[] head, recv;
            int i, n, len, ipend = Convert.ToInt32(myip.Split(".".ToCharArray())[3]);
            sb.Append(uname + "%" + "C" + "%" + name + "%" + num);
            req = DES.Tool.txtDES(sb.ToString(), true, des_key);
            head = WindTool.GetHead(req.Length, AuthTool.s_c_v);

            StreamWriter sw = new StreamWriter(binfstream);
            foreach (byte b in head)
            {
                sw.Write(Convert.ToString(b, 16).PadLeft(2, '0').ToUpper());
            }
            sw.Write(req);
            sw.Flush();

            StreamReader sr = new StreamReader(binfstream);

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

            if ((cont = RepCont(destr, num)) != null)
                return new Page(num, cont);
            else
                return null;

        }
        public void Next()//下一页
        {
            if (pcur.GetPnum() == page_count)//当前页是最后一页
                return;
            plast = pcur;
            pcur = pnext;
            pnext = ReqPage(pcur.GetPnum() + 1);
        }

        public void Last()//上一页
        {
            int curnum = pcur.GetPnum();
            if (curnum == 1)
                return;
            pnext = pcur;
            pcur = plast;
            if (curnum == 2)
                plast = null;
            else
                plast = ReqPage(pcur.GetPnum() - 1);
        }
    }
}
