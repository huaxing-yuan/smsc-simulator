using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Hummingbird.Extension.SMSC { 
    [Serializable]
    [DataContract]
    public class SmsMo
    {
        [DataMember]
        public string Sender { get; set; }
        [DataMember]
        public string Receiver { get; set; }
        [DataMember]
        public string MessageText { get; set; }
        [DataMember]
        public MessageFormat MessageFormat { get; set; }
        [DataMember]
        public string CustomUDHHeader { get; set; }
    }
}
