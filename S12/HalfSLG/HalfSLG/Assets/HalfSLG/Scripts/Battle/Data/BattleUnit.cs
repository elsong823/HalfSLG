using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public enum TargetSearchResult
    {
        NeedMove,       //存在目标但需要移动过去
        InRange,        //目标在攻击范围内，无需移动
        Inexistence,    //不存在目标
    }

    public enum HeroActionState
    {
        Normal,                  //正常
        WaitForPlayerChoose,     //等待玩家操作
        Error,                   //错误
        Warn,                    //警告(测试用)
    }

    //0 1 2 4  ....
    // 3 : 可以移动，可以使用技能
    // 2 : 仅可以使用技能(已经移动过)
    // 0 ：不能移动、不能使用技能(已经移动并使用了技能)
    [Flags]
    public enum ManualActionState
    {
        None  = 0,   //不能手动移动
        Move  = 1,   //可以移动
        SkillOrItem = 2,   //可以使用技能或道具
    }

    public class BattleUnit
        : CountableInstance, IVisualData<BattleUnit, BattleUnitRenderer>
    {
        private ManualActionState manualActionState = ManualActionState.None; //用于记录手动操作的状态

        //战斗机器人(属性)
        public SO_BattleUnitAttribute battleUnitAttribute;
        
        //所在战场
        public BattleField battleField;
        //所属队伍
        public BattleTeam battleTeam;
        //敌方队伍
        public BattleTeam enemyTeam;
        //目标单位
        public BattleUnit targetBattleUnit;
        //所在格子
        public GridUnit mapGrid;
        //背包
        public BattleUnitPackage package;

        //关联的渲染器
        public BattleUnitRenderer battleUnitRenderer;

        public BattleBehaviourSystem.BattleBehaviourSystem battleBehaviourSystem;
        
        //判断一个手动操作的目标是否可以进行某些操作
        public bool CheckManualState(ManualActionState actionState)
        {
            if ((manualActionState & actionState) != ManualActionState.None)
            {
                return true;
            }
            return false;
        }

        //完成一个手动操作，核销这个状态
        public void CompleteManualState(ManualActionState actionState)
        {
            manualActionState &= (~actionState);
            BroadcastManualStateChanged();
        }

        //扶我起来，我还可以改bug
        public bool CanAction
        {
            get
            {
                return battleUnitAttribute.hp > 0;
            }
        }

        /// <summary>
        /// 战斗单位动作的顺序是，移动->攻击
        /// </summary>
        /// <param name="heroActions"></param>
        /// <returns></returns>
        public HeroActionState BattleAction()
        {
            //恢复能量
            RecoverEnergy();
            //冷静
            if(battleBehaviourSystem != null)
                battleBehaviourSystem.RageLevelCooldown();

            //手动
            if (battleUnitAttribute.manualOperation)
                return ManualAction();
            //自动
            else
                return AutoAction();

        }

        private void RecoverEnergy()
        {
            //数值改变
            battleUnitAttribute.energy += EGameConstL.EnergyRecoverPerRound;
            battleUnitAttribute.energy = battleUnitAttribute.energy > battleUnitAttribute.maxEnergy ? battleUnitAttribute.maxEnergy : battleUnitAttribute.energy;

            //创建一个Action
            BattleUnitAttributeUpdate action = BattleUnitActionEvent.CreateEvent<BattleUnitAttributeUpdate>(BattleUnitActionType.AttributeUpdate, this);
            action.attribute = new BattleUnitSyncAttribute();
            action.attribute.hpChanged = 0;
            action.attribute.currentHP = battleUnitAttribute.hp;
            action.attribute.energyChanged = EGameConstL.EnergyRecoverPerRound;
            action.attribute.currentEnergy = battleUnitAttribute.energy;

            battleField.AppendBattleAction(action);
        }

        private BattleUnitSyncAttribute BattleSkillCostEnergy(SO_BattleSkill skill)
        {
            battleUnitAttribute.energy -= skill.energyCost;
            battleUnitAttribute.energy = battleUnitAttribute.energy < 0 ? 0 : battleUnitAttribute.energy; 

            BattleUnitSyncAttribute attribute = new BattleUnitSyncAttribute();
            attribute.hpChanged = 0;
            attribute.currentHP = battleUnitAttribute.hp;
            attribute.energyChanged = -skill.energyCost;
            attribute.currentEnergy = battleUnitAttribute.energy;

            return attribute;
        }

        //自动
        private HeroActionState AutoAction()
        {
            BattleBehaviourSystem.BattleDecision decision = battleBehaviourSystem.Think();

            if(decision == null)
            {
                BattleUnitWarningAction warningAction = BattleUnitActionEvent.CreateEvent<BattleUnitWarningAction>(BattleUnitActionType.Warning, this);
                battleField.AppendBattleAction(warningAction);
                warningAction.warningLog = "No target:" + ID;
                return HeroActionState.Normal;
            }

            //判断是否切换目标
            if (decision.targetBattleUnit != null && !decision.targetBattleUnit.Equals(targetBattleUnit))
            {
                BattleUnitChangeTargetAction action = BattleUnitActionEvent.CreateEvent<BattleUnitChangeTargetAction>(BattleUnitActionType.ChangeTarget, this);
                action.lastTargetUnit = targetBattleUnit;
                action.newTargetUnit = decision.targetBattleUnit;
                battleField.AppendBattleAction(action);

                targetBattleUnit = decision.targetBattleUnit;
            }

            //需要移动
            if (decision.movePath != null && decision.movePath.Length > 0)
            {
                MoveToTargetGrid(targetBattleUnit, decision.movePath[decision.movePath.Length - 1], decision.movePath);
            }

            //自动搓招儿
            AutoUseSkill(decision);

            return HeroActionState.Normal;
        }

        //自动搓招
        private void AutoUseSkill(BattleBehaviourSystem.BattleDecision decision)
        {
            if (decision == null)
                return;

            if (decision.battleSkill != null)
            {
                //使用技能
                switch (decision.battleSkill.targetType)
                {
                    case BattleSkillTargetType.BattleUnit:
                        UseSkill(decision.battleSkill, decision.skillTargetBattleUnit);
                        break;

                    case BattleSkillTargetType.GridUnit:
                        UseSkill(decision.battleSkill, null, decision.skillTargetGrid);
                        break;

                    case BattleSkillTargetType.Self:
                        UseSkill(decision.battleSkill);
                        break;
                    default:
                        break;
                }
            }
        }

        private void BroadcastManualStateChanged()
        {
            NormalMessage normalMessage = new NormalMessage(EGameConstL.EVENT_BATTLE_UNIT_MANUAL_STATE_CHANGED);
            normalMessage.Body = this;
            EventManager.Instance.Run(EGameConstL.EVENT_BATTLE_UNIT_MANUAL_STATE_CHANGED, normalMessage);
        }

        //手动操作
        private HeroActionState ManualAction()
        {
            //重置手动操作状态
            manualActionState |= ManualActionState.Move;
            manualActionState |= ManualActionState.SkillOrItem;
            BroadcastManualStateChanged();

            //创建一个手动操作的行动用于显示
            BattleUnitManualAction action = BattleUnitActionEvent.CreateEvent<BattleUnitManualAction>(BattleUnitActionType.ManualOperate, this);
            battleField.AppendBattleAction(action);

            return HeroActionState.WaitForPlayerChoose;
        }

        //向目标格子移动
        public void MoveToTargetGrid(BattleUnit targetUnit, GridUnit targetGrid, GridUnit[] gridPath)
        {
            BattleUnitMotionAction action = BattleUnitActionEvent.CreateEvent<BattleUnitMotionAction>(BattleUnitActionType.MoveToTarget, this);

            action.targetUnit = targetUnit;
            action.fromGrid = mapGrid;
            action.gridPath = gridPath;
            action.moveRange = battleUnitAttribute.mobility;

            battleField.AppendBattleAction(action);

            //进入格子，直接设置数据
            EnterGrid(targetGrid);
        }
        
        //使用技能
        public void UseSkill(SO_BattleSkill battleSkill, BattleUnit targetBattleUnit = null, GridUnit targetGridUnit = null)
        {
            if (battleSkill == null)
            {
                UtilityHelper.LogError("Use skill error. Battle skill is none.");
                return;
            }
            BattleSkillEffectAnalysis analysis = BattleCalculator.Instance.AnalyseBattleSkillEffect(battleSkill, this, targetBattleUnit, targetGridUnit);
            if (analysis == null)
            {
                UtilityHelper.LogError("Use skill error. Analysis failed:" + battleSkill.skillName);
                return;
            }

            List<BattleUnitSkillResult> skillResults = new List<BattleUnitSkillResult>();
            
            //主要影响
            for (int i = 0; i < analysis.mainReceiver.Count; ++i)
                skillResults.Add(BattleCalculator.Instance.CalcSingle(this, analysis.mainReceiver[i], battleSkill, true));
            //次要影响
            for (int i = 0; i < analysis.minorReceiver.Count; ++i)
                skillResults.Add(BattleCalculator.Instance.CalcSingle(this, analysis.minorReceiver[i], battleSkill, false));


            //产生使用技能的动作
            BattleUnitSkillAction skillAction = BattleUnitActionEvent.CreateEvent<BattleUnitSkillAction>(BattleUnitActionType.UseSkill, this);
            skillAction.battleSkill = battleSkill;
            skillAction.skillResult = skillResults.ToArray();
            skillAction.targetBattleUnit = targetBattleUnit;
            skillAction.targetGrid = targetGridUnit;
            skillAction.selfAttribute = BattleSkillCostEnergy(battleSkill);
            battleField.AppendBattleAction(skillAction);

            //伤害产生效果，计算仇恨
            for (int i = 0; i < skillResults.Count; ++i)
            {
                //接收伤害，属性变更
                skillResults[i].battleUnit.AcceptSkillResult(skillResults[i].syncAttribute);
                
                //产生仇恨
                if (battleSkill.damageType != BattleSkillDamageType.Heal && !skillResults[i].battleUnit.Equals(this))
                {
                    //新仇记录
                    for (int j = 0; j < enemyTeam.battleUnits.Count; ++j)
                    {
                        if (!enemyTeam.battleUnits[j].CanAction)
                            continue;

                        //每个战斗单位都需要知道发生了什么
                        if(!enemyTeam.battleUnits[j].battleUnitAttribute.manualOperation)
                            enemyTeam.battleUnits[j].battleBehaviourSystem.RecordSkillResult(this, skillResults[i]);
                    }
                }
            }
        }

        //被使用技能
        private void AcceptSkillResult(BattleUnitSyncAttribute sync)
        {
            if (sync != null)
            {
                battleUnitAttribute.hp = sync.currentHP;

                if (battleUnitAttribute.hp <= 0)
                {
                    //被击败了
                    //从格子中移除
                    LeaveGrid();
                }
            }
        }

        //进入队伍
        public void JoinBattleTeam(BattleTeam team)
        {
            battleTeam = team;
        }

        //离开队伍
        public void QuitBattleTeam()
        {
            battleTeam = null;
        }

        //进入战场
        public void EnterBattleField(BattleField battleField, GridUnit bornGrid)
        {
            if (battleField != null && bornGrid != null)
            {
                this.battleField = battleField;

                //设置敌方队伍
                enemyTeam = battleField.GetBattleTeam(this, false);

                //重置属性
                battleUnitAttribute.RandomAttributes();
                battleUnitAttribute.Reset();

                //创建背包
                if (package == null)
                    package = BattleUnitPackage.CreateInstance(this, 2);

                package.Clear();

                //进入战场
                BattleUnitEnterBattleFieldAction enterBattleFieldAction = BattleUnitActionEvent.CreateEvent<BattleUnitEnterBattleFieldAction>(BattleUnitActionType.EnterBattleField, this);
                enterBattleFieldAction.battleField = battleField;
                enterBattleFieldAction.bornGrid = bornGrid;
                enterBattleFieldAction.attribute = new BattleUnitSyncAttribute();
                enterBattleFieldAction.attribute.hpChanged = 0;
                enterBattleFieldAction.attribute.currentHP = battleUnitAttribute.hp;
                enterBattleFieldAction.attribute.energyChanged = 0;
                enterBattleFieldAction.attribute.currentEnergy = 0;
                battleField.AppendBattleAction(enterBattleFieldAction);

                //进入格子
                EnterGrid(bornGrid);

                //初始化战斗行为系统
                if (battleBehaviourSystem != null)
                    battleBehaviourSystem.Init(this, battleField);

                //重置bbsys
                if (battleBehaviourSystem != null)
                    battleBehaviourSystem.ResetSystem();
            }
        }

        //离开战场
        public void LeaveBattleField()
        {
            LeaveGrid();
            if (battleUnitRenderer != null)
                battleUnitRenderer.StopAllCoroutines();
        }

        //进入格子
        private void EnterGrid(GridUnit grid)
        {
            if (grid == null)
            {
                UtilityHelper.LogError(string.Format("Battle unit {0} enter grid failed, grid is null.", ID));
                return;
            }
            if (mapGrid != null)
                LeaveGrid();

            mapGrid = grid;

            //判断这个格子是否有道具
            if (grid.gridItem != null)
                PickupItemFromGrid(grid);

            //通知格子被自己进入了
            grid.OnEnter(this);
        }

        //离开格子
        private void LeaveGrid()
        {
            if (mapGrid != null)
            {
                mapGrid.OnLeave();
                mapGrid = null;
            }
        }

        //拾起道具
        public void PickupItemFromGrid(GridUnit fromGrid)
        {
            if (fromGrid == null || fromGrid.gridItem == null || package == null)
                return;

            int itemID = fromGrid.gridItem.item.itemID;
            int itemCount = fromGrid.gridItem.count;
            int finalCount = 0;
            bool result = package.TryAddItem(itemID, itemCount, ref finalCount);
            //成功捡起道具
            if (result)
            {
                //先生成道具格子的事件
                if (fromGrid != null)
                    fromGrid.OnItemPicked();

                BattleUnitPickupItemAction action = BattleUnitActionEvent.CreateEvent<BattleUnitPickupItemAction>(BattleUnitActionType.PickupItem, this);
                action.itemID = itemID;
                action.addCount = itemCount;
                action.finalCount = finalCount;
                battleField.AppendBattleAction(action);
            }
        }

        //使用道具
        public bool UseItem(int itemID, int itemCount)
        {
            if (package != null)
            {
                var item = PackageItemManager.Instance.GetItem(itemID);
                if (item == null)
                    return false;

                int finalCount = 0;
                int usedCount = package.TryUseItem(itemID, itemCount, ref finalCount);
                if (usedCount > 0)
                {
                    BattleUnitUseItemAction action = BattleUnitActionEvent.CreateEvent<BattleUnitUseItemAction>(BattleUnitActionType.UseItem, this);
                    action.itemID = itemID;
                    action.useCount = usedCount;
                    action.remainCount = finalCount;
                    switch (item.itemType)
                    {
                        case PackageItemType.Recover:
                            action.attributeUpdate = new BattleUnitSyncAttribute();
                            action.attributeUpdate.hpChanged = Mathf.Min(battleUnitAttribute.maxHp - battleUnitAttribute.hp, item.hpRecovery);
                            action.attributeUpdate.energyChanged = Mathf.Min(battleUnitAttribute.maxEnergy - battleUnitAttribute.energy, item.energyRecovery);
                            action.attributeUpdate.currentHP = battleUnitAttribute.hp + action.attributeUpdate.hpChanged;
                            action.attributeUpdate.currentEnergy = battleUnitAttribute.energy + action.attributeUpdate.energyChanged;

                            //自身属性更新
                            battleUnitAttribute.hp = action.attributeUpdate.currentHP;
                            battleUnitAttribute.energy = action.attributeUpdate.currentEnergy;

                            break;
                        default:
                            break;
                    }
                    battleField.AppendBattleAction(action);
                }

                return usedCount > 0;
            } 
            else
            {
                UtilityHelper.LogError(string.Format("Use item failed, no package : {0} -> {1}", battleUnitAttribute.battleUnitName, itemID));
                return false;
            }
        }
        
        //连接渲染器
        public void ConnectRenderer(BattleUnitRenderer renderer)
        {
            if (renderer == null)
            {
                UtilityHelper.LogError("Battle unit connect renderer failed. RD is null");
                return;
            }

            if (battleUnitRenderer != null)
                DisconnectRenderer();

            battleUnitRenderer = renderer;
            battleUnitRenderer.OnConnect(this);
        }

        //断开渲染器
        public void DisconnectRenderer()
        {
            if (battleUnitRenderer != null)
            {
                battleUnitRenderer.OnDisconnect();
                battleUnitRenderer = null;
            }
        }
        
        public override string ToString()
        {
            return string.Format("BattleUnit_{0}_{1}", battleTeam.ID, ID);
        }

        public string Desc()
        {
            return string.Format("Name:{0},HP{1}/{2}", battleUnitAttribute.battleUnitName, battleUnitAttribute.hp, battleUnitAttribute.maxHp);
        }

        public string PrintThinking()
        {
            //hsSystem.Thinking();
            //return hsSystem.Desc();
            return string.Empty;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is BattleUnit)
            {
                return ((BattleUnit)obj).ID == ID;
            }
            return false;
        }
    }
}