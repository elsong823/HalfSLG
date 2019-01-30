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
        BattleEnd,               //战斗结束
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
        : IVisualData<BattleUnit, BattleUnitRenderer>
    {

        public int battleUnitID;

        public bool manual = false; //手动操作
        
        private ManualActionState manualActionState = ManualActionState.None; //用于记录手动操作的状态

        public int hp;              //当前生命值
        public int maxHp;           //最大生命值
        public int atk;             //攻击力
        public int mobility;        //机动力，每次行动的范围

        //所在战场
        public BattleField battleField;
        //所属队伍
        public BattleTeam battleTeam;
        //敌方队伍
        public BattleTeam enemyTeam;
        //目标单位
        private BattleUnit targetBattleUnit;
        //所在格子
        public GridUnit mapGrid;
        //目标格子
        private GridUnit targetGrid;
        //移动到目标的路径
        private List<GridUnit> toTargetPath = new List<GridUnit>();

        //关联的渲染器
        public BattleUnitRenderer battleUnitRenderer;

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
                return hp > 0;
            }
        }

        /// <summary>
        /// 战斗单位动作的顺序是，移动->攻击
        /// </summary>
        /// <param name="heroActions"></param>
        /// <returns></returns>
        public HeroActionState BattleAction(List<BattleAction> heroActions)
        {
            if (enemyTeam == null)
                return HeroActionState.Error;

            if (heroActions != null)
                heroActions.Clear();

            //等待手动操作
            if (manual)
                return ManualAction(heroActions);
            else
                return AutoAction(heroActions);
        }

        //自动
        private HeroActionState AutoAction(List<BattleAction> heroActions)
        {
            //搜索敌人
            //如果存在可以攻击的目标
            //将会选定目标 以及 要移动到的位置（格子）
            TargetSearchResult searchResult = SearchTarget(heroActions);

            switch (searchResult)
            {
                //需要移动
                case TargetSearchResult.NeedMove:
                    //先移动
                    MoveToTargetGrid(heroActions, targetBattleUnit, targetGrid, toTargetPath.ToArray());
                    //移动后判断是否可以攻击
                    if (CheckUnderAttackRadius())
                        UseSkill(heroActions, targetBattleUnit, 0);
                    break;

                //在攻击范围内
                case TargetSearchResult.InRange:
                    UseSkill(heroActions, targetBattleUnit, 0);
                    break;

                //战斗结束
                case TargetSearchResult.Inexistence:
                    if (battleField.CheckBattleEnd())
                        return HeroActionState.BattleEnd;
                    else
                        return HeroActionState.Warn;

                default:
                    break;
            }

            return HeroActionState.Normal;
        }

        //手动操作
        private HeroActionState ManualAction(List<BattleAction> heroActions)
        {
            //重置手动操作状态
            manualActionState |= ManualActionState.Move;
            manualActionState |= ManualActionState.Skill;

            //创建一个手动操作的行动用于显示
            BattleHeroManualAction action = new BattleHeroManualAction(this);
            heroActions.Add(action);
            
            return HeroActionState.WaitForPlayerChoose;
        }

        //进入战场
        public void EnterBattleField(BattleField battleField, GridUnit bornGrid, List<BattleAction> heroActions)
        {
            if (battleField != null && bornGrid != null)
            {
                this.battleField = battleField;

                EnterGrid(bornGrid);
                
                if (heroActions != null)
                {
                    BattleHeroEnterBattleFieldAction action = new BattleHeroEnterBattleFieldAction(this);
                    action.gridUnit = bornGrid;
                    action.attribute = new BattleHeroSyncAttribute();
                    action.attribute.hpChanged = 0;
                    action.attribute.currentHP = hp;
                    heroActions.Add(action);
                }
            }
        }

        //搜索目标
        private TargetSearchResult SearchTarget(List<BattleAction> actions)
        {
            //按照距离排序敌人
            UtilityObjs.battleUnits.Clear();
            //只考虑可以行动的
            for (int i = 0; i < enemyTeam.battleUnits.Count; ++i)
            {
                if (enemyTeam.battleUnits[i].CanAction)
                {
                    UtilityObjs.battleUnits.Add(enemyTeam.battleUnits[i]);
                }
            }

            //天下无敌了，还有谁？？
            if (UtilityObjs.battleUnits.Count == 0)
                return TargetSearchResult.Inexistence;

            //结果类型
            TargetSearchResult searchResult = TargetSearchResult.InRange;

            //按照距离排序
            UtilityObjs.battleUnits.Sort(delegate (BattleUnit b1, BattleUnit b2)
            {
                return mapGrid.Distance(b1.mapGrid) - mapGrid.Distance(b2.mapGrid);
            });

            //暂时不添加复杂的逻辑，只选择直线距离最近的
            BattleUnit newTarget = null;
            GridUnit newTargetGrid = null;
            for (int i = 0; i < UtilityObjs.battleUnits.Count; ++i)
            {
                //如果当前目标就在范围内
                if (mapGrid.Distance(UtilityObjs.battleUnits[i].mapGrid) <= 1)
                {
                    //设置目标，但是不需要移动
                    newTarget = UtilityObjs.battleUnits[i];
                    toTargetPath.Clear();
                    targetGrid = null;
                    searchResult = TargetSearchResult.InRange;
                    break;
                }

                //目标不在周围需要移动
                newTargetGrid = battleField.battleMap.GetEmptyGrid(mapGrid, UtilityObjs.battleUnits[i].mapGrid, toTargetPath, mobility);
                if (newTargetGrid == null)
                {
                    //UtilityHelper.LogWarning(battleUnitID + "找不到空格子了,看看下一个吧");
                    continue;
                }
                else
                {
                    newTarget = UtilityObjs.battleUnits[i];
                    searchResult = TargetSearchResult.NeedMove;
                    break;
                }
            }

            if (newTarget == null)
            {
                UtilityHelper.LogWarning("确实找不到了");
                targetBattleUnit = null;
                targetGrid = null;
                toTargetPath.Clear();
                if (actions != null)
                {
                    //创建一个warning
                    BattleHeroWarningAction action = new BattleHeroWarningAction(this, "No target:" + battleUnitID);
                    actions.Add(action);
                }
                return TargetSearchResult.Inexistence;
            }

            //目标不一致，切换目标
            if (targetBattleUnit != newTarget)
            {
                //切换目标
                BattleHeroChangeTargetAction action = new BattleHeroChangeTargetAction(this);
                action.lastTargetUnit = targetBattleUnit;
                action.newTargetUnit = newTarget;

                //设置当前目标以及格子
                targetBattleUnit = newTarget;
                actions.Add(action);
            }

            //移动的格子重新设置
            targetGrid = newTargetGrid;

            return searchResult;
        }

        //向目标格子移动
        public void MoveToTargetGrid(List<BattleAction> actions, BattleUnit targetUnit, GridUnit targetGrid, GridUnit[] gridPath)
        {
            if (actions != null)
            {
                BattleHeroMotionAction action = new BattleHeroMotionAction(this);
                action.targetUnit = targetUnit;
                action.fromGrid = mapGrid;
                action.toGrid = targetGrid;
                action.gridPath = gridPath;
                action.moveRange = mobility;
                actions.Add(action);
            }
            EnterGrid(targetGrid);
        }

        //使用技能
        public void UseSkill(List<BattleAction> actions, BattleUnit target, int skillID)
        {
            if (target == null)
            {
                UtilityHelper.LogError("Use skill error, none target");
                return;
            }

            BattleHeroSkillResult skillResult = BattleCalculator.Instance.CalcSingle(this, target, skillID);

            if (actions != null)
            {
                BattleHeroSkillAction action = new BattleHeroSkillAction(this, skillID);
                action.skillResult = new BattleHeroSkillResult[1] { skillResult };
                actions.Add(action);
            }

            if (skillResult != null)
            {
                skillResult.battleUnit.AcceptSkillResult(skillResult.syncAttribute, actions);
            }
        }

        //被使用技能
        private void AcceptSkillResult(BattleHeroSyncAttribute sync, List<BattleAction> actions)
        {
            if (sync != null)
            {
                hp = sync.currentHP;
                if (hp <= 0)
                {
                    //被击败了
                    //从格子中移除
                    LeaveGrid();
                    if (actions != null)
                    {
                        BattleHerodDefeatedAction action = new BattleHerodDefeatedAction(this);
                        actions.Add(action);
                    }
                }
            }
        }

        //检查是否在攻击范围
        private bool CheckUnderAttackRadius()
        {
            return mapGrid.Distance(targetBattleUnit.mapGrid) <= 1;
        }

        //进入格子
        private void EnterGrid(GridUnit grid)
        {
            if (grid == null)
            {
                UtilityHelper.LogError(string.Format("Battle unit {0} enter grid failed, grid is null.", battleUnitID));
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

        public override string ToString()
        {
            return string.Format("BattleUnit_{0}_{1}", battleTeam.teamID, battleUnitID);
        }

        public string Desc()
        {
            return string.Format("{0} atk = {1} hp = {2}/{3}.", this.ToString(), atk, hp, maxHp);
        }

        public override bool Equals(object obj)
        {
            if(obj != null && obj is BattleUnit)
            {
                return ((BattleUnit)obj).battleUnitID == battleUnitID;
            }
            return false;
        }
    }
}