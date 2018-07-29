using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extension.SMSC
{
    /// <summary>
    /// The type of EMI Message that supported in this extension
    /// </summary>
    public enum EmiMessageType
    {

        /// <summary>
        /// The system
        /// </summary>
        SYS,
        /// <summary>
        /// The session
        /// </summary>
        SESSION,
        /// <summary>
        /// The session ack
        /// </summary>
        SESSION_ACK,
        /// <summary>
        /// The ping
        /// </summary>
        PING,
        /// <summary>
        /// The ping ack
        /// </summary>
        PING_ACK,
        /// <summary>
        /// The mt
        /// </summary>
        MT,
        /// <summary>
        /// The mt ack
        /// </summary>
        MT_ACK,
        /// <summary>
        /// The mo
        /// </summary>
        MO,
        /// <summary>
        /// The mo ack
        /// </summary>
        MO_ACK,
        /// <summary>
        /// The sr
        /// </summary>
        SR,
        /// <summary>
        /// The sr ack
        /// </summary>
        SR_ACK,
        /// <summary>
        /// The connect
        /// </summary>
        CONNECT,
        /// <summary>
        /// The disconnect
        /// </summary>
        DISCONNECT,
        /// <summary>
        /// The mt nack
        /// </summary>
        MT_NACK,
        /// <summary>
        /// The mo nack
        /// </summary>
        MO_NACK,
        /// <summary>
        /// The sr nack
        /// </summary>
        SR_NACK,
    }
}
