using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Hummingbird.Extension.SMSC
{
    [Serializable]
    [DataContract]
    public class SmsSr
    {
        [DataMember]
        public string OAdC { get; set; }
        [DataMember]
        public string AdC { get; set; }
        [DataMember]
        public string Text { get; set; }
        [DataMember]
        public string SCTS { get; set; }
        [DataMember]
        public int Dst { get; set; }
        [DataMember]
        public int Rsn { get; set; }
    }
}
