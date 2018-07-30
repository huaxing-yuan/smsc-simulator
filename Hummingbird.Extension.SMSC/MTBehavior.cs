using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Extension.SMSC
{
    /// <summary>
    /// Changes the behavior that SMS-C simulator behaves when an MT message is received.
    /// </summary>
    public enum MTBehavior
    {
        /// <summary>
        /// Reply with a MT ACK
        /// </summary>
        ACK,
        /// <summary>
        /// Reply with a MT NACK
        /// </summary>
        NACK,
        /// <summary>
        /// Does not reply the MT
        /// </summary>
        Nothing
    }
}
