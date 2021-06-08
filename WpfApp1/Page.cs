using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBook_Reader
{
    class Page
    {
        int page;//页码
        string content;//内容

        public Page(int pnum, string ct)
        {
            page = pnum;
            content = ct;
        }

        public int GetPnum()//页码
        {
            return page;
        }

        public string GetContent()
        {
            return content;
        }
    }
}
