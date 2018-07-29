using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Hummingbird.Extension.SMSC
{
    /// <summary>
    /// This Class implements fields and operations used by the EMI Prototol.
    /// This example does not implement all the services decribed by the EMI-UCP interface. Only the following services are implemented.
    /// UCP 60 - Session
    /// UCP 51 - Submit Short Message Operation
    /// UCP 52 - Delivery Short Message Operation
    /// UCP 53 - Delivery Notification Operation
    /// </summary>
    public class EMIProtocol
    {
        private static char STX = '\x02';
        private static char ETX = '\x03';
        private static char SEP = '/';
        private static int TRN_MO = 00;

        [NonSerialized]
        private static SmsServer referedServer;
        private static string SRText = "this is a SR from {0} to {1}";
        private static string SCTSFormat = "ddMMyyhhmmss";

        private static Dictionary<string, string> SCTSCache = new Dictionary<string, string>();

        //EMI TRAME FORMAT:  STX <HEADER> / <DATA> / <CHECKSUM> / ETX

        internal string OriginalTrame { get; private set; }

        //EMI Header members        
        /// <summary>
        /// // 2 num char 00-99
        /// </summary>
        string TRN;

        /// <summary>
        ///  5 num char, Total number of IRA characters contained between stx and etx, right justified with leading zeros.
        /// </summary>
        string LRN;

        /// <summary>
        /// Char O or R, “O” indicates operation, “R” indicates result
        /// </summary>
        public string OR { get; private set; }

        /// <summary>
        /// 2 num char; Operation Type
        /// </summary>
        /// <value>
        /// The operation type: 60, 51, 52, ....
        /// </value>
        public string OT { get; private set; }

        //EMI MT DATA members
        /// <summary>
        /// Gets the AdC field.
        /// </summary>
        /// <value>
        /// The ad c.
        /// </value>
        public string AdC { get; private set; }
        /// <summary>
        /// Gets the o ad c.
        /// </summary>
        /// <value>
        /// The o ad c.
        /// </value>
        public string OAdC { get; private set; }
        string AC { get; set; }
        string NRq { get; set; }
        string NAdC { get; set; }
        string NT { get; set; }
        string NPID { get; set; }
        string LRq { get; set; }
        string LRAd { get; set; }
        string LPID { get; set; }
        string DD { get; set; }
        string DDT { get; set; }
        string VP { get; set; }
        string RPID { get; set; }
        string MT { get; set; }
        string NB { get; set; }
        string AMsg { get; set; }
        string NMsg { get; set; }
        string TMsg { get; set; }
        string MMS { get; set; }
        string PR { get; set; }
        string MCLs { get; set; }
        string RPI { get; set; }
        string OTOA { get; set; }

        /// <summary>
        /// Gets or sets the XSER field.
        /// </summary>
        /// <value>
        /// The x ser.
        /// </value>
        public string XSer { get; private set; }
        string SCTS { get; set; }

        //EMI SESSION DATA members
        string OTON { get; set; }
        string ONPI { get; set; }
        string STYP { get; set; }
        string PWD { get; set; }
        string NPWD { get; set; }
        string VERS { get; set; }
        string OPID { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EMIProtocol"/> class.
        /// </summary>
        /// <param name="trame">The original RAW trame of the current message, starts with STX charactor and ends with ETX charactor</param>
        /// <param name="server">The server.</param>
        public EMIProtocol(string trame, SmsServer server)
        {
            if (referedServer == null)
            {
                referedServer = server;
            }
            OriginalTrame = trame;
            string[] pairs = trame.Split(SEP);
            TRN = pairs[0].Substring(1);
            LRN = pairs[1];
            OR = pairs[2].ToUpper();
            OT = pairs[3];

            if (OR == "O")
            {
                // OPERATION
                switch (OT)
                {
                    case "51":
                        AdC = pairs[4];
                        OAdC = pairs[5];
                        AC = pairs[6];
                        NRq = pairs[7];
                        NAdC = pairs[8];
                        NT = pairs[9];
                        NPID = pairs[10];
                        LRq = pairs[11];
                        LRAd = pairs[12];
                        LPID = pairs[13];
                        DD = pairs[14];
                        DDT = pairs[15];
                        VP = pairs[16];
                        RPID = pairs[17];
                        MT = pairs[22];
                        NB = pairs[23];
                        AMsg = pairs[24];
                        NMsg = pairs[24];
                        TMsg = pairs[24];
                        MMS = pairs[25];
                        PR = pairs[26];
                        MCLs = pairs[28];
                        RPI = pairs[29];
                        OTOA = pairs[32];
                        XSer = pairs[34];
                        break;
                    case "60":
                        OAdC = pairs[4];
                        OTON = pairs[5];
                        ONPI = pairs[6];
                        STYP = pairs[7];
                        PWD = System.Text.ASCIIEncoding.ASCII.GetString(HexStringToByteArray(pairs[8]));
                        NPWD = System.Text.ASCIIEncoding.ASCII.GetString(HexStringToByteArray(pairs[9]));
                        VERS = pairs[10];
                        OPID = pairs[14];
                        break;
                }




            }
            else
            {
                // RESPONSE Nothing to do.
            }
        }
        /// <summary>
        /// used for trace trame only
        /// </summary>
        /// <param name="trame">Original raw trame of EMI-UCP representing a SMS</param>
        public EMIProtocol(string trame)
        {
            OriginalTrame = trame;
            try
            {
                string[] pairs = trame.Split(SEP);
                TRN = pairs[0].Substring(1);
                LRN = pairs[1];
                OR = pairs[2].ToUpper();
                OT = pairs[3];
            }
            catch { }
        }
        /// <summary>
        /// Gets the real SCTS.
        /// </summary>
        /// <param name="OAdc">The oadc.</param>
        /// <param name="Adc">The adc.</param>
        /// <param name="Date">The date.</param>
        /// <returns></returns>
        internal static string GetRealSCTS(string OAdc, string Adc, DateTimeOffset Date)
        {
            //Current Date
            string local_scts = Date.ToString(SCTSFormat);
            string new_scts = string.Empty;

            //If the STCSCache passed 1000 pairs, we clean all item earlier than the local_scts;
            if (SCTSCache.Count > 1000)
            {
                lock (SCTSCache)
                {
                    Dictionary<string, string> TempSCTSCache = new Dictionary<string, string>();
                    foreach (var v in SCTSCache)
                    {
                        if (v.Value.CompareTo(local_scts) > 0) TempSCTSCache.Add(v.Key, v.Value);
                    }
                    SCTSCache = TempSCTSCache;
                }
            }

            if (SCTSCache.TryGetValue(OAdc + Adc, out new_scts))
            {

                //Last SMS sent Date + 1 second.
                DateTimeOffset newdate = DateTimeOffset.ParseExact(new_scts, SCTSFormat, CultureInfo.InvariantCulture).AddSeconds(1);

                //compare current date and sms sent date.
                if (newdate > Date)
                {
                    //in this case, there are 2 or more sms in one second, we should use the last sms sent date + 1 second as SCTS
                    new_scts = newdate.ToString(SCTSFormat);
                }
                else
                {
                    //in this case, there is no 2 sms in one second, use current date as SCTS
                    new_scts = local_scts;
                }
                SCTSCache[OAdc + Adc] = new_scts; //update SCTS in the dictionary.
                return new_scts;
            }
            else
            {
                //in this case, there are no sms has been sent to this oAdC and AdC pair, use Current Time as SCTS and add it to the dinctionary.
                SCTSCache.Add(OAdc + Adc, local_scts);
                return local_scts;
            }
        }

        internal string CreateSRForMT(int DST, string RSN)
        {
            int localTRN = TRN_MO++;
            TRN_MO = TRN_MO % 100;
            if (AdC == string.Empty || (NT == "0" || NT == string.Empty)) return string.Empty; // IS PING OR SR Is not requried.

            if (NT == "2" && referedServer.srDst == 0) return string.Empty; // IF Nt = Non-delivery notification and SRDst = "0 - delivered" return an empty string
            string textSR = string.Format(SRText, AdC, OAdC);
            string sr = STX + localTRN.ToString("00") + "/LLLLL/O/53/" + OAdC + "/" + AdC + "/////////////" + SCTS + "/" + DST + "/" + RSN + "/" + SCTS + "/3//" + Encode(textSR, MessageFormat.GSM8) + "/////////////////SS" + ETX;
            sr = CheckSum(sr.Replace("LLLLL", (sr.Length - 2).ToString("00000")));
            return sr;
        }

        internal static string CreateSR(string OAdC, string AdC, string SCTS, string DST, string RSN, string text)
        {
            int localTRN = TRN_MO++;
            TRN_MO = TRN_MO % 100;
            string textSR = string.Format(SRText, AdC, OAdC);
            string sr = STX + localTRN.ToString("00") + "/LLLLL/O/53/" + AdC + "/" + OAdC + "/////////////" + SCTS + "/" + DST + "/" + RSN + "/" + SCTS + "/3//" + Encode(textSR, MessageFormat.GSM8) + "/////////////////SS" + ETX;
            sr = CheckSum(sr.Replace("LLLLL", (sr.Length - 2).ToString("00000")));
            return sr;
        }

        internal string CreateACK51(bool isACK, string NACK_CODE, string descriptionError)
        {

            //SCTS is not datetime.now, but we have a little trick.
            //from one oAdc to one Adc in the same second, we cannot return the same SCTS because that will lead 2 SMS with the same 
            this.SCTS = GetRealSCTS(this.OAdC, this.AdC, new DateTimeOffset(DateTime.Now));

            string mt = AdC + ":" + SCTS;
            if (AdC == string.Empty) mt = string.Empty;
            if (isACK)
            {
                //exemple of ACK: STX01/00043/R/51/A//0663301342:ddMMyyHHmmss/CheckSumETX
                string ack = STX + TRN + "/LLLLL/R/" + OT + "/A//" + mt + "/SS" + ETX;
                ack = ack.Replace("LLLLL", (ack.Length - 2).ToString("00000"));
                ack = CheckSum(ack);
                return ack;
            }
            else
            {
                //exemple of NACK:   STX01/00022/R/51/N/31//0AETX
                string nack = STX + TRN + "/LLLLL/R/" + OT + "/N/" + NACK_CODE + "/" + descriptionError + "/SS" + ETX;
                nack = nack.Replace("LLLLL", (nack.Length - 2).ToString("00000"));
                nack = CheckSum(nack);
                return nack;
            }
        }

        internal string CreateACK60(bool isACK, string NACK_CODE)
        {
            if (isACK)
            {
                //exemple of ACK: STX01/00043/R/51/A//0663301342:ddMMyyHHmmss/CheckSumETX
                string ack = STX + TRN + "/LLLLL/R/60/A//SS" + ETX;
                ack = ack.Replace("LLLLL", (ack.Length - 2).ToString("00000"));
                ack = CheckSum(ack);
                return ack;
            }
            else
            {
                //exemple of NACK:   STX01/00022/R/51/N/31//0AETX
                string nack = STX + TRN + "/LLLLL/R/60/N/" + NACK_CODE + "//SS" + ETX;
                nack = nack.Replace("LLLLL", (nack.Length - 2).ToString("00000"));
                nack = CheckSum(nack);
                return nack;
            }
        }

        internal static string CheckSum(string originalTrame)
        {
            int cksum = 0;
            char c;
            byte b;
            for (int i = 1; i < originalTrame.Length - 3; i++)
            {
                c = originalTrame[i];
                b = (byte)(c & 0xff);
                cksum += b;
            }
            cksum = cksum % 256;
            return originalTrame.Replace("SS", cksum.ToString("X2"));
        }

        internal static string CreateMO(string OAdC, string AdC, string text, DateTimeOffset SCTS, MessageFormat format, MT MT)
        {
            int localTRN = TRN_MO++;
            TRN_MO = TRN_MO % 100;
            string Msg = string.Empty;
            string Xser = string.Empty;
            string Mt = string.Empty;
            if ((int)MT != 0) Mt = ((int)MT).ToString();
            string NB = string.Empty;
            int iNB = 0;
            if (format == MessageFormat.Unicode)
            {
                //According to Spec EMI. if we want to send SMS coded to unicode, the MT must set to 4.
                Msg = TextToUnicodeHexString(text);
                iNB = Msg.Length;
                NB = iNB.ToString();
                Mt = "4";
                Xser = "020108"; // En unicode non compressé
            }
            else if (format == MessageFormat.GSM7)
            {
                Msg = TextToGSM8HexString(text);// septetToOctet(stringToSeptet(text));
                iNB = Msg.Length;
                NB = iNB.ToString();
                Xser = "020100"; // En GSM 7bit noncompressé
            }
            else
            {
                // Par defaut on encode en GSM
                //According to Spec EMI. if we want to send SMS coded to 8-bit, the MT must set to 4.
                Msg = TextToGSM8HexString(text);
                Xser = ""; // En GSM non compresse
                Mt = "4";
                Xser = "0201F4";
                NB = string.Empty;
            }
            string mo = STX + localTRN.ToString("00") + "/LLLLL/O/52/" + AdC + "/" + OAdC + "/////////////" + SCTS.ToString(SCTSFormat) + "////" + Mt + "/" + NB + "/" + Msg + "//////////" + Xser + "///SS" + ETX;
            string length = (mo.Length - 2).ToString("00000");
            mo = mo.Replace("LLLLL", length);
            mo = CheckSum(mo);
            return mo;
        }

        internal static byte[] HexStringToByteArray(string hexValue)
        {
            return decode(hexValue.ToCharArray());
        }

        internal static string Encode(string text, MessageFormat format)
        {
            switch (format)
            {
                case MessageFormat.GSM7:
                    return SeptetToOctet(StringToSeptet(text));
                case MessageFormat.GSM8:
                    return TextToGSM8HexString(text);
                case MessageFormat.Unicode:
                    return TextToUnicodeHexString(text);
            }
            return string.Empty;
        }

        internal static string TextToGSM8HexString(string text)
        {
            StringBuilder result = new StringBuilder();
            foreach (char c in text)
            {
                bool foundInTable = false;

                if (c == '.')
                {
                    result.Append("2E");
                    continue;
                }

                //find char in standard table;
                for (int i = 0; i < MMMtcBufGSMToLatin.Length; i++)
                {
                    if (c == MMMtcBufGSMToLatin[i])
                    {
                        result.Append(i.ToString("X2"));
                        foundInTable = true;
                        break;
                    }
                }


                //find char in extended table;
                for (int i = 0; i < MMMtcBufExtGSMToLatin.Length; i++)
                {
                    if (c == MMMtcBufExtGSMToLatin[i])
                    {
                        result.Append("1B" + i.ToString("X2"));
                        foundInTable = true;
                        break;
                    }
                }

                if (!foundInTable) result.Append("2E");
            }

            return result.ToString();
        }

        internal static string TextToUnicodeHexString(string text)
        {
            string retour = string.Empty;
            byte[] titi = null;

            for (int i = 0; i < text.Length; i++)
            {
                byte tmpByte;
                char enUniCodeChar = isoToUnicode[text[i]];
                titi = CharToByteArray(enUniCodeChar);
                tmpByte = titi[0];
                titi[0] = titi[1];
                titi[1] = tmpByte;
                retour = retour + ByteToHexString(titi);
            }

            retour = retour.ToUpper();
            return (retour);
        }

        internal static string TextToGsm7HexString(string text)
        {
            return SeptetToOctet(StringToSeptet(text));
        }

        internal static string GSM8HexToString(string gsmStr)
        {
            string retour = string.Empty;

            byte[] tableauDeBytes = decode(gsmStr.ToCharArray());
            int iMax = tableauDeBytes.Length;
            char caractere;

            for (int i = 0; i < iMax; i++)
            {
                try
                {
                    if (tableauDeBytes[i] == (byte)0x1B)
                    {
                        i++;
                        caractere = MMMtcBufExtGSMToLatin[tableauDeBytes[i]];
                        if (caractere == '.')
                        {
                            caractere = MMMtcBufGSMToLatin[tableauDeBytes[i]];
                        }
                    }
                    else
                        caractere = MMMtcBufGSMToLatin[tableauDeBytes[i]];

                    retour = retour + caractere;
                }
                catch
                {
                    retour = retour + "?";
                }
            }

            return retour;
        }

        internal static string GSM7HexToString(string s)
        {
            //first restore GSM7bit to GSM8bit
            byte[] gsm7 = HexStringToByteArray(s);
            byte[] gsm8 = new byte[gsm7.Length * 8 / 7];
            int current = 0;
            byte stock = (byte)0;
            for (int i = 0; i < gsm7.Length; i++)
            {
                int shift = (i + 1) % 7;
                int low = (gsm7[i] << shift >> shift) & 0x7F;
                int high = gsm7[i] >> (8 - shift);
                if (shift == 0)
                {
                    gsm8[current++] = (byte)(((gsm7[i] << 6) & 0x7F) + stock);
                    gsm8[current++] = (byte)(gsm7[i] >> 1);
                    stock = 0;
                }
                else
                {
                    gsm8[current++] = (byte)(((low << (shift - 1)) & 0x7F) + stock);
                    stock = (byte)high;
                }
            }
            return GSM8HexToString(ByteToHexString(gsm8));
        }

        static private string ByteToHexString(byte[] b)
        {
            char[] sb = new char[b.Length * 2];
            for (int i = 0; i < b.Length; i++)
            {
                // look up high nibble char
                sb[i * 2] = hexChar[(b[i] & '\xf0') >> 4];
                sb[i * 2 + 1] = hexChar[b[i] & '\x0f'];
            }
            return new string(sb);
        }

        static private byte[] CharToByteArray(char c)
        {
            byte[] twoBytes = { (byte)(c & 0xff), (byte)(c >> 8 & 0xff) };
            return twoBytes;
        }

        /**
         * Decode an array of hex chars
         *
         * @param hexChars an array of hex characters.
         * @return the decode hex chars as bytes.
         */
        internal static byte[] decode(char[] hexChars)
        {
            return decode(hexChars, 0, hexChars.Length);
        }

        /**
         * Decode an array of hex chars.
         *
         * @param hexChars an array of hex characters.
         * @param starIndex the index of the first character to decode
         * @param length the number of characters to decode.
         * @return the decode hex chars as bytes.
         */
        internal static byte[] decode(char[] hexChars, int startIndex, int length)
        {
            if ((length & 1) != 0)
                throw new ArgumentException("Length must be even");

            byte[] result = new byte[length / 2];
            for (int j = 0; j < result.Length; j++)
            {
                result[j] = (byte)(hexCharToNibble(hexChars[startIndex++]) * 16 + hexCharToNibble(hexChars[startIndex++]));
            }
            return result;
        }

        /**
         * Internal method to turn a hex char into a nibble.
         */
        private static int hexCharToNibble(char ch)
        {
            if ((ch >= '0') && (ch <= '9'))
                return ch - '0';
            else if ((ch >= 'a') && (ch <= 'f'))
                return ch - 'a' + 10;
            else if ((ch >= 'A') && (ch <= 'F'))
                return ch - 'A' + 10;
            else
                throw new ArgumentException("Not a hex char - '" + ch + "'");
        }


        internal static List<string> StringToSeptet(string text)
        {
            List<string> septet = new List<string>();
            int msglen = text.Length;
            for (int i = 0; i < msglen; i++)
            {
                byte b = Convert.ToByte(text[i]);
                string space = Convert.ToString(b, 2);
                if (space.Length == 6)
                {
                    space = "0" + space;
                }
                //txtresult.Text += space + "-"; //Converting to binary
                septet.Add(space); //storing in a list
            }
            septet.Add("0000000");
            return septet;
        }

        private static string SWAP(string str)
        {
            char[] org = str.ToCharArray();
            char[] swap = new char[org.Length];
            string uy = string.Empty;
            int pppp = 0;
            for (int sw = (org.Length - 1); sw >= 0; sw--)
            {
                swap[pppp] = org[sw];
                uy += swap[pppp].ToString();
                pppp++;

            }
            return uy;
        }

        internal static string SeptetToOctet(List<string> septet)
        {
            List<string> octet = new List<string>();
            string hexa;
            #region Converting from septet to octet
            //-------------
            int len = 1;
            int j = 0;
            //-----------
            int septetcount = septet.Count;
            //MessageBox.Show(septetcount.ToString());
            //---------------
            while (j < septet.Count - 1)
            {

                string tmp = septet[j]; // storing jth value
                string tmp1 = septet[j + 1]; //storing j+1 th value
                //-------------- Swapping----------
                string mid = SWAP(tmp1);

                //---------------------
                tmp1 = mid;
                tmp1 = tmp1.Substring(0, len);
                //-----------reverse swapping
                string add = SWAP(tmp1);
                //-------------------
                tmp = add + tmp;// +"-";
                tmp = tmp.Substring(0, 8);
                //txtoctet.Text += tmp + " || ";
                octet.Add(tmp);
                len++;
                if (len == 8)
                {
                    len = 1;
                    j = j + 1;
                }
                j = j + 1;

                //}


            }
            #endregion

            hexa = string.Empty;

            #region Converting from octet to hex
            for (int x = 0; x < octet.Count; x++)
            {
                string oct = octet[x];
                //MessageBox.Show(oct.Length.ToString());
                string Fhalf = oct.Substring(0, 4);
                string Shalf = oct.Substring(4, 4).ToString();
                string hex1 = string.Empty;
                string hex2 = string.Empty;

                switch (Fhalf)
                {
                    case "0000": hex1 = "0"; break;
                    case "0001": hex1 = "1"; break;
                    case "0010": hex1 = "2"; break;
                    case "0011": hex1 = "3"; break;
                    case "0100": hex1 = "4"; break;
                    case "0101": hex1 = "5"; break;
                    case "0110": hex1 = "6"; break;
                    case "0111": hex1 = "7"; break;
                    case "1000": hex1 = "8"; break;
                    case "1001": hex1 = "9"; break;
                    case "1010": hex1 = "A"; break;
                    case "1011": hex1 = "B"; break;
                    case "1100": hex1 = "C"; break;
                    case "1101": hex1 = "D"; break;
                    case "1110": hex1 = "E"; break;
                    case "1111": hex1 = "F"; break;
                    default: break;
                }

                switch (Shalf)
                {
                    case "0000": hex2 = "0"; break;
                    case "0001": hex2 = "1"; break;
                    case "0010": hex2 = "2"; break;
                    case "0011": hex2 = "3"; break;
                    case "0100": hex2 = "4"; break;
                    case "0101": hex2 = "5"; break;
                    case "0110": hex2 = "6"; break;
                    case "0111": hex2 = "7"; break;
                    case "1000": hex2 = "8"; break;
                    case "1001": hex2 = "9"; break;
                    case "1010": hex2 = "A"; break;
                    case "1011": hex2 = "B"; break;
                    case "1100": hex2 = "C"; break;
                    case "1101": hex2 = "D"; break;
                    case "1110": hex2 = "E"; break;
                    case "1111": hex2 = "F"; break;
                    default: break;
                }


                hexa += hex1 + hex2;

            }
            #endregion


            //------------------

            return hexa;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return OriginalTrame;
        }

        internal string GetFriendlyMessage()
        {
            string friendlyMessage = string.Empty;
            string rawmessage;
            string op = "0";
            string to = "0";
            string from = "0";
            string direction = "O";
            rawmessage = this.OriginalTrame;
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
                    message = System.Text.UnicodeEncoding.BigEndianUnicode.GetString(EMIProtocol.decode(hexmessage.ToCharArray()));
                }
                else
                {
                    //GSM8BIT / GSM7BIT
                    message = EMIProtocol.GSM8HexToString(hexmessage);
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
                        from = EMIProtocol.GSM7HexToString(from.Substring(2)).Substring(0, int.Parse(from.Substring(0, 2), NumberStyles.HexNumber) * 8 / 14);
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
                        to = EMIProtocol.GSM7HexToString(to.Substring(2)).Substring(0, int.Parse(to.Substring(0, 2), NumberStyles.HexNumber) * 8 / 14);
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
                    friendlyMessage = "SMS: Open Session Acknolegement";
                }
                else if (op == "51" && direction == "R")
                {
                    friendlyMessage = "SMS: MT Acknowlegement";
                }
                else if (op == "51" && direction == "O")
                {
                    friendlyMessage = "SMS: Empty MT (Ping)";
                }
                else if (op == "52")
                {
                    friendlyMessage = "SMS: MO Acknowlegement";
                }
                else if (op == "53")
                {
                    friendlyMessage = "SMS: SR Acknowlegement";
                }


            }
            return friendlyMessage;
        }

        #region Charactor Tables
        //static members
        static char[] isoToUnicode =
        {
            '\u0000','\u0001','\u0002','\u0003','\u0004','\u0005','\u0006','\u0007',
            '\u0008','\u0009','\u000A','\u000B','\u000C','\u000D','\u000E','\u000F',
            '\u0010','\u0011','\u0012','\u0013','\u0014','\u0015','\u0016','\u0017',
            '\u0018','\u0019','\u001A','\u001B','\u001C','\u001D','\u001E','\u001F',
            '\u0020','\u0021','\u0022','\u0023','\u0024','\u0025','\u0026','\u0027',
            '\u0028','\u0029','\u002A','\u002B','\u002C','\u002D','\u002E','\u002F',
            '\u0030','\u0031','\u0032','\u0033','\u0034','\u0035','\u0036','\u0037',
            '\u0038','\u0039','\u003A','\u003B','\u003C','\u003D','\u003E','\u003F',
            '\u0040','\u0041','\u0042','\u0043','\u0044','\u0045','\u0046','\u0047',
            '\u0048','\u0049','\u004A','\u004B','\u004C','\u004D','\u004E','\u004F',
            '\u0050','\u0051','\u0052','\u0053','\u0054','\u0055','\u0056','\u0057',
            '\u0058','\u0059','\u005A','\u005B','\u005C','\u005D','\u005E','\u005F',
            '\u0060','\u0061','\u0062','\u0063','\u0064','\u0065','\u0066','\u0067',
            '\u0068','\u0069','\u006A','\u006B','\u006C','\u006D','\u006E','\u006F',
            '\u0070','\u0071','\u0072','\u0073','\u0074','\u0075','\u0076','\u0077',
            '\u0078','\u0079','\u007A','\u007B','\u007C','\u007D','\u007E','\u007F',
            '\u0080','\u0081','\u0082','\u0083','\u0084','\u0085','\u0086','\u0087',
            '\u0088','\u0089','\u008A','\u008B','\u008C','\u008D','\u008E','\u008F',
            '\u0090','\u0091','\u0092','\u0093','\u0094','\u0095','\u0096','\u0097',
            '\u0098','\u0099','\u009A','\u009B','\u009C','\u009D','\u009E','\u009F',
            '\u00A0','\u0104','\u0105','\u0141','\u20AC','\u201E','\u0160','\u00A7',
            '\u0161','\u00A9','\u0218','\u00AB','\u0179','\u00AD','\u017A','\u017B',
            '\u00B0','\u00B1','\u010C','\u0142','\u017D','\u201D','\u00B6','\u00B7',
            '\u017E','\u010D','\u0219','\u00BB','\u0152','\u0153','\u0178','\u017C',
            '\u00C0','\u00C1','\u00C2','\u0102','\u00C4','\u0106','\u00C6','\u00C7',
            '\u00C8','\u00C9','\u00CA','\u00CB','\u00CC','\u00CD','\u00CE','\u00CF',
            '\u0110','\u0143','\u00D2','\u00D3','\u00D4','\u0150','\u00D6','\u015A',
            '\u0170','\u00D9','\u00DA','\u00DB','\u00DC','\u0118','\u021A','\u00DF',
            '\u00E0','\u00E1','\u00E2','\u0103','\u00E4','\u0107','\u00E6','\u00E7',
            '\u00E8','\u00E9','\u00EA','\u00EB','\u00EC','\u00ED','\u00EE','\u00EF',
            '\u0111','\u0144','\u00F2','\u00F3','\u00F4','\u0151','\u00F6','\u015B',
            '\u0171','\u00F9','\u00FA','\u00FB','\u00FC','\u0119','\u021B','\u00FF'
        };

        static char[] GSM7BitToUnicode =
        {
            '\u0040','\u00A3','\u0024','\u00A5','\u00E8','\u00E9','\u00F9','\u00Ec',  //00
			'\u00F2','\u00E7','\u000A','\u00D8','\u00F8','\u000D','\u00C5','\u00E5',  //08
			'\u0394','\u005F','\u03A6','\u0393','\u039B','\u03A9','\u03A0','\u03A8',  //10
			'\u0018','\u0019','\u001A','\u001B','\u001C','\u001D','\u001E','\u001F',  //18
			'\u0020','\u0021','\u0022','\u0023','\u0024','\u0025','\u0026','\u0027',  //20
			'\u0028','\u0029','\u002A','\u002B','\u002C','\u002D','\u002E','\u002F',  //28
			'\u0030','\u0031','\u0032','\u0033','\u0034','\u0035','\u0036','\u0037',  //30
			'\u0038','\u0039','\u003A','\u003B','\u003C','\u003D','\u003E','\u003F',  //38
			'\u0040','\u0041','\u0042','\u0043','\u0044','\u0045','\u0046','\u0047',  //40
			'\u0048','\u0049','\u004A','\u004B','\u004C','\u004D','\u004E','\u004F',  //48
			'\u0050','\u0051','\u0052','\u0053','\u0054','\u0055','\u0056','\u0057',  //50
			'\u0058','\u0059','\u005A','\u005B','\u005C','\u005D','\u005E','\u005F',  //58
			'\u0060','\u0061','\u0062','\u0063','\u0064','\u0065','\u0066','\u0067',  //60
			'\u0068','\u0069','\u006A','\u006B','\u006C','\u006D','\u006E','\u006F',  //68
			'\u0070','\u0071','\u0072','\u0073','\u0074','\u0075','\u0076','\u0077',  //70
			'\u0078','\u0079','\u007A','\u007B','\u007C','\u007D','\u007E','\u007F',  //78
			'\u0080','\u0081','\u0082','\u0083','\u0084','\u0085','\u0086','\u0087',  //80
			'\u0088','\u0089','\u008A','\u008B','\u008C','\u008D','\u008E','\u008F',  //88
			'\u0090','\u0091','\u0092','\u0093','\u0094','\u0095','\u0096','\u0097',  //90
			'\u0098','\u0099','\u009A','\u009B','\u009C','\u009D','\u009E','\u009F',  //98
			'\u00A0','\u0104','\u0105','\u0141','\u20AC','\u201E','\u0160','\u00A7',  //A0
			'\u0161','\u00A9','\u0218','\u00AB','\u0179','\u00AD','\u017A','\u017B',  //A8
			'\u00B0','\u00B1','\u010C','\u0142','\u017D','\u201D','\u00B6','\u00B7',  //B0
			'\u017E','\u010D','\u0219','\u00BB','\u0152','\u0153','\u0178','\u017C',  //B8
			'\u00C0','\u00C1','\u00C2','\u0102','\u00C4','\u0106','\u00C6','\u00C7',  //C0
			'\u00C8','\u00C9','\u00CA','\u00CB','\u00CC','\u00CD','\u00CE','\u00CF',  //C8
			'\u0110','\u0143','\u00D2','\u00D3','\u00D4','\u0150','\u00D6','\u015A',  //D0
			'\u0170','\u00D9','\u00DA','\u00DB','\u00DC','\u0118','\u021A','\u00DF',  //D8
			'\u00E0','\u00E1','\u00E2','\u0103','\u00E4','\u0107','\u00E6','\u00E7',  //E0
			'\u00E8','\u00E9','\u00EA','\u00EB','\u00EC','\u00ED','\u00EE','\u00EF',  //E8
			'\u0111','\u0144','\u00F2','\u00F3','\u00F4','\u0151','\u00F6','\u015B',  //F0
			'\u0171','\u00F9','\u00FA','\u00FB','\u00FC','\u0119','\u021B','\u00FF'   //F8
		};

        static char[] hexChar = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        static char[] MMMtcBufLatinToGSM =
        {
            '.','.','.','.','.','.','.','.','.','.','\n','.','.','\r','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            ' ','!','"','#','\x02','%','&','\x27','(',')','*','+',',','-','.','/',
            '0','1','2','3','4','5','6','7','8','9',':',';','<','=','>','?',
            '\x00','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',
            'P','Q','R','S','T','U','V','W','X','Y','Z','\xFF','\xFF','\xFF','\xFF','\x11',
            '.','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o',
            'p','q','r','s','t','u','v','w','x','y','z','\xFF','\xFF','\xFF','\xFF','.',
            '\xFF','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','\x40','.','\x01','\x24','\x03','.','\x5F','.','.','.','.','\xFF','.','.','.',
            '.','.','.','.','.','\x16','.','.','.','.','.','.','.','.','.','\x60',
            '.','.','.','.','\x5b','\x0e','\x1c','\x09','\x05','\x1f','E','\x07','i','I','I','.',
            '.','\x5D','\x08','o','o','o','\x5C','x','\x0B','\x06','u','u','\x5E','y','.','\x1E',
            '\x7F','a','a','a','\x7B','\x0F','\x1D','c','\x04','\x05','e','e','\x07','i','i','i',
            'o','\x7D','\x08','o','o','o','\x7C','.','\x0C','\x06','u','u','\x7E','y','.','y'
        };

        static char[] MMMtcBufLatinToExtGSM =
        {
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','\x3c','\x2f','\x3e','\x14','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','\x28','\x40','\x29','\x3d','.',
            '\x65','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'
        };

        static char[] MMMtcBufGSMToLatin =
        {
            '@','£','$','¥','è','é','ù','ì','ò','Ç','\n','Ø','ø','\r','Å','å',
            '.','_','.','.','.','.','.','.','Θ','.','.','.','Æ','æ','ß','É',
            ' ','!','"','#','¤','%','&','\'','(',')','*','+',',','-','.','/',
            '0','1','2','3','4','5','6','7','8','9',':',';','<','=','>','?',
            '¡','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',
            'P','Q','R','S','T','U','V','W','X','Y','Z','Ä','Ö','Ñ','Ü','§',
            '¿','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o',
            'p','q','r','s','t','u','v','w','x','y','z','ä','ö','ñ','ü','à'
        };

        static char[] MMMtcBufExtGSMToLatin =
        {
            '.','.','.','.','.','.','.','.','.','.','\x0c','.','.','.','.','.',
            '.','.','.','.','^','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','{','}','.','.','.','.','.','\\',
            '.','.','.','.','.','.','.','.','.','.','.','.','[','~',']','.',
            '|','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','€','.','.','.','.','.','.','.','.','.','.',
            '.','.','.','.','.','.','.','.','.','.','.','.','.','.','.','.'
        };
        #endregion
    }
    // CLASS

    /// <summary>
    /// Text Message format used.
    /// </summary>
    [Serializable]

    public enum MessageFormat
    {
        /// <summary>
        /// The unicode
        /// </summary>
        Unicode,
        /// <summary>
        /// The gs m7
        /// </summary>
        GSM7,
        /// <summary>
        /// The gs m8
        /// </summary>
        GSM8
    };

    /// <summary>
    /// Possible value used in MT field.
    /// </summary>
    [Serializable]
    internal enum MT
    {
        /// <summary>
        /// The numeric
        /// </summary>
        Numeric = 2,
        /// <summary>
        /// The alpha numeric
        /// </summary>
        AlphaNumeric = 3,
        /// <summary>
        /// The transparent
        /// </summary>
        Transparent = 4
    }
}
