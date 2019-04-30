using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ELGame
{
    public enum RegEventResult
    {
        Success,
        Failed,
    }

    public delegate void GameEventHandler(ELGame.IGameEvent msg);

    public class GameEventHandlerItem
    {
        class EventInfo
        {
            public GameEventHandler handler;
            public string listener;
            public int time;

            public EventInfo(string listener, GameEventHandler handler, int time)
            {
                this.listener = listener;
                this.handler = handler;
                this.time = time;
            }
        }

        //调用的key
        private string eventKey;
        //记录回调函数和调用次数
        private List<EventInfo> eventInfos = new List<EventInfo>();
        //保存回调函数的事件
        private event GameEventHandler gameEvent;

        //new就等于新增
        public GameEventHandlerItem(string key, string listener, GameEventHandler _handler, int _time)
        {
            eventKey = key;
            eventInfos.Add(new EventInfo(listener, _handler, _time));
            gameEvent += _handler;
        }

        //注册
        public RegEventResult AddEventHandler(string listener, GameEventHandler handler, int time)
        {
            
#if UNITY_EDITOR
            //编辑器模式需要做一个重复检查
            for (int i = 0; i < eventInfos.Count; i++)
            {
                if (eventInfos[i].handler == handler
                    && string.Compare(eventInfos[i].listener ,listener, true) == 0)
                {
                    eventInfos[i].time = time;
                    UtilityHelper.LogWarning(string.Format("Add event --> {0} <-- handler repeatedly !", eventKey));
                    return RegEventResult.Failed;
                }
            }
#endif

            eventInfos.Add(new EventInfo(listener, handler, time));
            gameEvent += handler;
            return RegEventResult.Success;
        }
        
        //广播某个事件
        public void Run(IGameEvent msg)
        {
            gameEvent(msg);

            for (int i = eventInfos.Count - 1; i >= 0; --i)
            {
                if (eventInfos[i].time == EGameConstL.Infinity)
                    continue;

                eventInfos[i].time -= 1;

                if (eventInfos[i].time <= 0)
                {
                    //移除
                    gameEvent -= eventInfos[i].handler;
                    eventInfos.RemoveAt(i);
#if UNITY_EDITOR
                    EventManager.Instance.MgrLog(string.Format("Time is zero, remove handler. KEY = {0}", eventKey));
#endif
                }
            }
        }

        //根据对象清空
        public void Remove(string listener)
        {
            for (int i = eventInfos.Count - 1; i >= 0; --i)
            {
                if (string.Compare(eventInfos[i].listener, listener, true) == 0)
                {
                    //移除
                    gameEvent -= eventInfos[i].handler;
                    eventInfos.RemoveAt(i);
                }
            }
        }

        //获取当前注册的监听数量
        public int ListenerCount
        {
            get
            {
                return eventInfos.Count;
            }
        }

        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < eventInfos.Count; i++)
            {
                strBuilder.AppendFormat("{0}, Name:{1}, Time:{2}", i, eventInfos[i].listener, eventInfos[i].time);
            }
            return strBuilder.ToString();
        }
    }
        
	public class EventManager 
        : BaseManager<EventManager>
    {
        public override string MgrName => "EventManager";

        private Dictionary<string, GameEventHandlerItem> eventsDic = new Dictionary<string, GameEventHandlerItem>();

        //初始化
        public override void InitManager()
        {
            base.InitManager();
            Reset();
        }

        //注册
        public RegEventResult Register (
            string key,
            string listener,
            ELGame.GameEventHandler handler, 
            int times = EGameConstL.Infinity, 
            Dictionary<string, GameEventHandlerItem> dic = null)
		{
            Dictionary<string, GameEventHandlerItem> refDic = null;

            if (dic == null)
                refDic = eventsDic;
            else
                refDic = dic;

            //如果不传dic则默认是全局广播
            GameEventHandlerItem delegateItem = null;
            if (refDic.TryGetValue(key.ToUpper(), out delegateItem))
            {   
                return delegateItem.AddEventHandler(listener, handler, times);
            }
            else
            {
                //没有
                refDic.Add(key.ToUpper(), new GameEventHandlerItem(key, listener, handler, times));

                if (dic == null)
                    MgrLog(string.Format("注册世界事件:{0}, 相应次数:{1}", key, times));
                else
                    MgrLog(string.Format("注册本地事件:{0}, 相应次数:{1}", key, times));

                return RegEventResult.Success;  
            }
		}

        //根据key清空
        public void UnregisterByKey(
            string key,
            Dictionary<string, GameEventHandlerItem> dic = null)
        {
            Dictionary<string, GameEventHandlerItem> refDic = null;

            if (dic == null)
                refDic = eventsDic;
            else
                refDic = dic;

            refDic.Remove(key.ToUpper());
        }
            
        //根据注册对象移除
        public void Unregister(
            string listener,
            Dictionary<string, GameEventHandlerItem> dic = null)
        {
            Dictionary<string, GameEventHandlerItem> refDic = null;

            if (dic == null)
                refDic = eventsDic;
            else
                refDic = dic;

            List<string> removeList = new List<string>();
            foreach(var eventListItem in refDic)
            {
                eventListItem.Value.Remove(listener);
                if (eventListItem.Value.ListenerCount == 0)
                {
                    removeList.Add(eventListItem.Key);
                }
            }

            //删除
            foreach(var item in removeList)
            {
                refDic.Remove(item);
            }
        }

        //调用
        public void Run(string key, IGameEvent msg, Dictionary<string, GameEventHandlerItem> dic = null)
        {
            Dictionary<string, GameEventHandlerItem> refDic = null;

            if (dic == null)
                refDic = eventsDic;
            else
                refDic = dic;

            GameEventHandlerItem delegateItem = null;
            if(refDic.TryGetValue(key.ToUpper(), out delegateItem))
            {
                delegateItem.Run(msg);
                if (delegateItem.ListenerCount == 0)
                {
                    refDic.Remove(key.ToUpper());
                }
            }
            else
            {
                MgrLog("NO REGISTER KEY:" + key);
            }
        }

        //重置
		public void Reset(Dictionary<string, GameEventHandlerItem> dic = null)
        {
            Dictionary<string, GameEventHandlerItem> refDic = null;

            if (dic == null)
                refDic = eventsDic;
            else
                refDic = dic;

            refDic.Clear();
            MgrLog("EVENT MANAGER REST COMPLETE");
		}

        public void Desc(Dictionary<string, GameEventHandlerItem> dic = null)
        {
            Dictionary<string, GameEventHandlerItem> refDic = null;

            if (dic == null)
                refDic = eventsDic;
            else
                refDic = dic;

            string name = "(Global)";
            if (dic != null)
                name = "(Local)";

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendFormat("**********************************\n");
            stringBuilder.AppendFormat("Show Registed event: {0}\n" , name);
            stringBuilder.AppendFormat("**********************************\n");

            foreach (var eventList in refDic)
            {
                stringBuilder.AppendFormat("Key={0},Count={1}\n", eventList.Key, eventList.Value.ListenerCount);
                stringBuilder.AppendFormat(eventList.Value.ToString());
                stringBuilder.AppendFormat("------------------------------\n");
            }
            stringBuilder.AppendFormat("**********************************\n");

            MgrLog(stringBuilder.ToString());
        }
	}

}