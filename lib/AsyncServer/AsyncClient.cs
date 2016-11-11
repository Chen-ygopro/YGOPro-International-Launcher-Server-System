/*
 * 由SharpDevelop创建。
 * 用户： Administrator
 * 日期: 2015/11/10
 * 时间: 9:38
 * 
 * 要改变这种模板请点击 工具|选项|代码编写|编辑标准头文件
 */
using System;
using System.Net.Sockets;

namespace AsyncServer
{
	public delegate void OnReceviceHanlder(AsyncClient sender);
	/// <summary>
	/// Description of AsyncClient.
	/// </summary>
	public class AsyncClient : AClient
    {
        public event OnReceviceHanlder OnRecevice;

        public AsyncClient(TcpClient client):base(client)
		{
		}
		public bool Connect(string host,int port){
			if(m_client == null){
                m_client = new TcpClient();
			}
			if(Connected){
				try{
                    m_client.Close();
				}catch{}
                m_client = new TcpClient();
			}
			try{
                m_client.Connect(host, port);
				return m_client.Connected;
			}catch(Exception e){
				Logger.Warn(e);
			}
			return false;
		}
		
		public void AsyncConnect(string host,int port){
			if(m_client == null){
                m_client = new TcpClient();
			}
			if(Connected){
				try{
                    m_client.Close();
				}catch{}
                m_client = new TcpClient();
			}
			try{
                m_client.BeginConnect(host, port, new AsyncCallback(delegate(IAsyncResult ar){
				                                                  	try{
                        m_client.EndConnect(ar);
				                                                  	}catch{}
				                                                  }), m_client);
			}catch(Exception e){
				Logger.Warn(e);
			}
		}
		
		public void BeginRecevice(){
			if(!Connected)return;
			byte[] m_buff = new byte[1024];
			try{
				m_client.Client.BeginReceive(m_buff, 0, m_buff.Length, SocketFlags.None, new AsyncCallback(EndRecevice), m_buff);
			}catch{}
		}
		private void EndRecevice(IAsyncResult ar){
			try{
				byte[] buff = (byte[])ar.AsyncState;
				int len = m_client.Client.EndReceive(ar);
                PushPacketData((byte[])ar.AsyncState, 0, len);
				if(len != buff.Length){
					if(OnRecevice!=null){
						OnRecevice(this);
					}
				}
			}catch(Exception e){
				Logger.Warn(e);
			}finally{
				BeginRecevice();
			}
		}
		public void WaitSend(byte[] data){
            if (Connected)
            {
                try
                {
                    Client.Client.Send(data);
                }
                catch { }
            }
		}
    }
}
