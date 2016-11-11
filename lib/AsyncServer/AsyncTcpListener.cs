using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Timers;
using System.Net;
using System.IO;

namespace AsyncServer{
	
	/// <summary>
	/// Implaments an asynchronous TCP server.
	/// </summary>
	public class AsyncTcpListener<T> {
		
		#region delegate
		/// <summary>
		/// Receive event handler.
		/// </summary>
		public delegate void ReceiveEventHandler(Connection<T> Client);
		/// <summary>
		/// Connect event handler.
		/// </summary>
		public delegate void ConnectEventHandler(Connection<T> Client);
		/// <summary>
		/// Disconnect event handler.
		/// </summary>
		public delegate void DisconnectEventHandler(Connection<T> Client);
		/// <summary>
		/// Check Client frist
		/// </summary>
		public delegate ConnectStatu CheckEventHandler(Connection<T> Client);
		#endregion
		
		#region private member
		protected readonly List<IPAddress> BanIpList = new List<IPAddress>();
		/// <summary>
		/// The listening socket.
		/// </summary>
		private TcpListener listener;
		/// <summary>
		/// The host the server will listen on.
		/// </summary>
		private IPAddress host;
		/// <summary>
		/// The port the server will listen on.
		/// </summary>
		private int port;
		/// <summary>
		/// Client timeout. The maximum amount of time a client is permitted not to send data for.
		/// </summary>
		private double timeout = 0;
		/// <summary>
		/// Whether the server has been started.
		/// </summary>
		public bool Started = false;
		
		private readonly List<Connection<T>> m_clients = new List<Connection<T>>();
		/// <summary>
		/// Occurs when a packet has been received.
		/// </summary>
		public event ReceiveEventHandler OnReceive;
		/// <summary>
		/// Occurs when a client has connected.
		/// </summary>
		public event ConnectEventHandler OnConnect;
		/// <summary>
		/// Occurs when a client disconnects.
		/// </summary>
		public event DisconnectEventHandler OnDisconnect;
		/// <summary>
		/// check client first recevice
		/// </summary>
		public event CheckEventHandler OnCheckClient;
		#endregion
		
		#region public member
		public List<Connection<T>> Clients{
			get{return m_clients;}
		}
		/// <summary>
		/// Gets the host the server is listening in.
		/// </summary>
		/// <value>The host.</value>
		public IPAddress Host {
			get { return host; }
		}
		/// <summary>
		/// Gets the port the server is listening on.
		/// </summary>
		/// <value>The port.</value>
		public int Port {
			get { return port; }
		}
		/// <summary>
		/// Gets the count of clients currently connected to the server.
		/// </summary>
		/// <value>The count.</value>
		public int Count {
			get { return m_clients.Count; }
		}
		/// <summary>
		/// Gets the timeout time.
		/// </summary>
		/// <value>The timeout time.</value>
		public double Timeout {
			get { return timeout; }
		}
		protected bool m_banmode;
		#endregion
		
		#region Initializes
		/// <summary>
		/// Initializes a new instance of the <see cref="Asterion.Server"/> class.
		/// </summary>
		/// <param name="host">Host to listen on.</param>
		/// <param name="port">Port to listen on.</param>
		/// <param name="timeout">Client timeout time.</param>
		/// <param name="banmode">ban IPAddress</param>
		public AsyncTcpListener(IPAddress host, int port, int timeout = 0,bool banmode = false) {
			this.m_banmode = banmode;
			Init(host, port, timeout);
		}
		
		internal void Init(IPAddress host, int port,int timeout){
			this.host = host;
			this.port = port;
			this.timeout = timeout;
			listener = new TcpListener(host, port);
		}
		#endregion

		#region private method
		/// <summary>
		/// Listens for incoming connections.
		/// </summary>
		private void Listen() {
			listener.Start();
			listener.Server.UseOnlyOverlappedIO = true;
			listener.BeginAcceptTcpClient(AcceptCallback, null);
		}


		/// <summary>
		/// Callback for an incoming connection
		/// </summary>
		/// <param name="result">Asynchronous result.</param>
		private void AcceptCallback(System.IAsyncResult result) {
			if(!Started) return;
			TcpClient client  = null;
			try {
				client = listener.EndAcceptTcpClient(result);
				client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
				Heard(client);
			}catch(SocketException ex) {
				// If this happens, socket error code information is at: http://msdn.microsoft.com/en-us/library/windows/desktop/ms740668(v=vs.85).aspx
				Logger.Error("Could not accept socket (" + ex.ErrorCode.ToString() + "): " + ex.ToString());
				if(client!=null){
					DisconnectClient(client);
				}
			}catch(Exception ex) {
				// Either the server is full or the client has reached the maximum connections per IP.
				Logger.Error("Could not add client: " + ex.ToString());
				if(client!=null){
					DisconnectClient(client);
				}
			}finally{
			}
			if(Started){
				listener.BeginAcceptTcpClient(AcceptCallback, null);
			}
		}


		/// <summary>
		/// Handles a new client connecting to the server and starts reading from the client.
		/// </summary>
		/// <param name="client">The new client.</param>
		private void Heard(TcpClient client) {
			Connection<T> connection = new Connection<T>(client);
			lock(m_clients) {
				m_clients.Add(connection);
			}
			if(IsBan(connection.Address)){
				DisconnectClient(connection);
				Logger.Warn("ban ip :"+connection.Address+"  connect");
				return;
			}
			if(timeout != 0) {
				connection.Timer.Interval = timeout;
				connection.Timer.Elapsed += OnTimeoutEventHandler;
				connection.Timer.Start();
			}
			Connected(connection);
			BeginRead(connection);
		}
		/// <summary>
		/// Timer elapse event handler, raised only when the client has timed out
		/// </summary>
		/// <param name="source">The timer that elapsed.</param>
		/// <param name="e">Event arguments.</param>
		private void OnTimeoutEventHandler(object source, ElapsedEventArgs e) {
			TimeoutTimer timer = (TimeoutTimer) source;
			timer.Stop();
			Connection<T> connection = (Connection<T>) timer.Tag;
			DisconnectClient(connection);
		}
		/// <summary>
		/// Begins reading from a connected client.
		/// </summary>
		/// <param name="connection">The connection to read from.</param>
		private void BeginRead(Connection<T> connection) {
			if(connection.Statu == ConnectStatu.Fail){
				Logger.Warn("Illegal client ip :"+connection.Address);
				DisconnectClient(connection);
				return;
			}
			try {
				byte[] cache = connection.ResetCache();
				lock(connection.SyncRoot)
					if(connection.Connected)
						connection.Client.GetStream().BeginRead(cache, 0, cache.Length, ReceiveCallback, connection);
					else
						DisconnectHandler(connection);
			}catch(System.IO.IOException) {
				DisconnectHandler(connection);
			}
		}

		/// <summary>
		/// Callback for received data.
		/// </summary>
		/// <param name="result">Asynchronous result.</param>
		private void ReceiveCallback(System.IAsyncResult result) {
			Connection<T> connection = (Connection<T>) result.AsyncState;
			int read = 0;
			bool connected = false;
			int available = 0;
			lock(connection.SyncRoot)
				if(connection.Connected) {
				read = EndRead(connection, result);
				connected = connection.Client.Connected;
				available = connection.Client.Available;
			}
			//	Logger.Debug("receive:"+read);
			if (read != 0 && connected) {
                connection.PushPacketData(connection.Bytes, 0, read);
				if (read == connection.Bytes.Length) {
					//还有内容
					if(connection.Statu == ConnectStatu.Uncheck && OnCheckClient != null){
						connection.Statu = OnCheckClient(connection);
					}
				} else {
					if (available == 0) {
						//first packet
						if(connection.Statu == ConnectStatu.Uncheck && OnCheckClient != null){
							connection.Statu = OnCheckClient(connection);
						}
						Received(connection);
					}else{
						
					}
				}
				if(connection.Statu == ConnectStatu.Fail){
					if(BanAddress(connection.Address)){
						Logger.Warn("ban ip :"+connection.Address);
					}
					DisconnectClient(connection);
					return;
				}
				if (timeout > 0) {
					connection.Timer.Restart();
				}
				if (Started) {
					BeginRead(connection);
				}
			}else{
				DisconnectHandler(connection);
			}
		}

		/// <summary>
		/// Ends reading from a client.
		/// </summary>
		/// <returns>The read.</returns>
		/// <param name="connection">The connection to end reading from.</param>
		/// <param name="result">Asynchronous result.</param>
		private int EndRead(Connection<T> connection, System.IAsyncResult result) {
			try {
				lock(connection.SyncRoot)
					if(connection.Connected)
						return connection.Client.GetStream().EndRead(result);
					else
						return 0;
			}catch(System.IO.IOException) {
				return 0;
			}
		}

		/// <summary>
		/// Handles a client disconnect
		/// </summary>
		/// <param name="connection">Disconnected connection.</param>
		private void DisconnectHandler(Connection<T> connection) {
			lock(m_clients) {
				if(connection!=null)
					m_clients.Remove(connection);
			}
			Disconnected(connection);
			try{
				if(timeout>0){
					connection.Timer.Stop();
					connection.Timer.Close();
				}
				connection.Client.Close();
			}catch(Exception){
				//Logger.Error("Close error:"+e.ToString());
			}
		}
		private bool IsBan(IPAddress addr){
			if(!m_banmode) return false;
			if(addr==null||addr==IPAddress.None){
				return false;
			}
			lock(BanIpList){
				return BanIpList.Contains(addr);
			}
		}
		private void ReadBanAddress(){
			if(!m_banmode) return;
			string file  = "banip_"+port+".txt";
			if(File.Exists(file)){
				string[] lines  = File.ReadAllLines(file);
				foreach(string line in lines){
					if(line!=null&&!line.StartsWith("#")){
						IPAddress addr = null;
						try{
							addr = IPAddress.Parse(line.Trim());
						}catch(Exception){
							
						}
						if(addr!=null){
							lock(BanIpList){
								if(!BanIpList.Contains(addr)){
									BanIpList.Add(addr);
								}
							}
						}
					}
				}
			}
		}
		private bool BanAddress(IPAddress addr){
			bool statu = false;
			if(!m_banmode) return statu;
			lock(BanIpList){
				if(!BanIpList.Contains(addr)){
					BanIpList.Add(addr);
					statu = true;
				}
			}
			if(statu){
				try{
					File.WriteAllText("banip_"+port+".txt", addr + Environment.NewLine);
				}catch(Exception){
				}
			}
			return statu;
		}
		#endregion

		#region start/close
		/// <summary>
		/// Starts the server.
		/// </summary>
		public void Start() {
			if(!Started) {
				Started = true;
				if(m_banmode){
					ReadBanAddress();
				}
				Listen();
			}
		}
		
		/// <summary>
		/// Disconnects the connection from the server.
		/// </summary>
		/// <param name="connection">The connection to disconnect.</param>
		public void DisconnectClient(Connection<T> connection) {
			try {
				lock(connection.SyncRoot)
					if(connection.Connected) {
					connection.Client.Client.Shutdown(SocketShutdown.Both);
					connection.Client.Close();
				}
			}catch(System.Exception e) {
				Logger.Error("Could not disconnect socket: " + e.ToString());
			}
		}
		private void DisconnectClient(TcpClient connection) {
			try {
				if(connection.Connected) {
					connection.Client.Shutdown(SocketShutdown.Both);
					connection.Client.Close();
				}
			}catch(System.Exception e) {
				Logger.Error("Could not disconnect socket: " + e.ToString());
			}
		}
		public void Stop(){
			Started = false;
			lock(m_clients){
				foreach(Connection<T> client in m_clients){
					client.Close();
				}
			}
			listener.Stop();
		}
		#endregion
		
		#region Events
		/// <summary>
		/// Raises the OnConnect event.
		/// </summary>
		/// <param name="client">Client.</param>
		private void Connected(Connection<T> client) {
			if(OnConnect != null) OnConnect(client);
		}
		
		/// <summary>
		/// Raises the on receive event.
		/// </summary>
		/// <param name="client">Client.</param>
		/// <param name="packet">Packet.</param>
		private void Received(Connection<T> client) {
			if(OnReceive != null) OnReceive(client);
		}
		
		/// <summary>
		/// Raises the OnDisconnect event.
		/// </summary>
		/// <param name="client">Client.</param>
		private void Disconnected(Connection<T> client) {
			if(OnDisconnect != null) OnDisconnect(client);
		}
		#endregion
	}

}