/*
 * 由SharpDevelop创建。
 * 用户： Administrator
 * 日期: 2015/11/13
 * 时间: 11:20
 * 
 * 要改变这种模板请点击 工具|选项|代码编写|编辑标准头文件
 */
using System;
using AsyncServer;
using System.Collections.Generic;
using System.IO;
using YGOCore;

namespace YGOCore
{
	/// <summary>
	/// Description of ClinetEvent.
	/// </summary>
	public static class ClinetEvent
	{
		#region Message match
		static readonly EventHandler<ushort, Session, PacketReader> EventHandler = new EventHandler<ushort, Session, PacketReader>();

		static ClinetEvent()
		{
			RegisterEvents();
		}
		static void RegisterEvents()
		{
			//EventHandler.Register((ushort)RoomMessage.Error, 	OnError);
			EventHandler.Register((ushort)RoomMessage.RoomList, OnRoomList);
			EventHandler.Register((ushort)RoomMessage.Info, OnInfo);
			EventHandler.Register((ushort)RoomMessage.Chat, OnChat);
			EventHandler.Register((ushort)RoomMessage.Pause, OnPause);
			EventHandler.Register((ushort)RoomMessage.PlayerList, OnPlayerList);
			EventHandler.Register((ushort)RoomMessage.NETWORK_CLIENT_ID, OnRoomList2);
			EventHandler.Register((ushort)RoomMessage.STOP_CLIENT, OnClose);
			//ygopot
			//   EventHandler.Register((ushort)RoomMessage.PlayerInfo, OnInfo);
			EventHandler.Register((ushort)RoomMessage.PlayerInfo, OnGameConnect);
			EventHandler.Register((ushort)RoomMessage.CreateGame, On302);
			EventHandler.Register((ushort)RoomMessage.OnGameChat, OnGameChat);
			EventHandler.Register((ushort)RoomMessage.JoinGame, On302);
			//EventHandler.Register((ushort)RoomMessage.SystemChat,	OnSystemChat);
			//EventHandler.Register((ushort)RoomMessage.RoomCreate,	OnRoomCreate);
			//EventHandler.Register((ushort)RoomMessage.RoomStart,	OnRoomStart);
			//EventHandler.Register((ushort)RoomMessage.RoomClose,	OnRoomClose);
		}
		public static RoomMessage Handler(Session session, params PacketReader[] packets)
		{
			RoomMessage msg = RoomMessage.Unknown;
			foreach (PacketReader packet in packets)
			{
				//			Parse(player, packet);
				ushort id = packet.ReadByte();
				if (msg == RoomMessage.Unknown)
				{
					if (RoomMessage.IsDefined(typeof(RoomMessage), (byte)id))
					{
						msg = (RoomMessage)id;
					}
					else
					{
						Logger.Warn("unknown id:" + id);
					}
				}
				if ((session.IsLogin || session.IsClient) || (msg == RoomMessage.Info && session.Name == null) || msg == RoomMessage.PlayerInfo || msg == RoomMessage.NETWORK_CLIENT_ID)
				{
					EventHandler.Do(id, session, packet);
				}
				else
				{
					Logger.Warn("don't deal id:" + msg);
				}
				packet.Close();
				//}
			}
			return msg;
		}
		#endregion

		#region Processing client messages
		public static void LobbyError(this Session session, string message)
		{
			using (PacketWriter join = new PacketWriter(2))
			{
				join.Write((byte)RoomMessage.JoinGame);
				join.Write(0U);
				join.Write((byte)0);
				join.Write((byte)0);
				join.Write(0);
				join.Write(0);
				join.Write(0);
				// C++ padding: 5 bytes + 3 bytes = 8 bytes
				for (int i = 0; i < 3; i++)
					join.Write((byte)0);
				join.Write(0);
				join.Write((byte)0);
				join.Write((byte)0);
				join.Write((short)0);
				session.Send(join, false);
			}

			using (PacketWriter enter = new PacketWriter(2))
			{
				enter.Write((byte)RoomMessage.HsPlayerEnter);
				enter.WriteUnicode("[err]" + message, 20);
				enter.Write((byte)0);
				session.Send(enter);
			}
		}
		private static void OnGameChat(Session session, PacketReader packet)
		{
			session.CanGameChat = true;
			string msg = packet.ReadUnicode();
			//  Logger.Info(session.Name+":"+msg);
			if (session.Server != null && !string.IsNullOrEmpty(msg))
			{
				session.Server.OnChatMessage(session.Name, "", msg);
			}
		}
		private static void OnGameConnect(Session session, PacketReader packet)
		{
			session.IsClient = true;
			string tag = packet.ReadUnicode(20);
			string pwd = Password.GetPwd(tag);
			string name= Password.OnlyName(tag);
			session.Name =name;
			if(!Login(name, pwd)){
				session.LobbyError("Login fail");
				return;
			}
			session.CreateToken(session.Name, Password.GetPwd(tag));
			session.Server.SendToken(session.Name, session.Token);
			if (session.ip != null)
			{
				lock (session.Server.GameCliens)
				{
					if (!session.Server.GameCliens.ContainsKey(session.ip))
					{
						session.Server.GameCliens.Add(session.ip, session);
					}
				}
			}
		}
		private static void SendMessage(this Session session, string msg)
		{
			using (PacketWriter chat = new PacketWriter(2))
			{
				chat.Write((byte)RoomMessage.OnChat);
				//PlayerType.Yellow
				chat.Write((short)0x10);
				chat.WriteUnicode(msg);
				session.Send(chat);
			}
		}
		private static void On302(Session session, PacketReader packet)
		{
			session.LobbyError("This is the chat room port");
			DuelServer srv = session.Server.GetMinServer();
			session.SendMessage("It's chat ports and say a Word, and all chats。");
			session.SendMessage("Your short passwords are:"+session.Token+" (User name:"+session.Name+"$"+session.Token+")");
			if (srv != null && srv.Port > 0)
			{
				session.SendMessage("Recommended game port:" + srv.Port);
			}
			List<int> ports = session.Server.GetAllPorts();
			string msg = "All the game port:";
			foreach (int p in ports)
			{
				msg += p + ",";
			}
			session.SendMessage(msg);
			//  session.Close();
		}
		private static bool Login(string name, string pwd)
		{
			return true;
		}
		private static void OnPause(Session session, PacketReader packet)
		{
			session.IsPause = true;
		}

		private static void OnClose(Session session, PacketReader packet)
		{
			session.IsPause = true;
			session.Close();
		}
		private static void OnRoomList2(Session session, PacketReader packet)
		{
			Logger.Debug("OnRoomList2");
			session.IsPause = false;
			if (session.Server != null)
			{
				session.Server.SendRoomList(session);
			}
		}
		private static void OnRoomList(Session session, PacketReader packet)
		{
			bool nolock = packet.ReadBoolean();
			bool nostart = packet.ReadBoolean();
			session.IsPause = false;
			if (session.Server != null)
			{
				session.Server.OnRoomList(session, nolock, nostart);
			}
		}
		private static void OnPlayerList(Session session, PacketReader packet)
		{
			session.OnPlayerList();
		}
		//Login
		private static void OnInfo(Session session, PacketReader packet)
		{
			string name = packet.ReadUnicode(20);
			string pwd = packet.ReadUnicode(32);//md5
			//Login
			bool force = packet.ReadBoolean();
			if (Login(name, pwd))
			{
				session.Name = name;
				session.IsPause = false;
				//Back chat ports, game ports
				if (session.Server != null)
				{
					lock (session.Server.Clients)
					{
						Session old;
						if (session.Server.Clients.TryGetValue(session.Name, out old))
						{
							if (force)
							{
								old.SendError("[err] You are already loggedin");
								old.Name = "";
								//   old.Close();
							}
							else
							{
								session.IsLogin = false;
								session.IsPause = true;
								session.SendError("[err]Already logged on");
								return;
							}
						}
						session.CreateToken(name, pwd);
						session.Server.SendToken(session.Name, session.Token);
						session.IsLogin = true;
						session.Server.Clients[session.Name]=session;
						session.Server.OnSendServerInfo(session);
						session.Server.server_OnPlayerJoin(session.ServerInfo, session.Name, null);
						if (!string.IsNullOrEmpty(session.Server.Tip))
						{
							session.SendServerMsg(session.Server.Tip);
						}
					}
				}
			}
			else {
				session.SendError("[err]Authentication failed");
			}
		}
		private static void OnChat(Session session, PacketReader packet)
		{
			string name = packet.ReadUnicode(20);
			string toname = packet.ReadUnicode(20);
			string msg = packet.ReadUnicode(256);
			if (session.Server != null)
			{
				session.Server.OnChatMessage(name, toname, msg);
			}
		}
		#endregion
	}
}
