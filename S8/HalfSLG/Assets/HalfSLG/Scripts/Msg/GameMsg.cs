using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ELGame
{
    public enum MsgBattleActionType
    {
        None,
        Warning,            //警告(调试用)
        BattleUnitAction,   //英雄行动
        BattleStart,        //战斗开始
        End,                //战斗结束
    }

    public class BattleAction
    {
        protected BattleAction(MsgBattleActionType actionType)
        {
            this.actionType = actionType;
        }

        public MsgBattleActionType actionType;
    }

    //战斗结束消息
    public class BattleEndAction
        : BattleAction
    {
        public BattleEndAction() : base(MsgBattleActionType.End)
        {
        }
    }

    
    public class MsgAction
    {
        public List<BattleAction> battleActions;
    }
}