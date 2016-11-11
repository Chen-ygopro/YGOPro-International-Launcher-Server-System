using System;
using System.IO;
using System.Diagnostics;
using System.Xml;
using AsyncServer;

namespace YGOCore
{
	class Program
	{
		/// <summary>
		/// Is responsible for the room and online lists.
		/// <para>Client login validation, return to the chat server (currently the end is responsible for) and (at least) against the server address, full room list provided by the end of</para>
		/// <para>Request the room list, returns the waiting room
		/// <para>As the room changes, push</para></para>
		/// <para>When the client enters the game, send a pause message, push the pause, quit the game, requesting a room list</para>
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