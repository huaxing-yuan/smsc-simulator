using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Hummingbird.Extension.SMSC
{
    /// <summary>
    /// A SMS DeliveryReport message. This message is sent from terminal to the Large Account to report the previous MT message is delivered or read.
    /// </summary>
    [Serializable]
    [DataContract]
    public class SmsSr
    {
        /// <summary>
        /// Gets or sets the Original Address
        /// </summary>
        /// <value>
        /// The o ad c.
        /// </value>
        [DataMember]
        public string OAdC { get; set; }
        /// <summary>
        /// Gets or sets the Destination Address
        /// </summary>
        /// <value>
        /// The ad c.
        /// </value>
        [DataMember]
        public string AdC { get; set; }
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        [DataMember]
        public string Text { get; set; }
        /// <summary>
        /// Gets or sets the SMS Time Stamp
        /// </summary>
        /// <value>
        /// The SCTS.
        /// </value>
        [DataMember]
        public string SCTS { get; set; }
        
        /// <summary>
        /// Gets or sets the Status of the delivery report.
        /// </summary>
        /// <value>
        /// The DST.
        /// </value>
        [DataMember]
        public int Dst { get; set; }
        /// <summary>
        /// Gets or sets the reason code
        /// </summary>
        /// <value>
        /// The RSN.
        /// </value>
        [DataMember]
        public int Rsn { get; set; }
    }
}
