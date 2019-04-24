using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ELGame
{
    public enum BattleFieldEventType
    {
        None,
        Warning,            //警告(调试用)
        BattleStart,        //战斗开始
        BattleUnitAction,   //英雄行动
        GridUnit,           //格子事件
        BattleEnd,          //战斗结束
    }

    //战斗开始消息
    public class BattleStartAction
        : BattleFieldEvent
    {
        private BattleStartAction() : base(BattleFieldEventType.BattleStart)
        {
        }
    }

    //战斗结束消息
    public class BattleEndAction
        : BattleFieldEvent
    {
        private BattleEndAction() : base(BattleFieldEventType.BattleEnd)
        {
        }
    }

    //战场事件
    public class BattleFieldEvent
    {
        protected static int sn = 0;

        public int SN { get => sn; }

        protected BattleFieldEvent(BattleFieldEventType actionType)
        {
            sn++;
            this.actionType = actionType;
        }

        public BattleFieldEventType actionType;
    }
}