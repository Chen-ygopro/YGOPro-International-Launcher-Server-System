﻿using System;
using AsyncServer;
using System.Collections.Generic;
using YGOCore.Net;
using YGOCore.Game;
using OcgWrapper.Enums;

namespace YGOCore.Net
{
    /// <summary>
    /// Description of GameSession.
    /// </summary>
    public class GameSession
    {
        public GameSession(Connection<GameSession> client, int version, int timeout)
        {
            this.m_client = client;
            //异步发送
            this.Type = (int)PlayerType.Undefined;
            this.State = PlayerState.None;
            this.ClientVersion = version;
            if (timeout <= 0)
            {
                timeout = 15;
            }
            MyTimer CheckTimer = new MyTimer(1000, timeout);
            CheckTimer.AutoReset = true;
            CheckTimer.Elapsed += delegate
            {
                if (!string.IsNullOrEmpty(Name))
                {
                    CheckTimer.Stop();
                    CheckTimer.Close();
                }
                if (CheckTimer.CheckStop())
                {
                    //超时自动断开
                    CloseAsync();
                    CheckTimer.Close();
                }
            };
        }

        #region Member
        private bool m_close;
        private Connection<GameSession> m_client;
        private string namepassword;
        /// <summary>
        /// 房间
        /// </summary>
        public GameRoom Game;
        /// <summary>
        /// 名字
        /// </summary>
        public string Name
        {
            get
            {
                return Password.OnlyName(namepassword);
            }
            set
            {
                if (value != null)
                {
                    namepassword = value.Trim();
                }
            }
        }
        /// <summary>
        /// 是否认证
        /// </summary>
        public bool IsAuthentified { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// 跳过的回合数
        /// </summary>
        public int TurnSkip { get; set; }
        /// <summary>
        /// 卡组
        /// </summary>
        public Deck Deck { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public PlayerState State { get; set; }

        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected
        {
            get { return m_client != null && m_client.Connected; }
        }
        /// <summary>
        /// socket
        /// </summary>
        public Connection<GameSession> Client
        {
            get { return m_client; }
        }
        public int ClientVersion { get; private set; }
        #endregion

        #region packet

        public bool OnCheck()
        {
            byte[] data;
            if(m_client.GetPacketData(GameServerPacket.GamePacketByteLength, out data))
            {
                GameClientPacket packet = new GameClientPacket(data);
                if (GameEvent.Handler(this, packet) == CtosMessage.PlayerInfo)
                {
                    return true;
                }
            }
            Logger.Debug("first msg isn't PlayerInfo");
            CloseAsync();
            return false;
        }
        public void OnReceive(object obj)
        {
            if (m_close) return;
            //线程处理
            bool next = true;
            while (next)
            {
                byte[] data;
                next = m_client.GetPacketData(GameServerPacket.GamePacketByteLength, out data);
                if (data!=null&& data.Length>0)
                {
                    //处理游戏事件
                    GameClientPacket packet = new GameClientPacket(data);
                    GameEvent.Handler(this, packet);
                }
            }
        }
        public void Send(byte[] data, bool isNow = true)
        {
            if (m_close) return;
            m_client.SendPackage(data, isNow);
        }
        public void Send(GameServerPacket packet, bool isNow = true)
        {
         //   Logger.Debug("send "+packet.PacketMsg);
            Send(packet.Content, isNow);
        }

        public void PeekSend()
        {
            try
            {
                m_client.PeekSend();
            }
            catch { }
        }
        public void Close()
        {
            if (m_close) return;
            m_close = true;
            if (Game != null)
            {

                Game.RemovePlayer(this);
            }
            m_client.Close();
        }
        public void CloseAsync()
        {
            if (m_close) return;
            m_close = true;
            if (Game != null)
            {
                lock (Game.AsyncRoot)
                    Game.RemovePlayer(this);
            }
            m_client.Close();
        }
        #endregion
    }
}
