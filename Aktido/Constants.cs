using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aktido { 

    class Kind
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    class Estate
    {
        public int id { get; set; }
        public string name { get; set; }
        public int count { get; set; }
    }

    class Constants
    {
        public const int query_limit = 10;
    }
}
