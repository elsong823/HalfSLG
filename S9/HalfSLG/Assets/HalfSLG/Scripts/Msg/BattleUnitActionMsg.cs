//战场中战斗单位的行动消息

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ELGame
{
    //战斗单位的行动消息
    public class BattleUnitAction
        : BattleAction
    {
        protected BattleUnitAction(BattleUnit actionUnit) : base(MsgBattleActionType.BattleUnitAction)
        {
            this.actionUnit = actionUnit;
        }
        
        public BattleUnit actionUnit;                                   //行动单位
        public BattleUnitEnterBattleFieldAction enterBattleFieldAction; //进入战场消息
        public BattleUnitChangeTargetAction changeTargetAction;         //切换目标消息
        public BattleUnitMotionAction motionAction;                     //移动行动
        public BattleUnitSkillAction skillAction;                       //技能行动
        public BattleUnitAttributeUpdate attributeUpdate;               //属性直接变化
        public BattleUnitManualAction manualAction;                     //等待玩家手动操作的消息
        public BattleUnitWarningAction warningAction;                   //警告信息

        public static BattleUnitAction Create(BattleUnit actionUnit)
        {
            return new BattleUnitAction(actionUnit);
        }

        public string Desc()
        {
            StringBuilder desc = new StringBuilder();
            desc.AppendFormat("BattleUnit action:{0}\n", actionUnit.ToString());
            if (enterBattleFieldAction != null)
            {
                desc.AppendFormat("Enter battle field:{0}, born grid:{1}\n", enterBattleFieldAction.battleField, enterBattleFieldAction.bornGrid);
            }
            if (changeTargetAction != null)
            {
                desc.AppendFormat("Change target:from {0} to {1}\n", changeTargetAction.lastTargetUnit == null ? "None" : changeTargetAction.lastTargetUnit.ToString(), changeTargetAction.newTargetUnit == null ? "None" : changeTargetAction.newTargetUnit.ToString());
            }
            if (motionAction != null)
            {
                desc.AppendFormat("Move: target is {0}, move from {1},\n", motionAction.targetUnit, motionAction.fromGrid);
                if (motionAction.gridPath != null)
                {
                    for (int i = 0; i < motionAction.gridPath.Length; ++i)
                    {
                        desc.AppendFormat("Passed: {0},\n", motionAction.gridPath[i]);
                    }
                }
                desc.AppendFormat("Move range: {0}.\n", motionAction.moveRange);
            }
            if (skillAction != null)
            {
                desc.AppendFormat("Use skill {0}\n", skillAction.battleSkill.skillName);
                if (skillAction.skillResult != null)
                {
                    for (int i = 0; i < skillAction.skillResult.Length; ++i)
                    {
                        desc.AppendFormat("{0}\n", skillAction.skillResult[i].ToString());
                    }
                }
            }
            if (manualAction != null)
            {
                desc.AppendFormat("Waiting for manual operation.\n");
            }
            if (warningAction != null)
            {
                desc.AppendFormat("<color=#FFFF00>WARNING!!!</color>:{0}\n", warningAction.logWarning);
            }

            return desc.ToString();
        }
    }

    //进入战场
    public class BattleUnitEnterBattleFieldAction
    {
        public BattleField battleField;             //进入的战场
        public GridUnit bornGrid;                   //出生格子
        public BattleUnitSyncAttribute attribute;   //进入后同步的属性

        private BattleUnitEnterBattleFieldAction() { }

        public static BattleUnitEnterBattleFieldAction Get()
        {
            return new BattleUnitEnterBattleFieldAction();
        }
    }

    //切换目标
    public class BattleUnitChangeTargetAction
    {
        public BattleUnit lastTargetUnit;
        public BattleUnit newTargetUnit;

        private BattleUnitChangeTargetAction() { }

        public static BattleUnitChangeTargetAction Get()
        {
            return new BattleUnitChangeTargetAction();
        }
    }

    //移动
    public class BattleUnitMotionAction
    {
        public BattleUnit targetUnit;   //奔着谁去的 
        public GridUnit fromGrid;       //从哪个格子开始出发
        public GridUnit[] gridPath;     //移动路径
        public int moveRange;           //可移动半径

        private BattleUnitMotionAction() { }

        public static BattleUnitMotionAction Get()
        {
            return new BattleUnitMotionAction();
        }
    }

    //使用技能
    public class BattleUnitSkillAction
    {
        public SO_BattleSkill battleSkill;                       //所使用的技能
        public GridUnit targetGrid;                              //目标地图格子
        public BattleUnit targetBattleUnit;                      //目标战斗单位
        public BattleUnitSyncAttribute selfAttribute;    //自身属性变化(消耗能量)
        public BattleUnitSkillResult[] skillResult;              //技能造成的影响

        private BattleUnitSkillAction() { }

        public static BattleUnitSkillAction Get()
        {
            return new BattleUnitSkillAction();
        }
    }

    //属性更新
    public class BattleUnitAttributeUpdate
    {
        public BattleUnitSyncAttribute attribute;

        private BattleUnitAttributeUpdate() { }

        public static BattleUnitAttributeUpdate Get() { return new BattleUnitAttributeUpdate(); }
    }
     
    //等待玩家手动操作
    public class BattleUnitManualAction { }

    //警告动作，用于调试
    public class BattleUnitWarningAction
    {
        public string logWarning;

        private BattleUnitWarningAction() { }

        public static BattleUnitWarningAction Get()
        {
            return new BattleUnitWarningAction();
        }
    }
    
    //同步属性
    public class BattleUnitSyncAttribute
    {
        public int hpChanged;       //生命值变化量
        public int currentHP;       //变化后的生命值
        public int energyChanged;   //能量变化
        public int currentEnergy;   //当前能量
    }

    //技能造成的结果
    public class BattleUnitSkillResult
    {
        public BattleUnit battleUnit;                   //被影响的单位
        public SO_BattleSkill battleSkill;              //所使用的的技能
        public BattleUnitSyncAttribute syncAttribute;   //属性同步

        public override string ToString()
        {
            if (syncAttribute.hpChanged > 0)
                return string.Format("{0} hp changed -> +{1}", battleUnit, syncAttribute.hpChanged);
            else
                return string.Format("{0} hp changed -> {1}", battleUnit, syncAttribute.hpChanged);
        }
    }
}