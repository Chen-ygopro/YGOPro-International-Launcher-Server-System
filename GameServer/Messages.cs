/*
 * 由SharpDevelop创建。
 * 用户： Hasee
 * 日期: 2015/9/7
 * 时间: 22:16
 * 
 * 要改变这种模板请点击 工具|选项|代码编写|编辑标准头文件
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace YGOCore
{
	/// <summary>
	/// Description of Messages.
	/// </summary>
	public class Messages
	{
        public const string MSG_PLAYER_INGAME = "players already in the game";
        public const string MSG_CLOSE = "server will shut down in 3 minutes";
        public const string MSG_HIGH_VERSION = "Your game version is higher than the servers";
        public const string MSG_FULL = "full room";
        public const string MSG_GAMEOVER = "Game Over";
        public const string MSG_SEND_FAIL = "Send failed";
        public const string MSG_NO_AI = "You can not add AI";
        public const string MSG_NO_FREE_AI = "no idle AI";
        public const string MSG_ADD_AI = "Add AI success";
        public const string MSG_BAN_PCHAT = "whisper has banned";
        public const string MSG_NOCHECKDECK = "This room does not check the deck,";
        public const string MSG_NOSHUFFLEDECK = "This room does not shuffle the deck";
        public const string MSG_ENABLE_PROIORITY = "The room with the old rules ";
        public const string ERR_NAME_PASSWORD = "user name or password is incorrect";
        public const string ERR_NAME_PASSWORD_LONG = "name and password is too long, please change your password.";
        public const string MSG_SIDE = "Please change your side within 120 seconds";
        public const string MSG_DISCONECT = "{0} players dropped, wait {1} seconds, the timeout is counting duel ends";
        public const string MSG_TIP_TIME = "You have {0} seconds Timeout";
        public const string MSG_READY = "{0} ready";
        public const string MSG_WATCH_SIDE = "side deck";
        public const string MSG_PLAYER_BAN = "Your account can not visit the server";
        public const string ERR_NO_NAME = "name can not be empty";
        public const string ERR_NO_PASS = "password can not be empty";
        public const string ERR_AUTH_FAIL = "Login failed";
        public const string ERR_IS_LOGIN = "already logged in";
        public const string ERR_LOW_VERSION = "version of the game is too low";
        public const string ERR_PASSWORD = "room password";
        public const string ERR_NO_CLIENT = "User:is not online";

        public static void Init(string file){
		//	Logger.Debug("msg file="+file);
			if(File.Exists(file)){
				Msgs.Clear();
				string[] msgs = File.ReadAllLines(file);
				foreach(string msg in msgs){
					if(string.IsNullOrEmpty(msg) || msg.StartsWith("#")){
						continue;
					}
					Msgs.Add(msg);
				}
			}
		}
		static readonly List<string> Msgs=new List<string>();
		public static string RandomMessage(){
			if(Msgs.Count==0){
				return null;
			}
			int i = Program.Random.Next(Msgs.Count);
			return Msgs[i];
		}
	}
}
