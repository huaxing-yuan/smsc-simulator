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
    /// Description of EMIService.
    /// </summary>
    internal class EmiService : ISocketService
    {
        int totalConnections = 0;
        private static List<ISocketConnection> connections = new List<ISocketConnection>();
        private static int count = 0;
        internal readonly static List<MessageBuffers> Buffers = new List<MessageBuffers>();
        internal static SmsServer referredServer { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmiService"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public EmiService(SmsServer server)
        {
            referredServer = server;
        }

        public void OnConnected(ConnectionEventArgs e)
        {
            if (e.Connection.Host.HostType == HostType.htServer)
            {
                connections.Add(e.Connection);
                MessageBuffers buffer = new MessageBuffers
                {
                    Connection = e.Connection,
                    Running = true
                };
                Buffers.Add(buffer);
                e.Connection.BeginReceive();
                MessageQueue.Add(new Message()
                {
                    Direction = MessageDirection.None,
                    Title = string.Format("SMSC (EMI): The client from {0} connected to this server, remote port: {1}", e.Connection.RemoteEndPoint.Address.ToString(), e.Connection.RemoteEndPoint.Port),
                    Status = MessageStatus.None
                });
                totalConnections++;
                referredServer.SendRequestEnabled(true, null);
                Thread t = new Thread(RunReceiver)
                {
                    IsBackground = true
                };
                t.Start(buffer);
                Thread t2 = new Thread(RunSender)
                {
                    IsBackground = true
                };
                t2.Start(buffer);
                Thread t3 = new Thread(SendSR)
                {
                    IsBackground = true
                };
                t3.Start(buffer);


            }
            else
            {
                //byte[] b = GetMessage(e.Connection.SocketHandle.ToInt32())
                //e.Connection.BeginSend(b)
            }


        }

        public void OnReceived(MessageEventArgs e)
        {
            foreach (var buffer in Buffers)
            {
                if (buffer.Connection.ConnectionId == e.Connection.ConnectionId)
                {
                    if (e.Connection.Host.HostType == HostType.htServer)
                    {
                        buffer.AddToBufferIn(e.Buffer);
                    }
                    break;
                }

            }

        }

        public void OnSent(MessageEventArgs e)
        {
            //nothing to process here.
        }

        public void OnDisconnected(ConnectionEventArgs e)
        {

            foreach (var buffer in Buffers)
            {
                if (e.Connection.ConnectionId == buffer.Connection.ConnectionId)
                {
                    totalConnections--;
                    buffer.Running = false;
                    break;
                }
            }
            if (totalConnections == 0)
            {
                referredServer.SendRequestEnabled(false, "No more alive socket connections to the SMSC. before send MOs, you must connect an EMI client");
            }
        }

        public void OnException(ExceptionEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void RunReceiver(object buffer)
        {
            MessageBuffers workingbuffer = (MessageBuffers)buffer;
            string remoteaddress = workingbuffer.Connection.RemoteEndPoint.Address.ToString();
            while (workingbuffer.Running)
            {
                string msg = workingbuffer.ReadNextMessageIN();
                if (!string.IsNullOrEmpty(msg))
                {

                    //Response
                    EmiProtocol eo;
                    try
                    {
                        eo = new EmiProtocol(msg, referredServer);
                        string operation = eo.OT;
                        string response = string.Empty;
                        string SR = string.Empty;
                        EmiMessageType MTType = EmiMessageType.SYS;
                        EmiMessage m2 = new EmiMessage();

                        if (operation != null)
                        {

                            if (eo.OT == "51")
                            {
                                MTType = EmiMessageType.MT;

                                if (!workingbuffer.AcceptsMT)
                                {
                                    response = eo.CreateACK51(false, "07", "Authentication failure");
                                }


                                //CHECK IF THE MT IS PING OR ACK?
                                string MSISDN = eo.AdC;
                                if (string.IsNullOrEmpty(MSISDN))
                                {
                                    MTType = EmiMessageType.PING;
                                    m2.Type = EmiMessageType.PING_ACK;
                                    response = eo.CreateACK51(false, "06", "AdC invalid");

                                }
                                else
                                {
                                    //HERE Check The return type
                                    switch (referredServer.mtBehavior)
                                    {
                                        case MTBehavior.ACK:
                                            response = eo.CreateACK51(true, string.Empty, string.Empty);
                                            m2.Type = EmiMessageType.MT_ACK;

                                            //CHECK if SR is demanded
                                            if (referredServer.srActive)
                                            {
                                                SR = eo.CreateSRForMT(referredServer.srDst, referredServer.srRsn);
                                            }
                                            else
                                            {
                                                SR = string.Empty;
                                            }

                                            break;
                                        case MTBehavior.NACK:
                                            response = eo.CreateACK51(false, referredServer.nackCode.ToString(), string.Empty);
                                            m2.Type = EmiMessageType.MT_NACK;
                                            break;
                                        case MTBehavior.Nothing:
                                            response = string.Empty;
                                            m2.Type = EmiMessageType.SYS;
                                            break;
                                    }
                                }
                            }
                            else if (eo.OT == "52")
                            {
                                MTType = EmiMessageType.MO_ACK;
                                response = string.Empty;
                            }
                            else if (eo.OT == "53")
                            {
                                MTType = EmiMessageType.SR_ACK;
                                response = string.Empty;
                            }
                            else if (eo.OT == "60")
                            {
                                MTType = EmiMessageType.SESSION;
                                response = eo.CreateACK60(true, string.Empty);
                                m2.Type = EmiMessageType.SESSION_ACK;
                                workingbuffer.AcceptsMT = true;
                            }

                            //Add MT To Log

                            EmiMessage m = new EmiMessage
                            {
                                Type = MTType,
                                Direction = MessageDirection.Incoming,
                                CreateDate = new DateTimeOffset(DateTime.Now),
                                RAWMessage = msg,
                                EMIProtocolObject = eo
                            };

                            AbstractMetadata metadata = null;
                            if (m.Type == EmiMessageType.MT)
                            {
                                metadata = referredServer.MTMetadata;
                            }
                            else if (m.Type == EmiMessageType.SESSION)
                            {
                                metadata = referredServer.SSMetadata;
                            }


                            //Do not log if hidePingACK is true and MessageType is Ping or ACK.
                            if (!(referredServer.hidePingACK && (m.Type == EmiMessageType.PING || m.Type == EmiMessageType.MO_ACK || m.Type == EmiMessageType.SR_ACK)))
                            {
                                MessageQueue.Add(
                                    new Message()
                                    {
                                        Metadata = referredServer.MTMetadata,
                                        Title = m.FriendlyMessage,
                                        Direction = MessageDirection.Incoming,
                                        RequestObject = m.EMIProtocolObject,
                                        RequestText = m.RAWMessage,
                                        Status = MessageStatus.Received,
                                        Tag = m.RAWMessage
                                    });
                            }

                            if (response.StartsWith("\x02"))
                            {
                                //Add ACK/NACK to buffer:
                                m2.Direction = 0;
                                m2.CreateDate = new DateTimeOffset(DateTime.Now);
                                m2.RAWMessage = response;
                                m2.EMIProtocolObject = new EmiProtocol(response);
                                lock (workingbuffer.MessageToBeSent)
                                {
                                    workingbuffer.MessageToBeSent.Enqueue(m2);
                                    Monitor.Pulse(workingbuffer.MessageToBeSent);
                                }




                                if (m2.Type == EmiMessageType.MT_ACK && !string.IsNullOrEmpty(SR))
                                {
                                    EmiMessage sr = new EmiMessage
                                    {
                                        Type = EmiMessageType.SR,
                                        Direction = 0,
                                        CreateDate = new DateTimeOffset(DateTime.Now).AddSeconds(referredServer.srDelay),
                                        RAWMessage = SR,
                                        ExpectedSendDate = new DateTimeOffset(DateTime.Now).AddSeconds(referredServer.srDelay),
                                        EMIProtocolObject = new EmiProtocol(SR)
                                    };

                                    //Before we sent SR directly, like this ->
                                    //SendMOSRACK(sr)
                                    //Now we stock the SR to a queue, and there will be a thread who will look this queue.
                                    //With this method, the SR can be sent 5 or 10 seconds later.
                                    workingbuffer.SRToBeSent.Enqueue(sr);

                                }

                            }
                            else
                            {
                                //Log that nothing will be replied
                                /*
                                m2.Direction = 0
                                m2.CreateDate = new DateTimeOffset(DateTime.Now)
                                m2.RAWMessage = "Nothing has been sent for this MT"
                                lock(MainPage.Messages){
                                    MainPage.Messages.Add(m2)
                                }			
                                */
                            }
                            Thread.Sleep(10);
                        }
                    }
                    catch
                    {
                        //EMIProtocol is not valid
                    }

                }
                else
                {
                    Thread.Sleep(100); //If there is no MT to receive, sleep 10 milliseconds
                }
            }

            MessageQueue.Add(new Message()
            {
                Direction = MessageDirection.None,
                Status = MessageStatus.Received,
                Title = string.Format("SMSC: The client from {0} disconnected from this SMS Center", remoteaddress),
            });
            connections.Remove(workingbuffer.Connection);
            Buffers.Remove(workingbuffer);
        }

        internal static void RunSender(object buffer)
        {

            MessageBuffers workingbuffer = (MessageBuffers)buffer;
            while (workingbuffer.Running)
            {
                lock (workingbuffer.MessageToBeSent)
                {
                    if (workingbuffer.MessageToBeSent.Count == 0)
                        Monitor.Wait(workingbuffer.MessageToBeSent);

                    while (workingbuffer.MessageToBeSent.Count > 0)
                    {
                        EmiMessage message = (EmiMessage)workingbuffer.MessageToBeSent.Dequeue();
                        AbstractMetadata metadata = null;

                        if (message.Type == EmiMessageType.MO)
                        {
                            metadata = referredServer.MOMetadata;
                        }
                        else if (message.Type == EmiMessageType.SR)
                        {
                            metadata = referredServer.SRMetadata;
                        }
                        else
                        {
                            metadata = referredServer.MTMetadata;
                        }
                        workingbuffer.Connection.BeginSend(MessageBuffers.StringToByte(message.RAWMessage));

                        
                        //If HidePingACK is True and MessageType = MT_ACK. do not add to message list.
                        if (!(referredServer.hidePingACK && (message.Type != EmiMessageType.MT_ACK || message.Type != EmiMessageType.PING_ACK)))
                        {
                            Message log = message.Message ?? new Message();
                            log.Metadata = metadata;
                            log.Title = message.FriendlyMessage;
                            log.RequestObject = message.EMIProtocolObject;
                            log.RequestText = message.RAWMessage;
                            log.Status = MessageStatus.Sent;
                            log.Tag = message.RAWMessage;
                            MessageQueue.Add(log);
                        }
                        
                    }

                }
            }

                   }

        public static void Stop()
        {
            foreach (var buffer in Buffers)
            {
                buffer.Running = false;
            }
        }

        internal static void SendSR(object buffer)
        {
            MessageBuffers workingbuffer = (MessageBuffers)buffer;
            while (workingbuffer.Running)
            {
                DateTimeOffset currentdate = new DateTimeOffset(DateTime.Now);

                if (workingbuffer.NextCheckingDate >= currentdate)
                {
                    //Why sleep so long?
                    //because if we dont sleep, there will be a "Lock" at SRToBeSent
                    //to check the first SR to be sent
                    //the "Lock" will block another thread:
                    // (MT process)
                    //So if no SR to be sent, we can sleep some time and let MTThread to receive MT...
                    Thread.Sleep(100);
                }
                else
                {
                    lock (workingbuffer.SRToBeSent)
                    {
                        if (workingbuffer.SRToBeSent.Count > 0)
                        {
                            lock (workingbuffer.SRToBeSent.SyncRoot)
                            {
                                while (workingbuffer.SRToBeSent.Count > 0)
                                {
                                    EmiMessage m = workingbuffer.SRToBeSent.Peek() as EmiMessage;
                                    if (m.ExpectedSendDate <= new DateTimeOffset(DateTime.Now))
                                    {
                                        m = workingbuffer.SRToBeSent.Dequeue() as EmiMessage;
                                        lock (workingbuffer.MessageToBeSent)
                                        {
                                            workingbuffer.MessageToBeSent.Enqueue(m);
                                            Monitor.Pulse(workingbuffer.MessageToBeSent);
                                        }
                                    }
                                    else
                                    {
                                        workingbuffer.NextCheckingDate = m.ExpectedSendDate;
                                        break;
                                    }
                                    if (!workingbuffer.Running) return;
                                }
                            }
                        }
                    }
                    Thread.Sleep(10);
                }
            }
        }

        internal static void SendMOSRACK(EmiMessage mo, MessageBuffers buffer)
        {
            if (buffer != null)
            {

                buffer.MessageToBeSent.Enqueue(mo);
            }
            else
            {
                if (Buffers.Count > 0)
                {
                    int c = count % Buffers.Count;
                    lock (Buffers[c].MessageToBeSent)
                    {
                        Buffers[c].MessageToBeSent.Enqueue(mo);
                        Monitor.Pulse(Buffers[c].MessageToBeSent);
                    }
                    count++;
                }
            }
        }
    }

}
