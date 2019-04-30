//战场中战斗单位的行动消息

using System.Collections.Generic;
using System.Text;

namespace ELGame
{
    public enum BattleUnitActionType
    {
        EnterBattleField,   //进入战场
        ChangeTarget,       //切换目标
        MoveToTarget,       //向某个位置移动
        UseSkill,           //使用技能
        AttributeUpdate,    //属性直接变化
        ManualOperate,      //手动操作
        PickupItem,         //拾取道具
        UseItem,            //使用道具
        Warning,            //警告（调试用）
    }

    //战斗单位的行动消息
    public class BattleUnitActionEvent
        : BattleFieldEvent
    {
        protected BattleUnitActionEvent() : base(BattleFieldEventType.BattleUnitAction) { }

        public BattleUnitActionType battleUnitActionType;
        public BattleUnit actionUnit;
        
        //创建事件
        public static T CreateEvent<T>(BattleUnitActionType battleUnitActionType, BattleUnit battleUnit)
            where T : BattleUnitActionEvent, new()
        {
            T t = new T();
            t.battleUnitActionType = battleUnitActionType;
            t.actionUnit = battleUnit;
            return t;
        }

        public virtual string Desc()
        {
            return battleUnitActionType.ToString();
        }

        public override string ToString()
        {
            return Desc();
        }
    }
    
    //进入战场
    public class BattleUnitEnterBattleFieldAction
        : BattleUnitActionEvent
    {
        public BattleField battleField;             //进入的战场
        public GridUnit bornGrid;                   //出生格子
        public BattleUnitSyncAttribute attribute;   //进入后同步的属性
        
        public override string Desc()
        {
            return string.Format("Enter battle field:{0}, born grid:{1}\n", battleField, bornGrid);
        }
    }

    //切换目标
    public class BattleUnitChangeTargetAction
        : BattleUnitActionEvent
    {
        public BattleUnit lastTargetUnit;
        public BattleUnit newTargetUnit;
        
        public override string Desc()
        {
            return string.Format("Change target:from {0} to {1}\n", lastTargetUnit == null ? "None" : lastTargetUnit.ToString(), newTargetUnit == null ? "None" : newTargetUnit.ToString());
        }
    }

    //移动
    public class BattleUnitMotionAction
        : BattleUnitActionEvent
    {
        public BattleUnit targetUnit;   //奔着谁去的 
        public GridUnit fromGrid;       //从哪个格子开始出发
        public GridUnit[] gridPath;     //移动路径
        public int moveRange;           //可移动半径
        
        public override string Desc()
        {
            StringBuilder desc = new StringBuilder();
            desc.AppendFormat("Move: target is {0}, move from {1},\n", targetUnit, fromGrid);
            if (gridPath != null)
            {
                for (int i = 0; i < gridPath.Length; ++i)
                {
                    desc.AppendFormat("Passed: {0},\n", gridPath[i]);
                }
            }
            desc.AppendFormat("Move range: {0}.\n", moveRange);
            return desc.ToString();
        }
    }

    //拾取了道具
    public class BattleUnitPickupItemAction
        : BattleUnitActionEvent
    {
        public int itemID;
        public int addCount;       //道具数量
        public int finalCount;  //最终数量
        
        public override string Desc()
        {
            return string.Format("{0} pickup {1} {2}, final count {3}.", 
                actionUnit.battleUnitAttribute.battleUnitName, 
                addCount,
                itemID, 
                finalCount
                );
        }
    }

    //使用了道具
    public class BattleUnitUseItemAction
        :BattleUnitActionEvent
    {
        public int itemID;
        public int useCount;     //使用数量
        public int remainCount;  //剩余数量

        public BattleUnitSyncAttribute attributeUpdate;   //属性变化
        
        public override string Desc()
        {
            return string.Format("{0} use {2} {3}, final count {4}.",
                actionUnit.battleUnitAttribute.battleUnitName,
                useCount,
                itemID,
                remainCount
                );
        }
    }

    //使用技能
    public class BattleUnitSkillAction
        : BattleUnitActionEvent
    {
        public SO_BattleSkill battleSkill;                       //所使用的技能
        public GridUnit targetGrid;                              //目标地图格子
        public BattleUnit targetBattleUnit;                      //目标战斗单位
        public BattleUnitSyncAttribute selfAttribute;    //自身属性变化(消耗能量)
        public BattleUnitSkillResult[] skillResult;              //技能造成的影响
        
        public override string Desc()
        {
            StringBuilder desc = new StringBuilder();
            desc.AppendFormat("Use skill {0}\n", battleSkill.skillName);
            if (skillResult != null)
            {
                for (int i = 0; i < skillResult.Length; ++i)
                {
                    desc.AppendFormat("{0}\n", skillResult[i].ToString());
                }
            }
            return desc.ToString();
        }
    }

    //属性更新
    public class BattleUnitAttributeUpdate
        : BattleUnitActionEvent
    {
        public BattleUnitSyncAttribute attribute;
        
        public override string Desc()
        {
            return string.Format("Battle Unit Attribut eUpdate: {0}", attribute);
        }
    }

    //等待玩家手动操作
    public class BattleUnitManualAction
        : BattleUnitActionEvent
    {
    }

    //警告动作，用于调试
    public class BattleUnitWarningAction
        : BattleUnitActionEvent
    {
        public string warningLog;
        
        public override string Desc()
        {
            return warningLog;
        }
    }

    //同步属性
    public class BattleUnitSyncAttribute
    {
        public int hpChanged;       //生命值变化量
        public int currentHP;       //变化后的生命值
        public int energyChanged;   //能量变化
        public int currentEnergy;   //当前能量

        public override string ToString()
        {
            return string.Format("\n\tHP:{0}({1}),\n\tEnergy:{2}({3})", hpChanged, currentHP, energyChanged, currentEnergy);
        }
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