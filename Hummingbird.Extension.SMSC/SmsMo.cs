using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Hummingbird.Extension.SMSC
{
    /// <summary>
    /// A SMS Mobile Originated message. This message represents a SMS is send from a terminal to a Large Account.
    /// </summary>
    [Serializable]
    [DataContract]
    public class SmsMo
    {
        /// <summary>
        /// Gets or sets the sender of this SMS MO
        /// </summary>
        /// <value>
        /// The sender.
        /// </value>
        [DataMember]
        public string Sender { get; set; }
        /// <summary>
        /// Gets or sets the receiver of this SMS MO
        /// </summary>
        /// <value>
        /// The receiver.
        /// </value>
        [DataMember]
        public string Receiver { get; set; }
        /// <summary>
        /// Gets or sets the message text.
        /// </summary>
        /// <value>
        /// The message text.
        /// </value>
        [DataMember]
        public string MessageText { get; set; }
        /// <summary>
        /// Gets or sets the message format.
        /// </summary>
        /// <value>
        /// The message format.
        /// </value>
        [DataMember]
        public MessageFormat MessageFormat { get; set; }

        /// <summary>
        /// Gets or sets the custom UDH (User Defined Header).
        /// </summary>
        /// <value>
        /// The custom UDH.
        /// </value>
        [DataMember]
        public string CustomUDHHeader { get; set; }
    }
}
