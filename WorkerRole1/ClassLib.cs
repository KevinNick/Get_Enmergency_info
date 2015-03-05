using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class OpenDataInfo
    {
        public string openid { get; set; }
        public string title { get; set; }
        public string updated { get; set; }
        public string summary { get; set; }
    }

    public class Machine
    {
        public string CO { get; set; }
        public string Liquidgas { get; set; }
        public string NaturalGas { get; set; }
        public string Smoke { get; set; }
    }
}
