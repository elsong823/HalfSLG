using System.Collections.Generic;
using System.IO;
using System.Text;
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
        : CountableInstance, IVisualData<BattleField, BattleFieldRenderer>
    {
        private const int BATTLE_ACTIONS_DEFAULT_CAPACITY = 120;    //初始行动列表容量
        
        public BattleState battleState = BattleState.Prepare;       //战斗状态
        public BattleMap battleMap;                                 //地图信息
        public List<BattleTeam> teams = new List<BattleTeam>();     //参战队伍
        public List<BattleFieldEvent> battleFieldEvents = new List<BattleFieldEvent>(BATTLE_ACTIONS_DEFAULT_CAPACITY); //战斗行动信息
        private Queue<BattleUnit> actionQueue = new Queue<BattleUnit>();    //行动列表

        private BattleFieldRenderer battleFieldRenderer;
        private int resetTimes = 0;     //重置次数
        
        public void Init(
            int mapWidth, int mapHeight, 
            int obstacleCount, int obstacleGap,
            int buffCount, int itemCount,
            List<SO_BattleUnitAttribute> teamA, List<SO_BattleUnitAttribute> teamB)
        {
            //生成地图
            battleMap = BattleMapCreator.Instance.Create(mapWidth, mapHeight, obstacleCount, obstacleGap, buffCount, itemCount);
            battleMap.battleField = this;

            //生成战斗小组
            GenerateBattleTeam(teamA, teamB);
        }
        
        //生成战斗小组
        private void GenerateBattleTeam(List<SO_BattleUnitAttribute> teamA, List<SO_BattleUnitAttribute> teamB)
        {
            int teamCount = 2;
            //创建两支队伍
            for (int i = 0; i < teamCount; ++i)
            {
                teams.Add(BattleTeamCreator.Instance.Create(i == 0 ? teamA : teamB));
            }
        }

        //追加单个行动
        public void AppendBattleAction(BattleFieldEvent action)
        {
            if (action == null)
                return;

            battleFieldEvents.Add(action);
        }

        //追加战斗行动
        public void AppendBattleActions(List<BattleFieldEvent> actions)
        {
            if (actions == null)
                return;
            
            battleFieldEvents.AddRange(actions);
        }
        
        public void CheckBattleEnd()
        {
            for (int i = 0; i < teams.Count; ++i)
            {
                int totalHP = 0;
                for (int j = 0; j < teams[i].battleUnits.Count; ++j)
                {
                    totalHP += teams[i].battleUnits[j].battleUnitAttribute.hp;
                }
                //一方生命为0，战斗结束
                if (totalHP <= 0)
                {
                    battleState = BattleState.End;
                    return;
                }
            }
        }

        public BattleFieldRenderer Renderer
        {
            get
            {
                return battleFieldRenderer;
            }
        }

        //将战斗单位放置入战场
        private void EnterBattleField()
        {
            //队伍进入战场
            for (int i = 0; i < teams.Count; ++i)
            {
                //队伍进入战场
                teams[i].EnterBattleField(this, battleMap.GetBornGrid(i, teams[i].battleUnits.Count, true));
            }

            //随机行动顺序
            bool reverse = DebugHelper.Instance.randomFirstAction && (resetTimes & 1) == 0;
            for (int i = 0; i < teams.Count; ++i)
            {
                int teamIdx = reverse ? 1 - i : i;
                //随机
                List<BattleUnit> shuffle = new List<BattleUnit>(teams[teamIdx].battleUnits);
                UtilityHelper.Shuffle<BattleUnit>(shuffle);
                for (int j = 0; j < shuffle.Count; j++)
                {
                    actionQueue.Enqueue(shuffle[j]);
                }
            }
        }

        //生成战斗结果
        private void GenerateBattleResult()
        {
        }

        public override string ToString()
        {
            return string.Format("Battle field {0}", ID);
        }

        //整理行动单位队列
        private void CalculateNextAction(BattleUnit actionUnit)
        {
            //显然这个家伙已经不能战斗了
            if (battleState == BattleState.End
                || actionUnit == null
                || !actionUnit.CanAction)
            {
                return;
            }

            actionQueue.Enqueue(actionUnit);
        }
        
        //准备战斗
        private void Prepare()
        {
            //进入战场
            EnterBattleField();

            //到此生成了战斗入场数据
            battleState = BattleState.Ready;

            //连接了渲染器，则播放入场动作
            if (battleFieldRenderer != null)
                battleFieldRenderer.PlayBattle(Run);
            //直接开战
            else
                Fight();
        }

        //开始战斗
        private void Fight()
        {
            battleState = BattleState.Fighting;
            
            BattleUnit actionUnit = null;

            do
            {
                //连接渲染器，则一步一更新
                //没有连接渲染器，则一直计算直到结束
                actionUnit = actionQueue.Dequeue();

                if (actionUnit == null)
                {
                    battleState = BattleState.End;
                    break;
                }

                if (actionUnit.CanAction)
                {
                    HeroActionState state = actionUnit.BattleAction();

                    switch (state)
                    {
                        case HeroActionState.Normal:
                            battleState = BattleState.Fighting;
                            break;

                        case HeroActionState.WaitForPlayerChoose:
                            battleState = BattleState.WaitForPlayer;
                            break;

                        case HeroActionState.Error:
                            battleState = BattleState.Exception;
                            break;

                        case HeroActionState.Warn:
                            battleState = BattleState.Exception;
                            UtilityHelper.LogWarning(string.Format("Warning: battle action state warning -> {0}", actionUnit.ToString()));
                            break;

                        default:
                            break;
                    }
                }

                //考虑是否在放回队列
                CalculateNextAction(actionUnit);

                if (battleFieldEvents.Count > EGameConstL.BattleFieldMaxActions)
                {
                    UtilityHelper.LogError("Battle actions overflow max limit.");
                    battleState = BattleState.Exception;
                }
                else
                {
                    //只在这种情况下做战斗结束的判断
                    if (!actionUnit.CanAction
                        || actionUnit.targetBattleUnit == null
                        || !actionUnit.targetBattleUnit.CanAction)
                    {
                        CheckBattleEnd();
                    }
                }

            } while (battleFieldRenderer == null 
                        && battleState != BattleState.End
                        && battleState != BattleState.Exception);

            //连接了渲染器，一步一表现
            if (battleFieldRenderer != null)
            {
                if (battleState == BattleState.WaitForPlayer)
                    battleFieldRenderer.PlayBattle(null);

                else
                    battleFieldRenderer.PlayBattle(Run);
            }
            else
                Run();
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

            Debug.Log(string.Format("<color=#ff0000> {0} battle end, step {1}.</color>\n{2}", this.ToString(), battleFieldEvents.Count, Desc()));

            //输出到csv
            if(!string.IsNullOrEmpty(BattleManager.Instance.brPath))
                OutputBattleReport(BattleManager.Instance.brPath);

            //生成战斗结果
            GenerateBattleResult();
        }

        //输出分析战报
        private void OutputBattleReport(string path)
        {
            StringBuilder strBuilder = new StringBuilder();
            if(!File.Exists(path))
                strBuilder.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}\n", "BattleID", "State", "SN", "AtkTeam", "FirstAction", "Attacker", "SufferTeam", "Sufferer", "SkillName", "SkillType", "Damage/HP");

            foreach (var item in battleFieldEvents)
            {
                switch (item.actionType)
                {
                    case BattleFieldEventType.BattleUnitAction:
                        BattleUnitSkillAction skillAction = item as BattleUnitSkillAction;
                        if (skillAction != null )
                        {
                            foreach (var skillResult in skillAction.skillResult)
                            {
                                BattleTeam firstActionTeam = ((resetTimes & 1) == 0) ? teams[1] : teams[0];
                                strBuilder.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}\n",
                                    resetTimes,
                                    "Action",
                                    item.SN,
                                    skillAction.actionUnit.battleTeam,
                                    firstActionTeam.Equals(skillAction.actionUnit.battleTeam) ? 1 : 0,
                                    skillAction.actionUnit.battleUnitAttribute.battleUnitName,
                                    skillResult.battleUnit.battleTeam,
                                    skillResult.battleUnit.battleUnitAttribute.battleUnitName,
                                    skillAction.battleSkill.skillName,
                                    skillAction.battleSkill.damageType.ToString(),
                                    skillResult.syncAttribute.hpChanged);

                                //统计击杀
                                if (skillResult.syncAttribute.currentHP <= 0)
                                {
                                    strBuilder.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}\n",
                                        resetTimes,
                                        "Kill",
                                        item.SN,
                                        skillAction.actionUnit.battleTeam,
                                        firstActionTeam.Equals(skillAction.actionUnit.battleTeam) ? 1 : 0,
                                        skillAction.actionUnit.battleUnitAttribute.battleUnitName,
                                        skillResult.battleUnit.battleTeam,
                                        skillResult.battleUnit.battleUnitAttribute.battleUnitName,
                                        skillAction.battleSkill.skillName,
                                        skillAction.battleSkill.damageType.ToString(),
                                        skillResult.syncAttribute.hpChanged);
                                }
                            }
                        }
                        break;
                    case BattleFieldEventType.BattleStart:
                        break;
                    case BattleFieldEventType.BattleEnd:
                        break;
                    default:
                        break;
                }
            }
            foreach (var team in teams)
            {
                foreach (var bu in team.battleUnits)
                {
                    BattleTeam firstActionTeam = ((resetTimes & 1) == 0) ? teams[1] : teams[0];
                    strBuilder.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}\n",
                        resetTimes,
                        "End",
                        string.Empty,
                        bu.battleTeam,
                        firstActionTeam.Equals(bu.battleTeam) ? 1 : 0,
                        bu.battleUnitAttribute.battleUnitName,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        bu.battleUnitAttribute.hp);
                }
            }
            File.AppendAllText(path, strBuilder.ToString());
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

                //准备完毕（后期需要把进入战场后的等待效果做出来）
                case BattleState.Ready:
                    Fight();
                    break;
                
                //***主要的战斗逻辑
                case BattleState.Fighting:
                    Fight();
                    break;
                
                //等待玩家操作
                case BattleState.WaitForPlayer:
                    UtilityHelper.LogError("Run error, wait for player operation.");
                    break;
                
                //战斗结束
                case BattleState.End:
                    BattleEnd();
                    break;

                case BattleState.Exception:
                    break;

                default:
                    UtilityHelper.LogError(string.Format("Unknown battle state --> {0} <-- ", battleState));
                    break;
            }
        }

        //手动操作结束
        private void ManualOperationComplete(IGameEvent gameEvent)
        {
            CheckBattleEnd();

            if (battleState == BattleState.WaitForPlayer)
                battleState = BattleState.Fighting;

            Run();
        }

        /// <summary>
        /// 获取参加的
        /// </summary>
        /// <param name="battleUnit">目标战斗单位</param>
        /// <param name="sameTeam">是否相同队伍</param>
        public BattleTeam GetBattleTeam(BattleUnit battleUnit, bool sameTeam)
        {
            if (battleUnit == null)
                return null;

            if (teams[0].ID == battleUnit.battleTeam.ID)
                return sameTeam ? teams[0] : teams[1];
            else if (teams[1].ID == battleUnit.battleTeam.ID)
                return sameTeam ? teams[1] : teams[0];

            UtilityHelper.LogError(string.Format("Get battle team failed.Target teamID = {0}, team 0 id = {1}, team 1 id = {2}",
                battleUnit.battleTeam.ID,
                teams[0].ID,
                teams[1].ID));

            return null;
        }

        //重置一场战斗
        public void ResetBattle()
        {
            //通知队伍离开战场
            for (int i = 0; i < teams.Count; i++)
                teams[i].LeaveBattleField();

            battleMap.ResetMap();
            
            //重置状态
            battleState = BattleState.Prepare;
            
            //清空行动数据
            battleFieldEvents.Clear();

            //清空行动队列
            actionQueue.Clear();

            //增加重置次数
            ++resetTimes;

            if (battleFieldRenderer != null)
                battleFieldRenderer.RefreshRenderer();
        }

        //连接了渲染器
        public void ConnectRenderer(BattleFieldRenderer renderer)
        {
            //当前已经在显示状态中了，整啥呢啊？
            if (battleFieldRenderer != null)
                return;

            battleFieldRenderer = renderer;
            battleFieldRenderer.OnConnect(this);

            //连接了渲染器的需要注册一个消息的监听
            EventManager.Instance.Register(EGameConstL.EVENT_MANUAL_OPERATION_COMPLETE, string.Format("BattleField_{0}", ID), ManualOperationComplete);

            BattleManager.Instance.MgrLog(string.Format("{0} connect renderer.", this.ToString()));
        }

        //断开了从渲染器的连接
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

                EventManager.Instance.Unregister(string.Format("BattleField_{0}", ID));

                BattleManager.Instance.MgrLog(string.Format("{0} disconnect renderer.", this.ToString()));
            }
        }

        private string Desc()
        {
            return string.Format("{0}\n{1}", teams[0].Desc(), teams[1].Desc());
        }
    }
}