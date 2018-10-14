using ALAZ.SystemEx.NetEx.SocketsEx;

using Hummingbird.TestFramework.Messaging;
using Hummingbird.TestFramework.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Hummingbird.Extension.SMSC
{
    /// <summary>
    /// The buffer to receive or send messages through Socket.
    /// </summary>
    internal class MessageBuffers
    {
        public ISocketConnection Connection;
        public int BUFFERSIZE;
        public byte[] buffer_in; // 1MB of buffer for MT
        public byte[] buffer_out; // 1MB of buffer for MO and SR
        public int in_startindex = 0;
        public int in_endindex = 0;
        public int out_startindex = 0;
        public int out_endindex = 0;
        public Queue MessageToBeSent = new Queue(); 
        public Queue SRToBeSent = Queue.Synchronized(new Queue());
        public DateTimeOffset NextCheckingDate = new DateTimeOffset(DateTime.Now);
        public bool Running;
        public bool warned = false;
        public bool AcceptsMT = false;

        public string ReadNextMessageIN()
        {
            lock (buffer_in)
            {
                bool ReadOK = false;
                byte[] data = new byte[1024];
                int i = 0;
                int data_length = 0;
                //Read the message in buffer
                if (in_endindex >= in_startindex)
                {
                    int k = 0;
                    for (i = in_startindex; i < in_endindex; i++)
                    {
                        data[k] = buffer_in[i];
                        if (data[k] == '\x03')
                        {
                            ReadOK = true;
                            data_length = k + 1;
                            break;
                        }
                        k++;
                    }
                }
                else
                {
                    int k = 0;
                    for (i = in_startindex; i < BUFFERSIZE; i++)
                    {
                        data[k] = buffer_in[i];
                        if (data[k] == '\x03')
                        {
                            ReadOK = true;
                            data_length = k + 1;
                            break;
                        }
                        k++;

                    }
                    if (!ReadOK)
                    {
                        for (i = 0; i < in_endindex; i++)
                        {
                            data[k] = buffer_in[i];
                            if (data[k] == '\x03')
                            {
                                ReadOK = true;
                                data_length = k + 1;
                                break;
                            }
                            k++;
                        }
                    }
                }

                //update the startindex

                if (ReadOK)
                {
                    in_startindex = in_startindex + data_length;
                    if (in_startindex >= BUFFERSIZE) in_startindex = in_startindex - BUFFERSIZE;
                    return ByteToString(data, data_length);
                }
                else return string.Empty;
            }
        }

        public MessageBuffers()
        {
            BUFFERSIZE = 1024 * 10;
            buffer_in = new byte[BUFFERSIZE];
            buffer_out = new byte[BUFFERSIZE];
        }


        public byte[] ReadNextMessageOut(out int length)
        {
            lock (buffer_out)
            {
                bool ReadOK = false;
                byte[] data = new byte[1024];
                int i = 0;
                length = 0;
                //Read the message in buffer
                if (out_endindex >= out_startindex)
                {
                    int k = 0;
                    for (i = out_startindex; i < out_endindex; i++)
                    {
                        data[k] = buffer_out[i];
                        if (data[k] == '\x03')
                        {
                            ReadOK = true;
                            length = k + 1;
                            break;
                        }
                        k++;
                    }
                }
                else
                {
                    int k = 0;
                    for (i = out_startindex; i < BUFFERSIZE; i++)
                    {
                        data[k] = buffer_out[i];
                        if (data[k] == '\x03')
                        {
                            ReadOK = true;
                            length = k + 1;
                            break;
                        }
                        k++;

                    }
                    if (!ReadOK)
                    {
                        for (i = 0; i < out_endindex; i++)
                        {
                            data[k] = buffer_out[i];
                            if (data[k] == '\x03')
                            {
                                ReadOK = true;
                                length = k + 1;
                                break;
                            }
                            k++;
                        }
                    }
                }

                //update the startindex

                if (ReadOK)
                {
                    out_startindex = out_startindex + length;
                    if (out_startindex >= BUFFERSIZE) out_startindex = out_startindex - BUFFERSIZE;
                    return data;
                }
                else return new byte[0];
            }

        }


        public static string ByteToString(byte[] data, int length)
        {
            return System.Text.ASCIIEncoding.ASCII.GetString(data, 0, length);
        }

        public static byte[] StringToByte(string s)
        {
            return System.Text.ASCIIEncoding.ASCII.GetBytes(s);
        }

        public bool AddToBufferOut(byte[] b)
        {
            lock (buffer_out)
            {
                //check if overflow
                int maxlength;
                if (out_endindex >= out_startindex)
                {
                    maxlength = BUFFERSIZE - out_endindex + out_startindex;
                }
                else
                {
                    maxlength = out_startindex - out_endindex;
                }
                if (maxlength < b.Length)
                {
                    if (!warned)
                    {
                        MessageQueue.Add(new Message()
                        {
                            Direction = MessageDirection.None,
                            Status = MessageStatus.Abandoned,
                            Title = "SMS message buffer is full, some message were dropped.",
                        });
                        warned = true;
                    }
                    return false;
                }
                for (int i = 0; i < b.Length; i++)
                {
                    if (out_endindex >= BUFFERSIZE) out_endindex = 0;
                    buffer_out[out_endindex++] = b[i];
                }
            }
            return true;
        }

        public bool AddToBufferIn(byte[] b)
        {
            lock (buffer_in)
            {
                //check if overflow
                int maxlength;
                if (in_endindex >= in_startindex)
                {
                    maxlength = BUFFERSIZE - in_endindex + in_startindex;
                }
                else
                {
                    maxlength = in_startindex - in_endindex;
                }
                if (maxlength < b.Length)
                {
                    if (!warned)
                    {
                        MessageQueue.Add(new Message()
                        {
                            Direction = MessageDirection.None,
                            Status = MessageStatus.Abandoned,
                            Title = "SMS message buffer is full, some message were dropped.",
                        });
                        warned = true;
                    }
                    return false;
                }
                for (int i = 0; i < b.Length; i++)
                {
                    if (in_endindex >= BUFFERSIZE) in_endindex = 0;
                    buffer_in[in_endindex++] = b[i];
                }
            }
            return true;
        }

    }

}
