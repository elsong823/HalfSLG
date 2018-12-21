using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BattleField
        : IVisualData<BattleField, BattleFieldRenderer>
    {
        public int battleID;
        //地图信息
        public BattleMap battleMap;
        //参战队伍
        public List<BattleTeam> teams = new List<BattleTeam>();
        //战斗动作序列
        public MsgAction msgAction = new MsgAction();

        private BattleFieldRenderer battleFieldRenderer;

        public void Generate(int width, int height, int obstacleCount, int gap, int battleUnitCount)
        {
            //生成地图
            GenerateMap(width, height, obstacleCount, gap);
            //生成战斗小组
            GenerateBattleTeam(battleUnitCount);
        }

        //生成地图
        private void GenerateMap(int width, int height, int obstacleCount, int gap)
        {
            //创建地图
            battleMap = BattleMapManager.Instance.CreateMap(width, height, obstacleCount, gap);
        }

        //生成战斗小组
        private void GenerateBattleTeam(int battleUnitCount)
        {
            int teamCount = 2;
            //创建两支队伍
            for (int i = 0; i < teamCount; ++i)
            {
                //添加到地图中
                AddBattleTeam(BattleTeamManager.Instance.CreateBattleTeam());
            }

            if (battleUnitCount > battleMap.BornCount)
            {
                UtilityHelper.LogWarning(string.Format("Generate battle units warning.Not enough born points. {0}/{1}", battleUnitCount, battleMap.BornCount));
                battleUnitCount = battleMap.BornCount;
            }

            //为两支队伍添加战斗单位
            for (int i = 0; i < teamCount; ++i)
            {
                BattleTeam battleTeam = teams[i];
                if (battleTeam != null)
                {
                    for (int j = 0; j < battleUnitCount; ++j)
                    {
                        //创建战斗单位
                        BattleUnit battleUnit = BattleUnitManager.Instance.CreateUnit();
                        //加入队伍
                        battleTeam.AddBattleUnit(battleUnit);
                        //设置敌队
                        battleUnit.enemyTeam = teams[1 - i];
                    }
                }
            }
        }

        //添加战斗队伍
        public void AddBattleTeam(BattleTeam battleTeam)
        {
            if (battleTeam == null)
                return;

            teams.Add(battleTeam);
        }

        public void ConnectRenderer(BattleFieldRenderer renderer)
        {
            //当前已经在显示状态中了，整啥呢啊？
            if (battleFieldRenderer != null)
                return;

            battleFieldRenderer = renderer;
            battleFieldRenderer.OnConnect(this);
            UtilityHelper.Log(string.Format("{0} connect renderer.", this.ToString()));
        }

        public void DisconnectRenderer()
        {
            if (battleFieldRenderer != null)
            {
                //格子断开渲染
                foreach (var grid in battleMap.mapGrids)
                {
                    grid.DisconnectRenderer();
                }

                //战斗单位断开渲染
                foreach (var team in teams)
                {
                    foreach (var battleUnit in team.battleUnits)
                    {
                        battleUnit.DisconnectRenderer();
                    }
                }

                battleFieldRenderer.OnDisconnect();
                battleFieldRenderer = null;
                UtilityHelper.Log(string.Format("{0} disconnect renderer.", this.ToString()));
            }
        }

        //战斗状态
        //0 0队赢
        //1 1队赢
        //-1 进行中
        private int BattleState
        {
            get
            {
                for (int i = 0; i < teams.Count; ++i)
                {
                    int totalHP = 0;
                    for (int j = 0; j < teams[i].battleUnits.Count; ++j)
                    {
                        totalHP += teams[i].battleUnits[j].hp;
                    }
                    //如果这个队伍的hp总和为0，则认为对方获胜
                    if (totalHP <= 0)
                        return 1 - i;
                }
                return -1;
            }
        }

        private string Desc()
        {
            return string.Format("{0}\n{1}", teams[0].Desc(), teams[1].Desc());
        }

        //协同计算战斗，必记录过程
        public IEnumerator Run()
        {
            BattleUnit actionUnit = null;
            List<BattleAction> heroActions = new List<BattleAction>();

            //先进入战场
            EnterBattleField(true);

            //开始战斗
            int maxRound = 999;
            int round = 0;
            bool battleEnd = false;
            while (!battleEnd && round++ < maxRound)
            {
                //按照先后顺序
                for (int i = 0; i < teams.Count; ++i)
                {
                    //战斗结束
                    if (battleEnd)
                        break;

                    for (int j = 0; j < teams[i].battleUnits.Count; ++j)
                    {
                        //战斗结束
                        if (battleEnd)
                            break;

                        actionUnit = teams[i].battleUnits[j];
                        if (actionUnit.CanAction)
                        {
                            HeroActionState state =  actionUnit.BattleAction(heroActions);
                            AppendBattleActions(heroActions.ToArray());
                            switch (state)
                            {
                                case HeroActionState.Normal:
                                    //正常
                                    break;
                                case HeroActionState.WaitForPlayerChoose:
                                    yield return EGameConstL.WaitForTouchScreen;
                                    break;
                                case HeroActionState.BattleEnd:
                                    battleEnd = true;
                                    break;
                                case HeroActionState.Warn:
                                    break;
                                case HeroActionState.Error:
                                    UtilityHelper.LogError("Aciont error.");
                                    break;
                                default:
                                    UtilityHelper.LogError("Unknow hero action state..." + state);
                                    break;
                            }
                        }
                    }
                }
            }

            if (round >= maxRound)
                UtilityHelper.LogError(string.Format("Battle round greater than {0} -> {1}", maxRound, battleID));

            //生成战斗结果
            GenerateBattleResult();
        }

        //一次完成战斗计算，不一定需要记录过程
        public int Run(bool recordProcess)
        {
            BattleUnit actionUnit = null;
            List<BattleAction> heroActions = null;
            if(recordProcess)
                heroActions = new List<BattleAction>();

            //先进入战场
            EnterBattleField(recordProcess);

            //开始战斗
            int maxRound = 999;
            int round = 0;
            bool battleEnd = false;
            while (!battleEnd && round++ < maxRound)
            {
                //按照先后顺序
                for (int i = 0; i < teams.Count; ++i)
                {
                    //战斗结束
                    if (battleEnd)
                        break;

                    for (int j = 0; j < teams[i].battleUnits.Count; ++j)
                    {
                        //战斗结束
                        if (battleEnd)
                            break;

                        actionUnit = teams[i].battleUnits[j];
                        if (actionUnit.CanAction)
                        {
                            HeroActionState state = actionUnit.BattleAction(heroActions);
                            AppendBattleActions(heroActions.ToArray());
                            switch (state)
                            {
                                case HeroActionState.BattleEnd:
                                    battleEnd = true;
                                    break;
                                case HeroActionState.Warn:
                                    break;
                                case HeroActionState.Error:
                                    UtilityHelper.LogError("Aciont error.");
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }

            //生成战斗结果
            GenerateBattleResult();
            
            //判断是否记录过程
            if (!recordProcess)
                CleanBattleAction();

            return BattleState;
        }

        //将战斗单位放置入战场
        private void EnterBattleField(bool recordProcess)
        {
            BattleUnit battleUnit = null;
            List<BattleAction> actions = null;

            if (recordProcess)
                actions = new List<BattleAction>();

            for (int i = 0; i < teams.Count; ++i)
            {
                for (int j = 0; j < teams[i].battleUnits.Count; ++j)
                {
                    battleUnit = teams[i].battleUnits[j];
                    GridUnit bornUnit = battleMap.GetBornGrid(i, true);
                    if(bornUnit == null)
                    {
                        UtilityHelper.LogError("Get born unit failed.");
                        continue;
                    }
                    BattleHeroEnterBattleFieldAction action = battleUnit.EnterBattleField(this, bornUnit, recordProcess);
                    if (recordProcess && action != null)
                        actions.Add(action);
                }
            }

            if (recordProcess)
                AppendBattleActions(actions.ToArray());
        }

        //生成战斗结果
        private void GenerateBattleResult()
        {
            //TODO:生成战斗结果
            if (BattleState == 0)
                UtilityHelper.Log("Team 0 win");
            else if (BattleState == 1)
                UtilityHelper.Log("Team 1 win");
            else
                UtilityHelper.LogError("Draw game.");
        }

        //清空战斗过程
        private void CleanBattleAction()
        {

        }

        //追加战斗行动
        private void AppendBattleActions(BattleAction[] actions)
        {
            if (msgAction.battleActions == null)
            {
                msgAction.battleActions = new List<BattleAction>(actions);
                return;
            }

            this.msgAction.battleActions.AddRange(actions);
        }

        public override string ToString()
        {
            return string.Format("Battle field {0}", battleID);
        }
    }
}