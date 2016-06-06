﻿using FlowerMaster.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FlowerMaster.Models
{
    /// <summary>
    /// 游戏信息类
    /// </summary>
    class GameInfo : IGameInfo
    {
        public struct PlayerInfo
        {
            public string name;
            public int lv;
            public int exp;
            public int maxExp;
            public int oldAP;
            public int AP;
            public int maxAP;
            public DateTime apTime;
            public int oldBP;
            public int BP;
            public int maxBP;
            public DateTime bpTime;
            public int oldSP;
            public int SP;
            public int maxSP;
            public int money;
            public int stone;
            public string friendId;
            public DateTime spTime;
        }
        public PlayerInfo player;

        public struct NotifyInfo
        {
            public int lastAP;
            public int lastBP;
            public int lastSP;
        }
        public NotifyInfo notifyRecord;

        /// <summary>
        /// 好友信息结构体
        /// </summary>
        public class FriendInfo
        {
            public string name { get; set; }
            public int lv { get; set; }
            public int totalPower { get; set; }
            public string regTime { get; set; }
            public string lastTime { get; set; }
            public string leader { get; set; }
            public string card1 { get; set; }
            public string card2 { get; set; }
            public string card3 { get; set; }
            public string card4 { get; set; }
            public string card5 { get; set; }
        }
        public ObservableCollection<FriendInfo> friendList = null;

        /// <summary>
        /// 怪物信息结构体
        /// </summary>
        public class BossInfo
        {
            public string name { get; set; }
            public int hp { get; set; }
            public int atk { get; set; }
            public int def { get; set; }
            public string skill { get; set; }
            public int group { get; set; }
            public int money { get; set; }
            public int gp { get; set; }
        }
        public ObservableCollection<BossInfo> bossList = null;

        public class DaliyInfo
        {
            public string day { get; set; }
            public string eventStage { get; set; }
        }

        /// <summary>
        /// 游戏服务器列表枚举
        /// </summary>
        public enum ServersList
        {
            Japan = 0,
            JapanR18 = 1,
            American = 2,
            Taiwan = 3,
        };

        /// <summary>
        /// 用户点数类型枚举
        /// </summary>
        public enum PlayerPointType
        {
            AP = 0,
            BP = 1,
            SP = 2,
        };

        public const int PLAYER_MAX_BP = 6; //玩家最大BP点数
        public const int PLAYER_MAX_BP_A = 3; //玩家最大BP点数（美服）
        public const int PLAYER_MAX_BP_T = 3; //玩家最大BP点数（台服）
        public const int PLAYER_MAX_SP = 3; //玩家最大探索点数

        public const int TIMEOUT_AP = 180; //AP回复时间，3分钟
        public const int TIMEOUT_BP = 1800; //BP回复时间，30分钟
        public const int TIMEOUT_SP = 7200; //探索回复时间，2小时

        //用户点数据操作锁
        private object locker = new object();
        //低等级经验表
        private int[] expLow = { 15, 30, 160, 200, 230, 260, 290, 320, 350, 400, 450, 500, 600, 700, 900 };
        //高等级经验表
        private int[] expHigh = { 300, 500, 800, 1200, 1500, 2500, 3500, 5000, 7500, 10000, 15000, 20000 };

        private Dictionary<int, string> _gameServers;
        private Dictionary<int, string> _gameUrls;
        private int _gameServer;
        private string _gameUrl;
        private bool _isOnline;
        private bool _isAuto;
        private bool _canAuto;
        private DateTime _serverTime;
        private string _lastNewsUrl;
        private ObservableCollection<DaliyInfo> _daliyInfo;
        
        /// <summary>
        /// 初始化 FlowerMaster.Models.GameInfo 类的新实例。
        /// </summary>
        public GameInfo()
        {
            InitGameServers();
            InitGameUrls();
            InitDaliyInfo();
            this._isOnline = false;
            this._isAuto = false;
            this._canAuto = false;
            this._lastNewsUrl = "";
            this._serverTime = DateTime.Now;
        }

        /// <summary>
        /// 初始化服务器游戏首页列表
        /// </summary>
        private void InitGameServers()
        {
            _gameServers = new Dictionary<int, string>();
            _gameServers.Add((int)ServersList.Japan, "http://www.dmm.com/netgame_s/flower/");
            _gameServers.Add((int)ServersList.JapanR18, "http://www.dmm.co.jp/netgame_s/flower-x/");
            _gameServers.Add((int)ServersList.American, "http://www.nutaku.net/games/flower-knight-girl/");
            _gameServers.Add((int)ServersList.Taiwan, "http://www.samurai-games.net/games/flowerknightgirlx/");
        }

        /// <summary>
        /// 初始化服务器游戏页列表
        /// </summary>
        private void InitGameUrls()
        {
            _gameUrls = new Dictionary<int, string>();
            _gameUrls.Add((int)ServersList.Japan, "http://www.dmm.com/netgame/social/-/gadgets/=/app_id=738496/");
            _gameUrls.Add((int)ServersList.JapanR18, "http://www.dmm.co.jp/netgame/social/-/gadgets/=/app_id=329993/");
            _gameUrls.Add((int)ServersList.American, "http://www.nutaku.net/games/flower-knight-girl/play/");
            _gameUrls.Add((int)ServersList.Taiwan, "https://www.samurai-games.net/login/?title_id=flowerknightgirlx&notification=0&token=e7aa0c35c2f81a4b00f0008e2c4df0d5&invite_id=&appParams=");
        }

        /// <summary>
        /// 计算当前用户低于15级的递归经验算法
        /// </summary>
        /// <param name="lv">还需计算的等级</param>
        /// <returns></returns>
        private int _ReCalcPlayerMaxExpLow(int lv)
        {
            if (lv > 1)
            {
                return expLow[lv - 1] + _ReCalcPlayerMaxExpLow(lv - 1);
            }
            else
            {
                return expLow[0];
            }
        }

        /// <summary>
        /// 计算当前用户超过100级的递归经验算法
        /// </summary>
        /// <param name="lv">还需计算的等级</param>
        /// <returns></returns>
        private int _ReCalcPlayerMaxExpHigh(int lv)
        {
            if (lv <= 10)
            {
                return 300 * lv;
            }
            else if (lv % 10 > 0)
            {
                int exp = (lv % 10) * expHigh[lv / 10];
                return exp + _ReCalcPlayerMaxExpHigh(lv - lv % 10);
            }
            else
            {
                int exp = lv > 50 ? 9 * expHigh[lv / 10 - 1] + expHigh[lv / 10] : 10 * expHigh[lv / 10 - 1];
                return exp + _ReCalcPlayerMaxExpHigh(lv - 10);
            }
        }

        /// <summary>
        /// 计算玩家数据值
        /// </summary>
        public void CalcPlayerMaxAPExp()
        {
            if (player.lv > 0)
            {
                if (player.lv <= 99)
                {
                    player.maxAP = 50 + 3 * (player.lv - 1);
                }
                else if (player.lv <= 155)
                {
                    player.maxAP = 344 + player.lv - 99;
                }
                else
                {
                    player.maxAP = 400 + (player.lv - 155) / 2;
                }
                if (player.lv <= 15)
                {
                    player.maxExp = _ReCalcPlayerMaxExpLow(player.lv);
                }
                else if (player.lv <= 100)
                {
                    player.maxExp = player.lv * 100 - 500;
                }
                else
                {
                    player.maxExp = 9500 + _ReCalcPlayerMaxExpHigh(player.lv - 100);
                }
            }
        }

        /// <summary>
        /// 用户点数变更处理
        /// </summary>
        /// <param name="timeType">点数类型</param>
        /// <param name="newVal">新值</param>
        /// <param name="newTime">新时间</param>
        public void CalcPlayerGamePoint(PlayerPointType timeType, JToken newVal, JToken newTime)
        {
            if (newVal == null || newTime == null) return;
            lock (locker)
            {
                switch (timeType)
                {
                    case PlayerPointType.AP:
                        player.oldAP = int.Parse(newVal.ToString());
                        player.apTime = Convert.ToDateTime(newTime.ToString());
                        TimeSpan span = DataUtil.Game.serverTime.Subtract(player.apTime);
                        player.AP = player.oldAP + (int)Math.Round(span.TotalSeconds) / TIMEOUT_AP;
                        if (player.AP > player.maxAP) player.AP = player.maxAP;
                        break;
                    case PlayerPointType.BP:
                        player.oldBP = int.Parse(newVal.ToString());
                        player.bpTime = Convert.ToDateTime(newTime.ToString());
                        span = DataUtil.Game.serverTime.Subtract(player.bpTime);
                        player.BP = player.oldBP + (int)Math.Round(span.TotalSeconds) / TIMEOUT_BP;
                        if (player.BP > player.maxBP) player.BP = player.maxBP;
                        break;
                    case PlayerPointType.SP:
                        player.oldSP = int.Parse(newVal.ToString());
                        player.spTime = Convert.ToDateTime(newTime.ToString());
                        span = DataUtil.Game.serverTime.Subtract(player.spTime);
                        player.SP = player.oldSP + (int)Math.Round(span.TotalSeconds) / TIMEOUT_SP;
                        if (player.SP > player.maxSP) player.SP = player.maxSP;
                        break;
                }
            }
        }

        /// <summary>
        /// 用户点数变更处理
        /// </summary>
        public void CalcPlayerGamePoint()
        {
            lock (locker)
            {
                TimeSpan span = serverTime.Subtract(player.apTime);
                player.AP = player.oldAP + (int)Math.Round(span.TotalSeconds, 0) / 180;
                if (player.AP > player.maxAP) player.AP = player.maxAP;
                span = serverTime.Subtract(player.bpTime);
                player.BP = player.oldBP + (int)Math.Round(span.TotalSeconds, 0) / 1800;
                if (player.BP > player.maxBP) player.BP = player.maxBP;
                span = serverTime.Subtract(player.spTime);
                player.SP = player.oldSP + (int)Math.Round(span.TotalSeconds, 0) / 7200;
                if (player.SP > player.maxSP) player.SP = player.maxSP;
            }
        }

        /// <summary>
        /// 游戏经验增加处理
        /// </summary>
        /// <param name="exp">增加的经验值</param>
        public void IncreasePlayerExp(JToken exp)
        {
            if (exp == null) return;
            player.exp += int.Parse(exp.ToString());
            if (player.exp >= player.maxExp)
            {
                lock (locker)
                {
                    player.lv++;
                    player.exp -= player.maxExp;
                    if (player.lv <= 15)
                    {
                        player.maxExp += expLow[player.lv - 1];
                    }
                    else if (player.lv <= 100)
                    {
                        player.maxExp += 100;
                    }
                    else
                    {
                        player.maxExp += player.lv > 159 ? expHigh[player.lv / 10 - 10] : expHigh[(player.lv - 1) / 10 - 10];
                    }
                    if (player.lv <= 99)
                    {
                        player.maxAP += 3;
                    }
                    else if (player.lv <= 155)
                    {
                        player.maxAP++;
                    }
                    else if (player.lv % 2 == 1)
                    {
                        player.maxAP++;
                    }
                    notifyRecord.lastAP = player.maxAP;
                    notifyRecord.lastBP = player.maxBP;
                    notifyRecord.lastSP = player.maxSP;
                    player.oldAP = player.maxAP;
                    player.oldBP = player.maxBP;
                    player.oldSP = player.maxSP;
                    player.apTime = serverTime;
                    player.bpTime = serverTime;
                    player.spTime = serverTime;
                    player.AP = player.maxAP;
                    player.BP = player.maxBP;
                    player.SP = player.maxSP;
                }
                MiscHelper.AddLog("升级了！你的等级提升到" + player.lv.ToString() + "级", MiscHelper.LogType.Levelup);
            }
        }

        /// <summary>
        /// 初始化日常副本信息列表集合
        /// </summary>
        public void InitDaliyInfo()
        {
            _daliyInfo = new ObservableCollection<DaliyInfo>();
            _daliyInfo.Add(new DaliyInfo() { day = "星期日", eventStage = "斩（红）打（蓝）狗粮本" });
            _daliyInfo.Add(new DaliyInfo() { day = "星期一", eventStage = "斩（红）进化龙本" });
            _daliyInfo.Add(new DaliyInfo() { day = "星期二", eventStage = "打（蓝）进化龙本" });
            _daliyInfo.Add(new DaliyInfo() { day = "星期三", eventStage = "突（黄）魔（紫）狗粮本" });
            _daliyInfo.Add(new DaliyInfo() { day = "星期四", eventStage = "突（黄）进化龙本" });
            _daliyInfo.Add(new DaliyInfo() { day = "星期五", eventStage = "魔（紫）进化龙本" });
            _daliyInfo.Add(new DaliyInfo() { day = "星期六", eventStage = "金币本" });
        }

        /// <summary>
        /// 游戏服务器
        /// </summary>
        public int gameServer
        {
            get
            {
                return this._gameServer;
            }
            set
            {
                this._gameServer = value;
                if (this._gameServers == null) InitGameServers();
                if (this._gameUrls == null) InitGameUrls();
                if (DataUtil.Config.sysConfig.gameHomePage == 0)
                {
                    this._gameUrl = this._gameServers.ContainsKey(this._gameServer) ? this._gameServers[this._gameServer] : "";
                }
                else
                {
                    this._gameUrl = this._gameUrls.ContainsKey(this._gameServer) ? this._gameUrls[this._gameServer] : "";
                }
            }
        }

        /// <summary>
        /// 游戏URL
        /// </summary>
        public string gameUrl
        {
            get
            {
                return this._gameUrl;
            }
        }

        /// <summary>
        /// 是否在线
        /// </summary>
        public bool isOnline
        {
            get
            {
                return this._isOnline;
            }
            set
            {
                this._isOnline = value;
            }
        }

        /// <summary>
        /// 是否在自动推图
        /// </summary>
        public bool isAuto
        {
            get
            {
                return this._isAuto;
            }
            set
            {
                this._isAuto = value;
            }
        }

        /// <summary>
        /// 是否可以自动推图
        /// </summary>
        public bool canAuto
        {
            get
            {
                return this._canAuto;
            }
            set
            {
                this._canAuto = value;
            }
        }

        /// <summary>
        /// 服务器时间
        /// </summary>
        public DateTime serverTime
        {
            get
            {
                return this._serverTime;
            }
            set
            {
                this._serverTime = value;
            }
        }

        /// <summary>
        /// 最新游戏公告URL
        /// </summary>
        public string lastNewsUrl
        {
            get
            {
                return this._lastNewsUrl;
            }
            set
            {
                this._lastNewsUrl = value;
            }
        }

        /// <summary>
        /// 日常副本信息集合
        /// </summary>
        public ObservableCollection<DaliyInfo> daliyInfo
        {
            get
            {
                return this._daliyInfo;
            }
        }
    }
}