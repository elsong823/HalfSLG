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
        None = 0,   //不能手动移动
        Move = 1,   //可以移动
        Skill = 2,  //可以使用技能
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
            BattleUnitAction battleUnitAction = BattleUnitAction.Create(this);
            battleField.AppendBattleAction(battleUnitAction);

            //恢复能量
            RecoverEnergy(battleUnitAction);
            //冷静
            battleBehaviourSystem.RageLevelCooldown();

            //手动
            if (battleUnitAttribute.manualOperation)
                return ManualAction(battleUnitAction);
            //自动
            else
                return AutoAction(battleUnitAction);

        }

        private void RecoverEnergy(BattleUnitAction battleUnitAction)
        {
            //数值改变
            battleUnitAttribute.energy += EGameConstL.EnergyRecoverPerRound;
            battleUnitAttribute.energy = battleUnitAttribute.energy > battleUnitAttribute.maxEnergy ? battleUnitAttribute.maxEnergy : battleUnitAttribute.energy;
            //创建一个Action
            if (battleUnitAction != null)
            {
                battleUnitAction.attributeUpdate = BattleUnitAttributeUpdate.Get();
                battleUnitAction.attributeUpdate.attribute = new BattleUnitSyncAttribute();
                battleUnitAction.attributeUpdate.attribute.hpChanged = 0;
                battleUnitAction.attributeUpdate.attribute.currentHP = battleUnitAttribute.hp;
                battleUnitAction.attributeUpdate.attribute.energyChanged = EGameConstL.EnergyRecoverPerRound;
                battleUnitAction.attributeUpdate.attribute.currentEnergy = battleUnitAttribute.energy;
            }
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
        private HeroActionState AutoAction(BattleUnitAction battleUnitAction)
        {
            BattleBehaviourSystem.BattleDecision decision = battleBehaviourSystem.Think();

            if(decision == null)
            {
                battleUnitAction.warningAction = BattleUnitWarningAction.Get();
                battleUnitAction.warningAction.logWarning = "No target:" + ID;
                return HeroActionState.Normal;
            }

            //判断是否切换目标
            if (decision.targetBattleUnit != null && !decision.targetBattleUnit.Equals(targetBattleUnit))
            {
                battleUnitAction.changeTargetAction = BattleUnitChangeTargetAction.Get();
                battleUnitAction.changeTargetAction.lastTargetUnit = targetBattleUnit;
                battleUnitAction.changeTargetAction.newTargetUnit = decision.targetBattleUnit;
                targetBattleUnit = decision.targetBattleUnit;
            }

            //需要移动
            if (decision.movePath != null && decision.movePath.Length > 0)
            {
                MoveToTargetGrid(battleUnitAction, targetBattleUnit, decision.movePath[decision.movePath.Length - 1], decision.movePath);
            }

            //自动搓招儿
            AutoUseSkill(battleUnitAction, decision);

            return HeroActionState.Normal;
        }

        //自动搓招
        private void AutoUseSkill(BattleUnitAction battleUnitAction, BattleBehaviourSystem.BattleDecision decision)
        {
            if (decision == null)
                return;

            if (decision.battleSkill != null)
            {
                //使用技能
                switch (decision.battleSkill.targetType)
                {
                    case BattleSkillTargetType.BattleUnit:
                        UseSkill(battleUnitAction, decision.battleSkill, decision.skillTargetBattleUnit);
                        break;

                    case BattleSkillTargetType.GridUnit:
                        UseSkill(battleUnitAction, decision.battleSkill, null, decision.skillTargetGrid);
                        break;

                    case BattleSkillTargetType.Self:
                        UseSkill(battleUnitAction, decision.battleSkill);
                        break;
                    default:
                        break;
                }
            }
        }

        //手动操作
        private HeroActionState ManualAction(BattleUnitAction battleUnitAction)
        {
            //重置手动操作状态
            manualActionState |= ManualActionState.Move;
            manualActionState |= ManualActionState.Skill;

            //创建一个手动操作的行动用于显示
            if (battleUnitAction != null)
                battleUnitAction.manualAction = LiteSingleton<BattleUnitManualAction>.Instance;
            
            return HeroActionState.WaitForPlayerChoose;
        }

        //向目标格子移动
        public void MoveToTargetGrid(BattleUnitAction battleUnitAction, BattleUnit targetUnit, GridUnit targetGrid, GridUnit[] gridPath)
        {
            //是否需要记录过程
            if (battleUnitAction != null)
            {
                battleUnitAction.motionAction = BattleUnitMotionAction.Get();
                battleUnitAction.motionAction.targetUnit = targetUnit;
                battleUnitAction.motionAction.fromGrid = mapGrid;
                battleUnitAction.motionAction.gridPath = gridPath;
                battleUnitAction.motionAction.moveRange = battleUnitAttribute.mobility;
            }
            //进入格子，直接设置数据
            EnterGrid(targetGrid);
        }
        
        //使用技能
        public void UseSkill(BattleUnitAction battleUnitAction, SO_BattleSkill battleSkill, BattleUnit targetBattleUnit = null, GridUnit targetGridUnit = null)
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
            if (battleUnitAction != null)
            {
                battleUnitAction.skillAction = BattleUnitSkillAction.Get();
                battleUnitAction.skillAction.battleSkill = battleSkill;
                battleUnitAction.skillAction.skillResult = skillResults.ToArray();
                battleUnitAction.skillAction.targetBattleUnit = targetBattleUnit;
                battleUnitAction.skillAction.targetGrid = targetGridUnit;
                battleUnitAction.skillAction.selfAttribute = BattleSkillCostEnergy(battleSkill);
            }

            //伤害产生效果，计算仇恨
            for (int i = 0; i < skillResults.Count; ++i)
            {
                //接收伤害，属性变更
                skillResults[i].battleUnit.AcceptSkillResult(skillResults[i].syncAttribute, battleUnitAction);
                
                //产生仇恨
                if (battleSkill.damageType != BattleSkillDamageType.Heal && !skillResults[i].battleUnit.Equals(this))
                {
                    //新仇记录
                    for (int j = 0; j < enemyTeam.battleUnits.Count; ++j)
                    {
                        if (!enemyTeam.battleUnits[j].CanAction)
                            continue;

                        //每个战斗单位都需要知道发生了什么
                        enemyTeam.battleUnits[j].battleBehaviourSystem.RecordSkillResult(this, skillResults[i]);
                    }
                }
            }
        }

        //被使用技能
        private void AcceptSkillResult(BattleUnitSyncAttribute sync, BattleUnitAction battleUnitAction)
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
                //重置bbsys
                battleBehaviourSystem.ResetSystem();
                
                EnterGrid(bornGrid);

                BattleUnitAction battleUnitAction = BattleUnitAction.Create(this);
                battleUnitAction.enterBattleFieldAction = BattleUnitEnterBattleFieldAction.Get();
                battleUnitAction.enterBattleFieldAction.bornGrid = bornGrid;
                battleUnitAction.enterBattleFieldAction.attribute = new BattleUnitSyncAttribute();
                battleUnitAction.enterBattleFieldAction.attribute.hpChanged = 0;
                battleUnitAction.enterBattleFieldAction.attribute.currentHP = battleUnitAttribute.hp;
                battleUnitAction.enterBattleFieldAction.attribute.energyChanged = 0;
                battleUnitAction.enterBattleFieldAction.attribute.currentEnergy = 0;

                //创建进入战场的消息
                battleField.AppendBattleAction(battleUnitAction);

                //初始化战斗行为系统
                battleBehaviourSystem.Init(this, battleField);
            }
        }

        //离开战场
        public void LeaveBattleField()
        {
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

        //技能相关
        //获取技能的停止移动距离
        private int SkillStopDistance
        {
            get
            {
                int minDistance = EGameConstL.Infinity;

                for (int i = 0; i < battleUnitAttribute.battleSkills.Length; ++i)
                {
                    //能量值不足的不考虑了
                    if (battleUnitAttribute.energy < battleUnitAttribute.battleSkills[i].energyCost)
                        continue;

                    //取最小的停止距离
                    minDistance = (minDistance > battleUnitAttribute.battleSkills[i].MaxReleaseRadiusForCalculate) ? battleUnitAttribute.battleSkills[i].MaxReleaseRadiusForCalculate : minDistance;
                }

                //总不能为无穷吧
                minDistance = (minDistance == EGameConstL.Infinity) ? 1 : minDistance;
                return minDistance;
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