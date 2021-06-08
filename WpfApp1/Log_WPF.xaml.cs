using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Kerberos.Authentication;
using System.Diagnostics;
using System.Windows.Forms;

namespace EBook_Reader
{
    public class LogState//登录状态
    {
        public string[] uinf;
        public string as_auth;//AS认证
        public string tgs_auth;//TGS认证
        public string v_auth;//应用服务器认证，也就是共用的密钥
        public TcpClient client;
        public bool judge_enble;//可以开始验证

        public LogState(string[] u, TcpClient c)
        {
            as_auth = null;
            tgs_auth = null;
            v_auth = null;
            uinf = u;
            client = c;
            judge_enble = false;
        }

        public string GetState()
        {
            if (as_auth == null)
                return "AS响应超时！";
            if (as_auth.Equals("error"))
                return "账号不存在！";
            if (tgs_auth == null)
                return "TGS响应超时！";
            if (tgs_auth.Equals("2"))
                return "密码错误！";
            if (v_auth == null)
                return "应用服务器响应超时！";
            return "登录成功！";
        }

       
    }
      
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class Log_WPF : Window
    {
        string myip, desv;
        IPAddress addr;
        IPEndPoint ep;
        TcpClient client;
        Thread logth, timeth;

        delegate void ModLogbox_delegate(LogState logs);
        delegate void ModLoginf_delegate(string s);
        delegate void OpenAll_delegate();
        delegate void IfLog_delegate();

        public Log_WPF()
        {
            myip = AuthTool.GetInternalIP().ToString();
            InitializeComponent();
        }


        public void StateChk(object state)//计时
        {
            LogState logs = state as LogState;
            Stopwatch stp = new Stopwatch();
            ModLogbox_delegate mb = new ModLogbox_delegate(Output_State);
            ModLoginf_delegate mi = new ModLoginf_delegate(Output_Loginf);
            OpenAll_delegate op = new OpenAll_delegate(OpenAll);
            IfLog_delegate il = new IfLog_delegate(IfLog);
            stp.Start();
            while (stp.ElapsedMilliseconds < 20000)
            {
                if (logs.judge_enble)
                {
                    if (logs.v_auth != null ||logs.as_auth.Equals("error") || logs.tgs_auth.Equals("2"))
                        break;
                }
                
            }

            if (logth.IsAlive)
                logth.Abort();

            this.Dispatcher.Invoke(op);
            //OpenAll();//恢复按钮和输入框

            this.Dispatcher.Invoke(mi, logs.GetState());
            //Output_State(logs);
            this.Dispatcher.Invoke(mb, logs);
            this.Dispatcher.Invoke(il);

            //if (Loginf.Text.Equals("登录成功！"))
            //{
            //    WindTool.des_key = logs.v_auth;
            //}
                //if (Loginf.Text.Equals("登录成功！"))
                //{
                //    Window win = new Window1();
                //    win.Show();

                //    this.Close();
                //}
        }
        private void Log_Click(object sender, RoutedEventArgs e)
        {
            string[] uinf = new string[2];
            uinf[0] = user.Text;
            uinf[1] = paswd.Password;
            BanAll();//禁用按钮以及输入框
            if (uinf[0].Length == 0 || uinf[1].Length == 0)
            {
                OpenAll();
                Loginf.Text = "账号密码不能为空！";
                return;
            }
            Loginf.Text = "登录中.......................";
            addr = IPAddress.Parse(AuthTool.vnet);
            ep = new IPEndPoint(addr, AuthTool.v_port);
            client = new TcpClient();

            try
            {
                client.Connect(ep);
            }
            catch (SocketException exp)
            {
                OpenAll();
                Loginf.Text = "应用服务器连接失败！";
                return;
            }

            logth = new Thread(new ParameterizedThreadStart(WindTool.Auth));
            LogState logs = new LogState(uinf, client);
            timeth = new Thread(new ParameterizedThreadStart(StateChk));
            logs.judge_enble = false;
            logth.Start(logs);
            timeth.Start(logs);

           
        }

        void Output_State(object state)
        {
            LogState logs = state as LogState;
            StringBuilder sb = new StringBuilder();
            if (logs.as_auth != null)
            {
                sb.Append("AS发来的报文信息" + logs.as_auth);
            }
            if (logs.tgs_auth != null)
            {
                sb.Append("\nTGS发来的报文信息" + logs.tgs_auth);
            }
            if (logs.v_auth != null)
            {
                sb.Append("\nV发来的秘钥" + logs.v_auth);
            }
            logBox.Text = sb.ToString();
        }

        void Output_Loginf(string s)
        {
            Loginf.Text = s;
        }

        private void BanAll()//禁用所有可造成影响的控件
        {
            user.IsEnabled = false;
            paswd.IsEnabled = false;
            LogButton.IsEnabled = false;
            RegButton.IsEnabled = false;
        }

        private void OpenAll()//打开所有可造成影响的控件
        {
            user.IsEnabled = true;
            paswd.IsEnabled = true;
            LogButton.IsEnabled = true;
            RegButton.IsEnabled = true;
        }

        private void IfLog()//是否登录
        {
            if (Loginf.Text.Equals("登录成功！"))
            {
                WindTool.myid = user.Text;
                Window searchwin = new Search_WPF();
                searchwin.Show();

                //this.Close();
            }
        }
    }
}
