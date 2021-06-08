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
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Search_WPF : Window
    {
        delegate void ModLBox(List<string> list);//修改列表框内容
        public Search_WPF()
        {
            InitializeComponent();
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            string kw = BNameKw.Text, author = Author.Text;
            Tuple<string, string> req = new Tuple<string, string>(kw, author);
            Thread thsch = new Thread(new ParameterizedThreadStart(Search));

            SearchBtn.IsEnabled = false;
            Sch_Lbox.IsEnabled = false;
            thsch.Start(req);
        }

        void Search(object state)
        {
            Tuple<string, string> req = state as Tuple<string, string>;
            string kw = req.Item1, author = req.Item2;
            Console.WriteLine("kw=" + kw + "\nauthor=" + author);
            string[] binfo = WindTool.GetBookInfo(kw, author), temp;
            ModLBox mlb = new ModLBox(Update_LBox);
            List<string> blist = new List<string>();
            if (binfo == null)
            {
                this.Dispatcher.Invoke(mlb, blist);
                return;
            }
            int bnlen = 50, athlen = 30, i;
            StringBuilder sb = new StringBuilder();
            for (i = 0; i < binfo.Length; i++)
            {
                temp = binfo[i].Split(",".ToCharArray());
                sb.Append(temp[0].PadRight(50, ' '));
                sb.Append(temp[1].PadRight(30, ' '));
                sb.Append(temp[2].PadRight(10, ' '));
                binfo[i] = sb.ToString();
                sb.Clear();
            }
            blist = new List<string>(binfo);
            this.Dispatcher.Invoke(mlb, blist);

        }

        private void Update_LBox(List<string> blist)
        {
            int i;
            if (blist != null)
            {
                Sch_Lbox.ItemsSource = blist;
                //Sch_Lbox.Background = new SolidColorBrush(Colors.LightGray);

                SolidColorBrush brush1, brush2;
                ListBoxItem lbi = (ListBoxItem)(Sch_Lbox.ItemContainerGenerator.ContainerFromIndex(0));//第一项也就是属性项
                if (lbi != null)
                {
                    lbi.FontWeight = FontWeights.Bold;
                    lbi.IsEnabled = false;
                    brush1 = new SolidColorBrush(Colors.LightSkyBlue);
                    brush2 = new SolidColorBrush(Colors.LightPink);

                    for (i = 1; i < Sch_Lbox.Items.Count; i++)
                    {
                        lbi = (ListBoxItem)(Sch_Lbox.ItemContainerGenerator.ContainerFromIndex(i));
                        if (i % 2 == 1)
                            lbi.Background = brush1;
                        else
                            lbi.Background = brush2;
                    }
                }
            }
            SearchBtn.IsEnabled = true;
            Sch_Lbox.IsEnabled = true;
        }

        private void Sch_Lbox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string select_val = ((ListBoxItem)(Sch_Lbox.SelectedItem)).ToString();
            string[] binf = WindTool.del_bspace(select_val);
            Read_WPF win2 = new Read_WPF(binf);
        }
    }

   
}
