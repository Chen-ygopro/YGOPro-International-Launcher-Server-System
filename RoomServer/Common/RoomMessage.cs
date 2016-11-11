/*
 * 由SharpDevelop创建。
 * 用户： Administrator
 * 日期: 2015/11/12
 * 时间: 17:36
 * 
 * 要改变这种模板请点击 工具|选项|代码编写|编辑标准头文件
 */
using System;

namespace YGOCore
{
	/// <summary>
	/// Description of RoomMessage.
	/// </summary>
	public enum RoomMessage : byte
	{
		Error,
		/// <summary>
		/// Request the room list
		/// </summary>
		RoomList,
		/// <summary>
		/// Log in/Server information
		/// </summary>
		Info,
		/// <summary>
		/// Chat
		/// </summary>
		Chat,
		/// <summary>
		/// Push pause
		/// </summary>
		Pause,
		RoomCreate,
		RoomStart,
		RoomClose,
		PlayerEnter,
		PlayerLeave,
		ServerClose,
		PlayerList,
        ServerStop,
        PlayerInfo = 0x10,
        CreateGame = 0x11,
        JoinGame = 0x12,
        TypeChange = 0x13,
        OnChat = 0x19,
        OnGameChat = 0x16,
        HsPlayerEnter = 0x20,
        HsPlayerChange = 0x21,
        STOP_CLIENT = 0xdf,
        NETWORK_CLIENT_ID = 0xdd,
        VersionCheck = 0xdc,
        Unknown = 0xff
    }
}
