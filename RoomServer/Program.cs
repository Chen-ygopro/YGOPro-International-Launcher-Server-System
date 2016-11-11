﻿using System;
using System.IO;
using System.Diagnostics;
using System.Xml;
using AsyncServer;

namespace YGOCore
{
	class Program
	{
		/// <summary>
		/// 负责房间列表和在线列表。
		/// <para>客户端登录，验证后，返回聊天服务端(目前本端负责)和（人数最少的）对战服务端的地址，全部的房间列表由本端提供</para>
		/// <para>请求房间列表，返回等待的房间
		/// <para>当房间变化，推送</para></para>
		/// <para>当客户端进入游戏，发送暂停消息，暂停推送，退出游戏，则请求房间列表</para>
		/// </summary>
		/// <param name="args">port GameServer.exe config.txt config1.txt config2.txt ...</param>
		public static void Main(string[] args)
		{
            RoomServer Server = new RoomServer();
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                Server.Stop();
                File.WriteAllText("crash_room_" + DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt", e.ExceptionObject.ToString());
                Process.GetCurrentProcess().Kill();
            };
			
			Console.Title = "RoomServer";
			Logger.SetLogLevel(LogLevel.Info);
#if DEBUG
			Logger.SetLogLevel(LogLevel.Debug);
#endif
            //close event
            Console.CancelKeyPress += delegate
            {
                Server.Stop();
            };
            Kernel32.SetConsoleCtrlHandler(new ControlCtrlDelegate((int type)=>{
                Server.Stop();
                return false;
            }), true);


            if (Server.Start()){
				Command(Server);
				Console.WriteLine("server is close");
			}else{
				Console.WriteLine("start fail.");
			}
			Console.ReadKey(true);
		}

		private static void Command(RoomServer server){
			string cmd="";
			while(server.IsListening){
				cmd = Console.ReadLine();
				server.OnCommand(cmd);
			}
		}
	}
}