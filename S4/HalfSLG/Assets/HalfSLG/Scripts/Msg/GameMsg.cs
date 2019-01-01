using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ELGame
{
    public enum MsgBattleHeroActionType
    {
        None,
        Warning,            //警告(调试用)
        EnterBattleField,   //进入战场
        ChangeTarget,       //切换目标
        MotionAction,       //移动
        SkillAction,        //使用技能
        Defeated,           //被击败
        Manual,             //等待手动操作
    }

    public class BattleAction
    {
        protected BattleAction(MsgBattleHeroActionType actionType)
        {
            this.actionType = actionType;
        }

        public MsgBattleHeroActionType actionType;
    }

    public class BattleHeroAction
        : BattleAction
    {
        protected BattleHeroAction(BattleUnit actionUnit, MsgBattleHeroActionType actionType) : base(actionType)
        {
            this.actionUnit = actionUnit;
        }

        public BattleUnit actionUnit;
    }

    //移动
    public class BattleHeroMotionAction
        : BattleHeroAction
    {
        public BattleHeroMotionAction(BattleUnit actionUnit):base(actionUnit, MsgBattleHeroActionType.MotionAction) { }

        public BattleUnit targetUnit;
        public GridUnit fromGrid;
        public GridUnit toGrid;
        public GridUnit[] gridPath;
        public int moveRange;

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} move from {1} to {2}\n", actionUnit, fromGrid, gridPath[0]);
            for (int i = 0; i < gridPath.Length - 1; ++i)
            {
                builder.AppendFormat("{0} move from {1} to {2}\n", actionUnit, gridPath[i], gridPath[i + 1]);
            }
            return builder.ToString();
        }
    }

    //使用技能
    public class BattleHeroSkillAction
        : BattleHeroAction
    {
        public BattleHeroSkillAction(BattleUnit actionUnit, int skillID) : base(actionUnit, MsgBattleHeroActionType.SkillAction)
        {
            this.skillID = skillID;
        }

        public int skillID;
        public BattleHeroSkillResult[] skillResult;

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0} use skill {1}\n", actionUnit, skillID);
            if (skillResult != null)
            {
                for (int i = 0; i < skillResult.Length; ++i)
                {
                    builder.AppendFormat("{0}\n", skillResult[i].ToString());
                }
            }
            return builder.ToString();
        }
    }

    //进入战场
    public class BattleHeroEnterBattleFieldAction
        : BattleHeroAction
    {
        public BattleHeroEnterBattleFieldAction(BattleUnit actionUnit) : base(actionUnit, MsgBattleHeroActionType.EnterBattleField){}

        public GridUnit gridUnit;
        public BattleHeroSyncAttribute attribute;

        public override string ToString()
        {
            return string.Format("{0} enter battle field -> {1}", actionUnit, gridUnit == null ? "None" : gridUnit.ToString());
        }
    }

    //切换目标
    public class BattleHeroChangeTargetAction
        : BattleHeroAction
    {
        public BattleHeroChangeTargetAction(BattleUnit actionUnit) : base(actionUnit, MsgBattleHeroActionType.ChangeTarget) { }

        public BattleUnit lastTargetUnit;
        public BattleUnit newTargetUnit;

        public override string ToString()
        {
            return string.Format("{0} change target from {1} to {2}",
                actionUnit,
                lastTargetUnit == null ? "None" : lastTargetUnit.ToString(),
                newTargetUnit == null ? "None" : newTargetUnit.ToString());
        }
    }

    //被击败，无法继续战斗
    public class BattleHerodDefeatedAction
        : BattleHeroAction
    {
        public BattleHerodDefeatedAction(BattleUnit actionUnit) : base(actionUnit, MsgBattleHeroActionType.Defeated) { }

        public override string ToString()
        {
            return string.Format("<color=#ff0000>{0} has been defeated.</color>", actionUnit);
        }
    }

    //警告动作，用于调试
    public class BattleHeroWarningAction
        : BattleHeroAction
    {
        public BattleHeroWarningAction(BattleUnit actionUnit, string logWarning) : base(actionUnit, MsgBattleHeroActionType.Warning)
        {
            this.logWarning = logWarning;
        }

        string logWarning;

        public override string ToString()
        {
            return string.Format("<color=#FF00FF>WARNING:{0}</color>", actionUnit);
        }
    }

    //等待玩家手动操作
    public class BattleHeroManualAction
        : BattleHeroAction
    {
        public BattleHeroManualAction(BattleUnit actionUnit) : base(actionUnit, MsgBattleHeroActionType.Manual) { }
        
        public override string ToString()
        {
            return string.Format("<color=#FF00FF>Manual action:{0}</color>", actionUnit);
        }
    }

    //同步属性
    public class BattleHeroSyncAttribute
    {
        public int hpChanged;   //生命值变化量
        public int currentHP;   //变化后的生命值
    }

    //技能造成的结果
    public class BattleHeroSkillResult
    {
        public BattleUnit battleUnit;
        public BattleHeroSyncAttribute syncAttribute;

        public override string ToString()
        {
            if (syncAttribute.hpChanged > 0)
                return string.Format("{0} hp changed -> +{1}", battleUnit, syncAttribute.hpChanged);
            else
                return string.Format("{0} hp changed -> {1}", battleUnit, syncAttribute.hpChanged);
        }
    }
    
    public class MsgAction
    {
        public List<BattleAction> battleActions;
    }
}