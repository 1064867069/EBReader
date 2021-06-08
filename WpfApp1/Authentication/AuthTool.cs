using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Kerberos.Authentication
{
    class AuthTool
    {
        public const string c_as = "000";
        public const string as_c = "001";
        public const string c_tgs = "010";
        public const string tgs_c = "011";
        public const string c_v = "100";
        public const string v_c = "101";
        public const string s_c_v = "110";
        public const string s_v_c = "111";
        public const int bufsize = 8192;
        public const int as_port = 9000;
        public const int tgs_port = 9000;
        public const int v_port = 8000;

        public const string idtgs = "shenruijie";
        public const string idv = "xiongguozheng";

        public const string asnet = "192.168.43.218";
        public const string tgsnet = "192.168.43.121";
        public const string vnet = "192.168.43.66";
        static public IPAddress GetInternalIP()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in nics)
            {
                foreach (var uni in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (uni.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return uni.Address;
                    }
                }
            }
            return null;
        }

        public static int GetIpLast(string ip)//获取IP地址最后一位
        {
            return Convert.ToInt32(ip.Split(".".ToCharArray(),4)[3]);
        }

        public static string GetAthctor(string id, string myip, RSA.RSA rsa)//生成认证符
        {
            string result;
            StringBuilder sb = new StringBuilder();
            sb.Append(id);
            sb.Append("%");
            sb.Append(myip);
            sb.Append("%");
            V_Auth.TS5 = DateTime.Now.ToString();
            sb.Append(V_Auth.TS5);
            result = rsa.RSAEn(sb.ToString());
            return result;
        }

        public static string GetTxt(string ticket, string athctor, string id = "")//获取正文内容
        {
            StringBuilder sb = new StringBuilder();
            if (id.Length > 0)
            {
                sb.Append(id + "%");
            }
            sb.Append(ticket + "%");
            sb.Append(athctor);
            return sb.ToString();
        }

        public static string ModTick(string ticket)//修正Ticket
        {
            string modt;
            Regex rgx = new Regex("[0-9,A-F]+");
            modt = rgx.Match(ticket).Value;
            //Console.WriteLine(modt);
            return modt;
        }

        public static byte[] GetHead(int len, string myip, string type, string desip)//获取报头
        {
            byte[] result = new byte[4];
            int l1, l2;
            string headbstr;//TGS的IP地址，报头二进制形式字符串
            StringBuilder headbin = new StringBuilder();
            headbin.Append(type);//报文类型
            headbin.Append(Convert.ToString(len, 2).PadLeft(13, '0'));//报文长度

            l1 = Convert.ToInt32(myip.Split(".".ToCharArray())[3]);
            l2 = Convert.ToInt32(desip.Split(".".ToCharArray())[3]);


            headbin.Append(Convert.ToString(l1, 2).PadLeft(8, '0'));
            headbin.Append(Convert.ToString(l2, 2).PadLeft(8, '0'));

            headbstr = headbin.ToString();

            for (int i = 0; i < 4; i++)
            {
                result[i] = Convert.ToByte(headbstr.Substring(i * 8, 8), 2);
            }

            return result;
        }
    }
}
