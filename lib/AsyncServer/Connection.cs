using System;
using System.Timers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace AsyncServer {
	#region TimeoutTimer
	/// <summary>
	/// Timeout Timer
	/// </summary>
	public class TimeoutTimer : Timer {
		private object tag;
		
		public object Tag {
			get { return tag; }
		}
		
		public TimeoutTimer(object tag) : base() {
			this.tag = tag;
		}
		
		public void Restart() {
			Stop();
			Start();
		}
	}
	#endregion
	
	public enum ConnectStatu{
		Uncheck =0,
		Success,
		Fail
	}
	/// <summary>
	/// Represents an Asterion client connection.
	/// </summary>
	public class Connection<T> : AClient
    {

        public ConnectStatu Statu { get; internal set; }

        internal byte[] Bytes;
        internal byte[] ResetCache()
        {
            Bytes = new byte[1024];
            return Bytes;
        }
        /// <summary>
        /// Gets the timeout timer.
        /// </summary>
        /// <value>The timeout timer.</value>
        public TimeoutTimer Timer { get; internal set; }
        public T Tag { get; set; }
        /// <summary>
        /// Initializes a new instance of the Connection class.
        /// </summary>
        public Connection(TcpClient client):base(client) {
            Timer = new TimeoutTimer(this);
			Statu = ConnectStatu.Uncheck;
		}
    }
}