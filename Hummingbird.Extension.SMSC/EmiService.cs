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
    internal class EMIService : ISocketService
    {
        int totalConnections = 0;
        private static List<ISocketConnection> connections = new List<ISocketConnection>();
        private static int count = 0;
        internal static List<MessageBuffers> Buffers = new List<MessageBuffers>();
        internal static SmsServer referredServer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EMIService"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public EMIService(SmsServer server)
        {
            referredServer = server;
        }

        public void OnConnected(ConnectionEventArgs e)
        {
            if (e.Connection.Host.HostType == HostType.htServer)
            {
                connections.Add(e.Connection);
                MessageBuffers buffer = new MessageBuffers();
                buffer.Connection = e.Connection;
                buffer.Running = true;
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
                Thread t = new Thread(this.RunReceiver);
                t.IsBackground = true;
                t.Start(buffer);
                Thread t2 = new Thread(this.RunSender);
                t2.IsBackground = true;
                t2.Start(buffer);
                Thread t3 = new Thread(this.SendSR);
                t3.IsBackground = true;
                t3.Start(buffer);


            }
            else
            {
                //byte[] b = GetMessage(e.Connection.SocketHandle.ToInt32());
                //e.Connection.BeginSend(b);
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
                        //EmiOperation eo = new EmiOperation(
                        //e.Connection.BeginSend();
                        //BuffertoWrite.Push(e.Buffer);
                        buffer.AddToBufferIn(e.Buffer);
                    }
                    break;
                }

            }

        }

        public void OnSent(MessageEventArgs e)
        {

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
                if (msg != string.Empty)
                {

                    //Response;
                    EMIProtocol eo;
                    try
                    {
                        eo = new EMIProtocol(msg, referredServer);
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

                                if (workingbuffer.AcceptsMT == false)
                                {
                                    response = eo.CreateACK51(false, "07", "Authentication failure");
                                }


                                //CHECK IF THE MT IS PING OR ACK?
                                string MSISDN = eo.AdC;
                                if (MSISDN == string.Empty)
                                {
                                    MTType = EmiMessageType.PING;
                                    m2.Type = EmiMessageType.PING_ACK;
                                    response = eo.CreateACK51(false, "06", "AdC invalid");

                                }
                                else
                                {
                                    //HERE Check The return type;
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

                            //Add MT To Log;

                            EmiMessage m = new EmiMessage();
                            m.Type = MTType;
                            m.Direction = MessageDirection.Incoming;
                            m.CreateDate = new DateTimeOffset(DateTime.Now);
                            m.RAWMessage = msg;
                            m.EMIProtocolObject = eo;

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
                                m2.EMIProtocolObject = new EMIProtocol(response);
                                lock (workingbuffer.MessageToBeSent)
                                {
                                    workingbuffer.MessageToBeSent.Enqueue(m2);
                                    Monitor.Pulse(workingbuffer.MessageToBeSent);
                                }


                                /*lock(MainPage.Messages){
                                    MainPage.Messages.Add(m2);
                                }*/


                                if (m2.Type == EmiMessageType.MT_ACK && SR != string.Empty)
                                {
                                    //AddToBufferOut(StringToByte(SR));



                                    EmiMessage sr = new EmiMessage();
                                    sr.Type = EmiMessageType.SR;
                                    sr.Direction = 0;
                                    sr.CreateDate = new DateTimeOffset(DateTime.Now).AddSeconds(referredServer.srDelay);
                                    sr.RAWMessage = SR;
                                    sr.ExpectedSendDate = new DateTimeOffset(DateTime.Now).AddSeconds(referredServer.srDelay);
                                    sr.EMIProtocolObject = new EMIProtocol(SR);

                                    //Before we sent SR directly, like this ->
                                    //SendMOSRACK(sr);
                                    //Now we stock the SR to a queue, and there will be a thread who will look this queue.
                                    //With this method, the SR can be sent 5 or 10 seconds later.
                                    workingbuffer.SRToBeSent.Enqueue(sr);

                                    //MainPage.Messages.Add(sr);
                                    //SRToBeSent.Push(SR);
                                    //new Thread(new ThreadStart(SendSR)).Start();
                                }

                            }
                            else
                            {
                                //Log that nothing will be replied;
                                /*
                                m2.Direction = 0;
                                m2.CreateDate = new DateTimeOffset(DateTime.Now);
                                m2.RAWMessage = "Nothing has been sent for this MT";
                                lock(MainPage.Messages){
                                    MainPage.Messages.Add(m2);
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

        public void RunSender(object buffer)
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

            /*
            MessageBuffers workingbuffer = (MessageBuffers)buffer;
            while (workingbuffer.Running)
            {
                
                //lock (workingbuffer.MessageToBeSent.SyncRoot)
                //{
                    int j = 0;
                    while (workingbuffer.MessageToBeSent.Count > 0 && j<10)
                    {
                        Message message = (Message)(workingbuffer.MessageToBeSent.Dequeue());
                        Toolkit.AddToMessageQueue.Add(message);
                        workingbuffer.AddToBufferOut(MessageBuffers.StringToByte(message.RAWMessage));
                        j++;
                    }
                //}

                int length = 1;
                while (length > 0)
                {
                    byte[] data = workingbuffer.ReadNextMessageOut(out length);
                    byte[] msg = new byte[length];
                    for (int i = 0; i < length; i++)
                    {
                        msg[i] = data[i];
                    }
                    if (length > 0)
                    {
                        workingbuffer.Connection.BeginSend(msg);
                    }
                    else
                    {
                        //Thread.Sleep(5); //If there is no MO to send, sleep 5 milliseconds
                    }
                }
                Thread.Sleep(5);
            }
             * */
        }

        public void Stop()
        {
            foreach (var buffer in Buffers)
            {
                buffer.Running = false;
            }
        }

        public void SendSR(object buffer)
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
                    // (MT traitement)
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
                        else
                        {

                        }
                    }
                    Thread.Sleep(10);
                }
            }
        }

        public void SendMOSRACK(EmiMessage mo, MessageBuffers buffer)
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
