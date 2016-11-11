using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace AsyncServer
{
    public class AClient : IDisposable
    {
        /// <summary>
        /// Receive queue
        /// </summary>
        protected readonly ArrayQueue<byte> ReceiveQueue = new ArrayQueue<byte>();
        /// <summary>
        /// Send queue
        /// </summary>
        protected readonly Queue<byte[]> m_PendingBuffer = new Queue<byte[]>();
        /// <summary>
        /// Is connected
        /// </summary>
        public bool Connected
        {
            get
            {
                return (m_client != null && m_client.Connected);
            }
        }
        /// <summary>
        /// ipAddress
        /// </summary>
        public System.Net.IPAddress Address
        {
            get
            {
                IPEndPoint point = IPAndPoint;
                if (point != null)
                {
                    return point.Address;
                }
                return IPAddress.None;
            }
        }

        public IPEndPoint IPAndPoint
        {
            get
            {
                IPEndPoint point = null;
                if (m_client != null)
                {
                    point = m_client.Client.RemoteEndPoint as IPEndPoint;
                }
                return point;
            }
        }
        /// <summary>
        /// If asynchronous is
        /// </summary>
        public bool IsSync
        {
            get; protected set;
        }
        public int Available
        {
            get
            {
                int rs = 0;
                try
                {
                    rs = m_client.Available;
                }
                catch { }
                return rs;
            }
        }
        protected TcpClient m_client;
        protected bool _Dispose;

        public TcpClient Client
        {
            get { return m_client; }
        }
        public readonly byte[] SyncRoot = new byte[0];

        public AClient(TcpClient client)
        {
            this.m_client = client;
            this.IsSync = true;
        }
        public void SetSync(bool sync)
        {
            IsSync = sync;
        }
        /// <summary>
        /// 向客户端发送数据
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="isSendNow">是否立即发送</param>
        public void SendPackage(PacketWriter writer, bool isSendNow = true)
        {
            if (writer != null)
            {
                SendPackage(writer.Content, isSendNow);
            }
        }
        /// <summary>
        /// 向客户端发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="isSendNow">是否立即发送</param>
        public void SendPackage(byte[] data, bool isSendNow = true)
        {
            if (!Connected)
                return;
            lock (m_PendingBuffer)
            {
                m_PendingBuffer.Enqueue(data);
            }
            if (isSendNow)
            {
                PeekSend();
            }
        }

        public void Send(byte[] data)
        {
            if (!Connected) return;
            try
            {
                if (IsSync)
                {
                    m_client.Client.BeginSend(data, 0, data.Length, SocketFlags.None,
                                                           new AsyncCallback(SendDataEnd), m_client);
                }
                else
                {
                    m_client.Client.Send(data);
                }
               
            }
            catch (Exception e)
            {
                Logger.Warn(e);
            }
        }
        /// <summary>
        /// 检查队列里是否有要发送的数据，如果有则进行发送处理
        /// </summary>
        public void PeekSend()
        {
            if (!Connected)
            {
                //已经断开
                return;
            }
            Queue<byte[]> arrays = new Queue<byte[]>();
            lock (m_PendingBuffer)
            {
                while (m_PendingBuffer.Count > 0)
                {
                    arrays.Enqueue(m_PendingBuffer.Dequeue());
                }
            }
            if (arrays.Count > 1)
            {
                //Logger.Debug("send "+arrays.Count);
                //   2 个包以上，进行拼包后再发送
                byte[] datas = null;
                using (MemoryStream stream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        while (arrays.Count > 0)
                        {
                            var buff = arrays.Dequeue();
                            writer.Write(buff);
                        }
                    }
                    datas = stream.ToArray();
                }
                if (datas != null && datas.Length > 0)
                {
                    Send(datas);
                    //WriteData(this, datas, isAsync);
                    return;
                }
            }
            else if (arrays.Count == 1)
            {
                //Logger.Debug("send 1");
                var data = arrays.Dequeue();
                if (data != null && data.Length > 0)
                {
                    Send(data);
                    //WriteData(this, data, isAsync);
                    return;
                }
            }
            else {
                //Logger.Debug("nothing send");
            }
        }
        protected void SendDataEnd(IAsyncResult ar)
        {
            try
            {
                TcpClient client = (TcpClient)ar.AsyncState;
                client.Client.EndSend(ar);
            }
            catch (Exception e)
            {
                m_client.Close();
                Logger.Warn(e);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="GamePacketByteLength">包的长度2/4（ushort/uint）</param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool GetPacketData(int GamePacketByteLength, out byte[] data)
        {
            lock (ReceiveQueue)
            {
                if (GamePacketByteLength != 2)
                {
                    GamePacketByteLength = 4;
                }
                if (ReceiveQueue.Count > GamePacketByteLength)
                {
                    byte[] blen = new byte[GamePacketByteLength];
                    ReceiveQueue.Dequeue(blen);
                    uint len = (GamePacketByteLength == 2) ? BitConverter.ToUInt16(blen, 0) : BitConverter.ToUInt32(blen, 0);
                    if (len == 0)
                    {
                        data = new byte[0];
                        return true;
                    }
                    int lastcount = ReceiveQueue.Count;
                    if (lastcount >= len)
                    {
                        data = new byte[len];
                        ReceiveQueue.Dequeue(data);
                        return true;
                        //Logger.Debug("add packet");
                    }
                    else {
                        byte[] tmpdata = new byte[lastcount];
                        ReceiveQueue.Dequeue(tmpdata);
                        ReceiveQueue.Enqueue(blen);
                        ReceiveQueue.Enqueue(tmpdata);
                    }
                }
            }
            data = new byte[0];
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void PushPacketData(byte[] data, int start = 0, int length = -1)
        {
            lock (ReceiveQueue)
            {
                ReceiveQueue.Enqueue(data, start, length);
            }
        }
        public virtual void Close()
        {
            Dispose();
        }
        public virtual void Dispose()
        {
            if (_Dispose) return;
            _Dispose = true;
            try
            {
                if (m_client != null)
                {
                    m_client.Close();
                }
            }
            catch { }
            m_client = null;
        }
    }
}
