using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    //战斗状态
    public enum BattleState
    {
        Prepare,        //准备中
        Ready,          //准备就绪
        Fighting,       //战斗中
        WaitForPlayer,  //等待玩家
        End,            //战斗结束
        Exception,      //战斗状态异常
    }


    public class BattleField
        : IVisualData<BattleField, BattleFieldRenderer>
    {
        public BattleState battleState = BattleState.Prepare;
        public int currentIndex;

        public int battleID;
        //地图信息
        public BattleMap battleMap;
        //参战队伍
        public List<BattleTeam> teams = new List<BattleTeam>();
        //战斗动作序列
        public MsgAction msgAction = new MsgAction();

        private Queue<BattleUnit> actionQueue = new Queue<BattleUnit>();
            
        private BattleFieldRenderer battleFieldRenderer;

        private int actionCount = 0;

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
        private int BattleEndState
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

        public bool CheckBattleEnd()
        {
            for (int i = 0; i < teams.Count; ++i)
            {
                int totalHP = 0;
                for (int j = 0; j < teams[i].battleUnits.Count; ++j)
                {
                    totalHP += teams[i].battleUnits[j].hp;
                }
                //一方生命为0，战斗结束
                if (totalHP <= 0)
                    return true;
            }
            return false;
        }

        private string Desc()
        {
            return string.Format("{0}\n{1}", teams[0].Desc(), teams[1].Desc());
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
                    if (bornUnit == null)
                    {
                        UtilityHelper.LogError("Get born unit failed.");
                        continue;
                    }
                    battleUnit.EnterBattleField(this, bornUnit, actions);
                    //生成行动队列
                    actionQueue.Enqueue(battleUnit);

                    if (recordProcess)
                        AppendBattleActions(actions);
                }
            }

            if (recordProcess)
                AppendBattleActions(actions);
        }

        //生成战斗结果
        private void GenerateBattleResult()
        {
            //TODO:生成战斗结果
            if (BattleEndState == 0)
                UtilityHelper.Log("Team 0 win");
            else if (BattleEndState == 1)
                UtilityHelper.Log("Team 1 win");
            else
                UtilityHelper.LogError("Draw game.");
        }

        //清空战斗过程
        private void CleanBattleAction()
        {
            msgAction.battleActions.Clear();
            msgAction = null;
        }

        //追加单个行动
        public void AppendBattleAction(BattleAction action)
        {
            if (action == null)
                return;

            if (msgAction.battleActions == null)
                msgAction.battleActions = new List<BattleAction>();

            msgAction.battleActions.Add(action);
        }

        //追加战斗行动
        public void AppendBattleActions(List<BattleAction> actions)
        {
            if (actions == null)
                return;

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

        //整理行动单位队列
        private void CalculateNextAction(BattleUnit actionUnit)
        {
            if (battleState == BattleState.End
                || actionUnit == null
                || !actionUnit.CanAction)
            {
                if (!actionUnit.CanAction)
                    Debug.Log("action death." + actionUnit);
                return;
            }

            actionQueue.Enqueue(actionUnit);
        }
        
        //准备战斗
        private void Prepare()
        {
            Debug.Log(string.Format("<color=#ff0000> {0} battle prepare. </color>", this.ToString()));

            //进入战场
            EnterBattleField(true);

            //到此生成了战斗入场数据
            battleState = BattleState.Ready;

            if (battleFieldRenderer != null)
            {
                Debug.Log("Play actions");
                battleFieldRenderer.PlayBattle(Fight);
            }
            else
            {
                Fight();
            }
        }

        //开始战斗
        private void Fight()
        {
            battleState = BattleState.Fighting;

            Debug.Log(string.Format("<color=#ff0000> {0} battle fight. </color>", this.ToString()));

            List<BattleAction> actions = new List<BattleAction>();

            BattleUnit actionUnit = null;

            do
            {
                //没有连接渲染器，则一步一更新
                //连接了渲染器，则一直更新直到结束
                actionUnit = actionQueue.Dequeue();

                if (actionUnit == null)
                {
                    battleState = BattleState.End;
                    break;
                }

                if (actionUnit.CanAction)
                {
                    HeroActionState state = actionUnit.BattleAction(actions);

                    switch (state)
                    {
                        case HeroActionState.BattleEnd:
                            battleState = BattleState.End;
                            break;

                        case HeroActionState.Error:
                            battleState = BattleState.Exception;
                            break;

                        case HeroActionState.Warn:
                            UtilityHelper.LogWarning(string.Format("Warning: battle action state warning -> {0}", actionUnit.ToString()));
                            break;

                        case HeroActionState.WaitForPlayerChoose:
                            battleState = BattleState.WaitForPlayer;
                            break;

                        default:
                            break;
                    }

                    //追加动作
                    AppendBattleActions(actions);
                }

                //考虑是否在放回队列
                CalculateNextAction(actionUnit);

                ++actionCount;

                if (actionCount >= EGameConstL.BattleFieldMaxActions)
                    battleState = BattleState.Exception;
            
            } while (battleFieldRenderer == null 
                        && battleState != BattleState.End
                        && battleState != BattleState.Exception);

            //连接了渲染器，一步一更新
            if (battleFieldRenderer != null)
            {
                Debug.Log("Play actions");

                if (battleState == BattleState.End || battleState == BattleState.Exception)
                    battleFieldRenderer.PlayBattle(BattleEnd);

                else if (battleState == BattleState.WaitForPlayer)
                    battleFieldRenderer.PlayBattle(null);

                else
                    battleFieldRenderer.PlayBattle(Fight);
            }
            else
            {
                //没有连接渲染器，自动战斗
                BattleEnd();
            }
        }

        //战斗结束
        private void BattleEnd()
        {
            if (battleState == BattleState.Exception)
            {
                UtilityHelper.LogError(string.Format("{0} battle error:", this.ToString()));
            }

            if (battleFieldRenderer != null)
                battleFieldRenderer.BattleEnd();

            Debug.Log(string.Format("<color=#ff0000> {0} battle end, step {1}.</color>", this.ToString(), actionCount));
        }

        //运行
        public void Run()
        {
            switch (battleState)
            {
                //当前正在准备阶段
                case BattleState.Prepare:
                    Prepare();
                    break;

                case BattleState.Ready:
                    Fight();
                    break;

                case BattleState.Fighting:
                    Fight();
                    break;

                case BattleState.WaitForPlayer:
                    UtilityHelper.LogError("Run error, wait for player operation.");
                    break;

                case BattleState.End:
                    BattleEnd();
                    break;

                case BattleState.Exception:
                    break;

                default:
                    break;
            }
        }

        //手动操作完成
        public void ManualOperationComplete()
        {
            if (CheckBattleEnd())
                battleState = BattleState.End;
            else
                battleState = BattleState.Fighting;
            
            //继续手动
            Run();
        }
    }
}