﻿using Hummingbird.TestFramework.Messaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Hummingbird.Extension.SMSC
{
    /// <summary>
    /// EMIClass.Message (SMS Message) that is used to represent a EMI-TCP message
    /// </summary>
    public class EmiMessage
    {
        /// <summary>
        /// Gets or sets the type of the message.
        /// </summary>
        /// <value>
        /// The type of message,
        /// </value>
        public EmiMessageType Type { get; set; }

        private string rawmessage;

        internal static string GetFriendlyMessage(string rawmessage)
        {
            string friendlyMessage = string.Empty;
            string op = "0";
            string to ;
            string from ;
            string direction = "O";
            try
            {
                string[] m = rawmessage.Split('/');
                op = m[3];
                to = m[4];
                from = m[5];
                direction = m[2];
                string hexmessage = m[24];
                string message = null;
                string XSER = m[34];
                string MT4 = m[22];
                if (XSER.Contains("020108") || XSER.Contains("020109") || XSER.Contains("02010A") || XSER.Contains("02010B") || XSER.Contains("020118") || XSER.Contains("020119") || XSER.Contains("02011A") || XSER.Contains("02011B") || XSER.Contains("020128") || XSER.Contains("020129") || XSER.Contains("02012A") || XSER.Contains("02012B") || XSER.Contains("020138") || XSER.Contains("020139") || XSER.Contains("02013A") || XSER.Contains("02013B"))
                {
                    //Unicode
                    message = System.Text.UnicodeEncoding.BigEndianUnicode.GetString(EmiProtocol.Decode(hexmessage.ToCharArray()));
                }
                else
                {
                    //GSM8BIT / GSM7BIT
                    message = EmiProtocol.GSM8HexToString(hexmessage);
                }
                int shortcode;
                if (op == "51")
                {
                    if (int.TryParse(from, out shortcode))
                    {
                        if (m[5] != string.Empty) friendlyMessage = string.Format("SMS: {0} -> {1} : {2} ({3} chars)", from, to, message, message.Length);
                    }
                    else
                    {
                        from = EmiProtocol.GSM7HexToString(from.Substring(2)).Substring(0, int.Parse(from.Substring(0, 2), NumberStyles.HexNumber) * 8 / 14);
                        if (m[5] != string.Empty) friendlyMessage = string.Format("SMS: {0} -> {1} : {2} ({3} chars)", from, to, message, message.Length);
                    }
                }
                else
                {
                    if (int.TryParse(to, out shortcode))
                    {
                        if (m[5] != string.Empty) friendlyMessage = string.Format("SMS: {0} -> {1} : {2} ({3} chars)", from, to, message, message.Length);
                    }
                    else
                    {
                        to = EmiProtocol.GSM7HexToString(to.Substring(2)).Substring(0, int.Parse(to.Substring(0, 2), NumberStyles.HexNumber) * 8 / 14);
                        if (m[5] != string.Empty) friendlyMessage = string.Format("SMS: {0} -> {1} : {2} ({3} chars)", from, to, message, message.Length);
                    }
                }
            }
            catch
            {
                if (rawmessage[0] == '\x02') friendlyMessage = string.Empty;
                else friendlyMessage = "SMS: " + rawmessage;

                if (op == "60" && direction == "O")
                {
                    friendlyMessage = "SMS: Open Session Request";
                }
                else if (op == "60" && direction == "R")
                {
                    friendlyMessage = "SMS: Open Session Acknowledgment";
                }
                else if (op == "51" && direction == "R")
                {
                    friendlyMessage = "SMS: MT Acknowledgment";
                }
                else if (op == "51" && direction == "O")
                {
                    friendlyMessage = "SMS: Empty MT (Ping)";
                }
                else if (op == "52")
                {
                    friendlyMessage = "SMS: MO Acknowledgment";
                }
                else if (op == "53")
                {
                    friendlyMessage = "SMS: SR Acknowledgment";
                }


            }
            return friendlyMessage;
        }


        /// <summary>
        /// The original UCP / EMI Trame of the current SMS message.
        /// </summary>
        /// <value>
        /// The raw message.
        /// </value>
        public string RAWMessage
        {
            get
            {
                return rawmessage;
            }
            set
            {
                string op = "0";
                string direction = "O";
                rawmessage = value;
                try
                {
                    FriendlyMessage = GetFriendlyMessage(rawmessage);
                }
                catch
                {
                    if (value[0] == '\x02') FriendlyMessage = string.Empty;
                    else FriendlyMessage = "SMS: " + RAWMessage;

                    if (op == "60" && direction == "O")
                    {
                        FriendlyMessage = "SMS: Open Session Request";
                    }
                    else if (op == "60" && direction == "R")
                    {
                        FriendlyMessage = "SMS: Open Session Acknowledgment";
                    }
                    else if (op == "51" && direction == "R")
                    {
                        FriendlyMessage = "SMS: MT Acknowledgment";
                    }
                    else if (op == "51" && direction == "O")
                    {
                        FriendlyMessage = "SMS: Empty MT (Ping)";
                    }
                    else if (op == "52")
                    {
                        FriendlyMessage = "SMS: MO Acknowledgment";
                    }
                    else if (op == "53")
                    {
                        FriendlyMessage = "SMS: SR Acknowledgment";
                    }

                }
            }
        }


        /// <summary>
        /// Gets or sets the parsed UCP / EMI message represented by a <see cref="EmiProtocol"/> object
        /// </summary>
        /// <value>
        /// The EMI protocol object.
        /// </value>
        public EmiProtocol EMIProtocolObject
        {
            get
            {
                if (_emiObject == null)
                {
                    return new EmiProtocol(rawmessage);
                }
                else
                {
                    return _emiObject;
                }
            }
            set
            {
                _emiObject = value;
            }
        }

        private EmiProtocol _emiObject;

        /// <summary>
        /// Gets or sets the translated EMI protocol to a human readable message.
        /// </summary>
        /// <value>
        /// The friendly message.
        /// </value>
        public string FriendlyMessage { get; set; }
        /// <summary>
        /// Gets or sets the create date.
        /// </summary>
        /// <value>
        /// The create date.
        /// </value>
        public DateTimeOffset CreateDate { get; set; }
        /// <summary>
        /// Gets or sets the expected send date.
        /// </summary>
        /// <value>
        /// The expected send date.
        /// </value>
        public DateTimeOffset ExpectedSendDate { get; set; }

        /// <summary>
        /// The direction of the message.
        /// </summary>
        /// <remarks>
        /// Following messages are considering Incoming:
        /// <list type="bullet">
        /// <item><see cref="EmiMessageType.SESSION"/></item>
        /// <item><see cref="EmiMessageType.PING"/></item>
        /// <item><see cref="EmiMessageType.MT"/></item>
        /// <item><see cref="EmiMessageType.MO_ACK"/></item>
        /// <item><see cref="EmiMessageType.SR_ACK"/></item>
        /// <item><see cref="EmiMessageType.MO_NACK"/></item>
        /// <item><see cref="EmiMessageType.SR_NACK"/></item>
        /// </list>
        /// </remarks>
        public MessageDirection Direction { get; set; }


        /// <summary>
        /// Gets or sets the <see cref="Message"/> attached to this <see cref="EmiMessage"/>.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public Message Message { get; set; }
    }



}

