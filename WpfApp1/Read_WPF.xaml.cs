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
using System.Windows.Shapes;
using System.Threading;

namespace EBook_Reader
{
    /// <summary>
    /// Window2.xaml 的交互逻辑
    /// </summary>
    public partial class Read_WPF : Window
    {
        Book book;
        delegate void ModText(string txt, int page);
        public Read_WPF(string[] binf)
        {
            book = new Book(binf, WindTool.des_key, WindTool.client.GetStream());
            InitializeComponent();
            label_bn.Content = book.GetBName();
            cur_content.Text = book.GetCont();
        }

        private void lpbutton_click(object sender, RoutedEventArgs e)
        {
            int cur = book.GetCurPageNum();
            if (cur == 1)
            {
                return;
            }
            Thread thup = new Thread(new ParameterizedThreadStart(Update));
            npbutton.IsEnabled = false;
            lpbutton.IsEnabled = false;
            thup.Start(-1);
        }

        private void npbutton_click(object sender, RoutedEventArgs e)
        {
            int cur = book.GetCurPageNum();
            if (cur == book.GetPCount())
            {
                return;
            }
            Thread thup = new Thread(new ParameterizedThreadStart(Update));
            npbutton.IsEnabled = false;
            lpbutton.IsEnabled = false;
            thup.Start(1);
        }

        private void Update(object p)//更新当前阅读内容
        {
            ModText mt = new ModText(SetContent);
            Page pge;
            string txt;
            int cur = book.GetCurPageNum(), page = (int)p;
            if (page > 0)
            {
                book.Next();
            }
            else if(page <0)
            {
                book.Last();
            }
            if ((pge = book.GetCurPage()) == null)
            {
                txt = "出现未知错误！未能获取当前页";
            }
            else
            {
                //page_label.Content = "当前在" + book.GetCurPageNum() + "页";
                //cur_content.Text = book.GetCont();
                txt = pge.GetContent();
            }
            //mt(txt, cur + page);
            this.Dispatcher.Invoke(mt, txt, cur + page);
        }

        private void SetContent(string s, int p)//设置
        {
            page_label.Content = "当前在" + p + "页";
            cur_content.Text = s;
            lpbutton.IsEnabled = true;
            npbutton.IsEnabled = true;
        }
    }
}
