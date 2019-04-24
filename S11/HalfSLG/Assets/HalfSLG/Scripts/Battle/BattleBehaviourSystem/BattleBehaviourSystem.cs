using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ELGame.BattleBehaviourSystem
{
#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BattleBehaviourSystem))]
    public class BattleBehaviourSystemEditor
        : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Refresh Pct"))
            {
                BattleBehaviourSystem system = (BattleBehaviourSystem)target;
                if (system != null)
                {
                    system.battleBehaviourPct.damage = system.damageChip == null ? 0f : system.battleBehaviourPct.damage;
                    system.battleBehaviourPct.provoke = system.provokeChip == null ? 0f : system.battleBehaviourPct.provoke;
                    system.battleBehaviourPct.recovery = system.recoveryChip == null ? 0f : system.battleBehaviourPct.recovery;

                    system.battleBehaviourPct.Refresh();
                }
            }
        }
    }
#endif

    //战斗行为类型
    public enum BattleBehaviourType
    {
        Rage,       //愤怒
        Strategy,   //策略
        Damage,     //伤害
        Provoke,    //嘲讽
        Recovery,   //恢复
    }

    //战斗单位的角色
    public enum BattleUnitRole
    {
        Tank,       //伤害承担
        DPS,        //输出
        Support,    //辅助
    }
    
    //战斗基本数据
    public class BattleBaseData
    {
        public BattleUnit hostBattleUnit;
        public BattleField battleField;
        public BattleTeam ownBattleTeam;
        public BattleTeam enemyBattleTeam;

        private BattleBaseData() { }

        public static BattleBaseData CreateInstance(BattleUnit battleUnit, BattleField battleField)
        {
            BattleBaseData item = new BattleBaseData();
            item.hostBattleUnit = battleUnit;
            item.battleField = battleField;
            item.ownBattleTeam = battleUnit.battleTeam;
            item.enemyBattleTeam = battleField.GetBattleTeam(battleUnit, false);

            return item;
        }
    }

    [System.Serializable]
    public class BattleBehaviourPct
    {
        [Range(0, 1f)]
        public float damage = 0f;
        [Range(0, 1f)]
        public float provoke = 0f;
        [Range(0, 1f)]
        public float recovery = 0f;

        public void Refresh()
        {
            float sum = damage + provoke + recovery;
            if (sum <= 0f)
                return;

            damage /= sum;
            provoke /= sum;
            recovery /= sum;
        }

        public float GetWeight(BattleBehaviourType behaviourType)
        {
            switch (behaviourType)
            {
                case BattleBehaviourType.Strategy:
                    return 0f;

                //愤怒将影响damage，因此算一类
                case BattleBehaviourType.Rage:
                case BattleBehaviourType.Damage:
                    return damage;

                case BattleBehaviourType.Provoke:
                    return provoke;

                case BattleBehaviourType.Recovery:
                    return recovery;

                default:
                    UtilityHelper.LogError(string.Format("Get weight error, type = {0}", behaviourType));
                    return 0f;
            }
        }
    }
    
    //技能分析结果
    public class BattleSkillAutoReleaseAnalysisItem
    {
        public SO_BattleSkill battleSkill;
        public BattleUnit targetBattleUnit; //目标战斗单位
        public GridUnit targetGridUnit;     //目标地块单位
        public float score;                 //释放这个技能的得分
        public int effectedCount;           //这个技能可以影响的人数

        private BattleSkillAutoReleaseAnalysisItem() { }

        public static BattleSkillAutoReleaseAnalysisItem CreateInstance()
        {
            return new BattleSkillAutoReleaseAnalysisItem();
        }

        public void Recycle()
        {
            battleSkill = null;
            targetBattleUnit = null;
            targetGridUnit = null;
            score = 0;
            effectedCount = 0;
        }
    }

    //战斗决定
    public class BattleDecision
    {
        public BattleUnit targetBattleUnit; //目标单位
        public GridUnit[] movePath;         //移动格子
        public SO_BattleSkill battleSkill;  //所选的技能
        public GridUnit skillTargetGrid;    //释放技能的目标格子
        public BattleUnit skillTargetBattleUnit;    //释放技能的目标战斗单位

        private BattleDecision() { }

        public static BattleDecision CreateInstance()
        {
            return new BattleDecision();
        }

        public void Reset()
        {
            targetBattleUnit = null;
            movePath = null;
            battleSkill = null;
            skillTargetGrid = null;
            skillTargetBattleUnit = null;
        }
    }

    [CreateAssetMenu(menuName = "BBSystem/BattleBehaviourSys", order = 100)]
    public class BattleBehaviourSystem 
        : ScriptableObject
    {
        public BattleBaseData baseData;

        public BattleUnitRole battleUnitRole;
        
        public BattleRageChip rageChip = null;
        public BattleDamageChip damageChip = null;
        public BattleProvokeChip provokeChip = null;
        public BattleRecoveryChip recoveryChip = null;

        public BattleBehaviourPct battleBehaviourPct;

        private List<BattleBehaviourItem> behaviourItems = new List<BattleBehaviourItem>();

        //初始化芯片
        private void InitChip<T>(ref T chip)
            where T : ScriptableObject, IBattleBehaviourChip
        {
            if (chip == null)
                return;

            chip = Instantiate(chip);
            chip.Init(this, baseData);
        }

        public void Init(BattleUnit battleUnit, BattleField battleField)
        {
            if (baseData != null)
                return;

            //初始化基础信息
            baseData = BattleBaseData.CreateInstance(battleUnit, battleField);
            //初始化芯片
            InitChip(ref rageChip);
            InitChip(ref damageChip);
            InitChip(ref provokeChip);
            InitChip(ref recoveryChip);
        }

        public float GetDistanceWeight(int distance)
        {
            int gap = distance - baseData.hostBattleUnit.battleUnitAttribute.stopDistance;
            int range = baseData.hostBattleUnit.battleUnitAttribute.mobility * 3;
            //范围内
            if (gap <= 0)
                return 1f;
            //Lerp
            else if(gap <= range)
                return Mathf.Lerp(0.3f, 1f, ((float)range - gap) / range);
            return
                0.3f;
        }

        public void RageLevelCooldown()
        {
            if (rageChip != null)
                rageChip.RageLevelCooldown();
        }

        //对当前局势进行分析，并得出战斗决策
        public BattleDecision Think()
        {
            behaviourItems.Clear();

            if(provokeChip != null)
                provokeChip.CalculateBehaviourItem(behaviourItems, battleBehaviourPct.GetWeight(provokeChip.BehaviourType));

            if (damageChip != null)
                damageChip.CalculateBehaviourItem(behaviourItems, battleBehaviourPct.GetWeight(damageChip.BehaviourType));

            if (recoveryChip != null)
                recoveryChip.CalculateBehaviourItem(behaviourItems, battleBehaviourPct.GetWeight(recoveryChip.BehaviourType));
            
            //愤怒芯片最后计算，因为要给damage类型的芯片增加决策分数
            if (rageChip != null)
                rageChip.CalculateBehaviourItem(behaviourItems, battleBehaviourPct.GetWeight(rageChip.BehaviourType));
            
            //排序
            behaviourItems.Sort(LiteSingleton<BattleBehaviourItemComparer>.Instance);

#if UNITY_EDITOR
            if(DebugHelper.Instance.debugBBSys)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendFormat("Action battle unit:{0}\n", baseData.hostBattleUnit.battleUnitAttribute.battleUnitName);
                foreach (var item in behaviourItems)
                {
                    sb.AppendFormat("Target = {0},type = {1}, point = {2}\n", 
                    item.targetBattleUnit.battleUnitAttribute.battleUnitName,
                    item.behaviourType,
                    item.point
                      );
                }
                Debug.Log(sb.ToString());
            }
#endif

            return MakeDecision();
        }

        //计算单个技能释放的得分
        private float CalculateSkillScore(BattleUnit releaser, BattleUnit target, SO_BattleSkill battleSkill, BattleBehaviourType battleBehaviourType)
        {
            switch (battleBehaviourType)
            {
                //伤害型
                case BattleBehaviourType.Damage:
                    return releaser.battleUnitAttribute.Atk - target.battleUnitAttribute.Def + battleSkill.mainValue;
                //仇恨型
                case BattleBehaviourType.Provoke:
                    return (releaser.battleUnitAttribute.Atk - target.battleUnitAttribute.Def + battleSkill.mainValue) * battleSkill.hatredMultiple;
                //恢复型
                case BattleBehaviourType.Recovery:
                    return battleSkill.mainValue;
                default:
                    UtilityHelper.LogError(string.Format("CalculateSkillScore error, unknown type:{0}", battleBehaviourType));
                    return 0f;
            }
        }

        /// <summary>
        /// 计算释放技能的得分
        /// </summary>
        /// <returns>The skill score.</returns>
        /// <param name="releaser">释放技能的人</param>
        /// <param name="releaserPos">释放位置</param>
        /// <param name="target">目标对象</param>
        /// <param name="skill">使用的技能</param>
        /// <param name="behaviourType">行为类型</param>
        private BattleSkillAutoReleaseAnalysisItem CalculateSkillScore(BattleUnit releaser, GridUnit releaserPos, BattleUnit target, SO_BattleSkill skill, BattleBehaviourType behaviourType)
        {
            //判断能量是否够
            if (skill.energyCost > releaser.battleUnitAttribute.energy)
                return null;

            //释放这个技能的主要影响目标和次要影响目标
            BattleSkillEffectAnalysis effectAnalysis = null;
            //捎带手保存一下释放技能的地点
            GridUnit targetGridUnit = null;

            List<GridUnit> analysisTempGrids = new List<GridUnit>();
            switch (skill.targetType)
            {
                //对目标单位
                case BattleSkillTargetType.BattleUnit:
                    effectAnalysis = BattleCalculator.Instance.AnalyseBattleSkillEffect(
                        skill,
                        releaser,
                        target);
                    break;

                case BattleSkillTargetType.Self:
                    effectAnalysis = BattleCalculator.Instance.AnalyseBattleSkillEffect(
                        skill,
                        releaser);
                    break;

                case BattleSkillTargetType.GridUnit:
                    analysisTempGrids.Clear();
                    //如果目标在范围内
                    if (skill.GetReleaseRadius(releaser.mapGrid) > 0 
                        && releaser.mapGrid.Distance(target.mapGrid) <= skill.GetReleaseRadius(releaser.mapGrid))
                    {
                        effectAnalysis = BattleCalculator.Instance.AnalyseBattleSkillEffect(
                            skill,
                            releaser,
                            null,
                            target.mapGrid);

                        //以地块为目标的技能，保存一下目标地块
                        targetGridUnit = target.mapGrid;
                    }
                    //自身为中心
                    else if (skill.GetReleaseRadius(releaser.mapGrid) <= 0 
                        && releaser.mapGrid.Distance(target.mapGrid) <= skill.effectRadius)
                    {
                        effectAnalysis = BattleCalculator.Instance.AnalyseBattleSkillEffect(
                            skill,
                            releaser,
                            null,
                            releaser.mapGrid);

                        //以地块为目标的技能，保存一下目标地块
                        targetGridUnit = releaser.mapGrid;
                    }
                    else
                    {
                        //这个最麻烦，对某个位置
                        target.battleField.battleMap.GetCircularGrids(
                            releaser.mapGrid.row, releaser.mapGrid.column,
                            skill.GetReleaseRadius(releaser.mapGrid), 0, true,
                            analysisTempGrids,
                            delegate (GridUnit grid)
                            {
                                return grid.Distance(target.mapGrid) <= skill.effectRadius;
                            }
                        );
                        //如果有可以使用技能的位置
                        if (analysisTempGrids.Count > 0)
                        {
                            targetGridUnit = analysisTempGrids[Random.Range(0, analysisTempGrids.Count)];
                            //随机一个位置作为释放目标点
                            effectAnalysis = BattleCalculator.Instance.AnalyseBattleSkillEffect(
                                skill,
                                releaser,
                                null,
                                targetGridUnit);

                            analysisTempGrids.Clear();
                        }
                    }
                    break;
            }

            //没有分析结果或者技能释放也不会有效果
            if (effectAnalysis == null
                || (effectAnalysis.mainReceiver.Count == 0 && effectAnalysis.minorReceiver.Count == 0))
                return null;

            //计算释放这个技能的得分
            BattleSkillAutoReleaseAnalysisItem analysisItem = BattleSkillAutoReleaseAnalysisItem.CreateInstance();
            analysisItem.score = 0f;
            analysisItem.targetBattleUnit = target;
            analysisItem.targetGridUnit = targetGridUnit;
            analysisItem.battleSkill = skill;

            //主要影响
            for (int i = 0; i < effectAnalysis.mainReceiver.Count; ++i)
            {
                analysisItem.score += CalculateSkillScore(releaser, effectAnalysis.mainReceiver[i], skill, behaviourType);
            }

            //次要影响
            for (int i = 0; i < effectAnalysis.minorReceiver.Count; ++i)
            {
                analysisItem.score += CalculateSkillScore(releaser, effectAnalysis.minorReceiver[i], skill, behaviourType);
            }

            analysisItem.score = analysisItem.score / skill.energyCost;

            return analysisItem;
        }

        //得出决策
        private BattleDecision MakeDecision()
        {
            var skills = baseData.hostBattleUnit.battleUnitAttribute.battleSkills;
            if (skills.Length == 0)
                return null;

            //遍历每一个行为
            //尝试可释放的每一个技能
            //计算释放这个技能的得分
            //找到最高回报的技能和释放目标
            BattleUnit target = null;
            List<GridUnit> movePath = new List<GridUnit>();
            int currentPoints = 0;  //当前得分
            int highestPoints = 0;  //最高分
            BattleDecision decision = null;
            //遍历行为列表
            for (int i = 0; i < behaviourItems.Count; ++i)
            {
                //这个行为的目标对象
                target = behaviourItems[i].targetBattleUnit;
                //遍历技能列表
                for (int j = 0; j < skills.Length; j++)
                {
                    switch (behaviourItems[i].behaviourType)
                    {
                        //如果是伤害或嘲讽行为，则直接忽略治疗技能
                        case BattleBehaviourType.Damage:
                        case BattleBehaviourType.Provoke:
                            {
                                if (skills[j].damageType == BattleSkillDamageType.Heal)
                                    continue;
                            }
                            break;
                        
                        //如果是治疗行为，则直接忽略伤害技能
                        case BattleBehaviourType.Recovery:
                            if (skills[j].damageType != BattleSkillDamageType.Heal)
                                continue;
                            break;

                        default:
                            break;
                    }

                    movePath.Clear();
                    //判断当前目标是否在范围内
                    if (target.mapGrid.Distance(baseData.hostBattleUnit.mapGrid) > skills[j].GetMaxReleaseRadiusForCalculate(baseData.hostBattleUnit.mapGrid))
                    {
                        //不在范围内
                        //判断是否可达
                        bool canCatch = MapNavigator.Instance.Navigate(
                            baseData.battleField.battleMap,
                            baseData.hostBattleUnit.mapGrid,
                            target.mapGrid,
                            movePath,
                            null,
                            baseData.hostBattleUnit.battleUnitAttribute.mobility,
                            skills[j].GetMaxReleaseRadiusForCalculate(null)
                            );

                        //别考虑这个技能了～
                        if (!canCatch)
                            continue;
                    }

                    //到此表示可以释放这个技能是有戏的呢
                    currentPoints = 10;

                    //假定一下移动后的位置
                    GridUnit virtualPosition = movePath.Count > 0 ? movePath[movePath.Count - 1] : baseData.hostBattleUnit.mapGrid;
                    if (target.mapGrid.Distance(virtualPosition) > skills[j].GetMaxReleaseRadiusForCalculate(virtualPosition))
                    {
                        //移动后仍然不在范围内
                        if(currentPoints > highestPoints)
                        {
                            if (decision == null)
                                decision = BattleDecision.CreateInstance();

                            decision.Reset();
                            decision.targetBattleUnit = target;
                            decision.movePath = movePath.Count > 0 ? movePath.ToArray() : null;
                            //目标不在范围，因此无需保存技能
                            decision.battleSkill = null;
                            decision.skillTargetGrid = null;
                            decision.skillTargetBattleUnit = null;


                            //记录最高分
                            highestPoints = currentPoints;
                        }
                    }
                    else
                    {
                        //移动后在范围内
                        //计算释放技能的得分
                        var analysisItem = CalculateSkillScore(
                            baseData.hostBattleUnit, 
                            virtualPosition,
                            target, 
                            skills[j], 
                            behaviourItems[i].behaviourType);

                        if (analysisItem == null)
                            continue;

                        currentPoints += Mathf.CeilToInt(analysisItem.score);

                        if (currentPoints > highestPoints)
                        {
                            if (decision == null)
                                decision = BattleDecision.CreateInstance();

                            decision.Reset();
                            decision.targetBattleUnit = target;
                            decision.movePath = movePath.Count > 0 ? movePath.ToArray() : null;
                            decision.battleSkill = skills[j];
                            decision.skillTargetGrid = analysisItem.targetGridUnit;
                            decision.skillTargetBattleUnit = analysisItem.targetBattleUnit;

#if UNITY_EDITOR
                            if (DebugHelper.Instance.debugBBSys)
                            {
                                Debug.Log(string.Format("Decision:\nTarget:{0}\nSkill:{1}\nSTargetUnit:{2}\nSGridUnit:{3}\n",
                                decision.targetBattleUnit.battleUnitAttribute.battleUnitName,
                                decision.battleSkill.skillName,
                                decision.skillTargetBattleUnit == null ? "None" : decision.skillTargetBattleUnit.battleUnitAttribute.battleUnitName,
                                decision.skillTargetGrid == null ? "None" : decision.skillTargetGrid.ToString()
                                ));
                            }
#endif

                            //记录最高分
                            highestPoints = currentPoints;
                        }
                    }
                }
                //有一个可行决策
                if (decision != null)
                    break;
            }

            return decision;
        }

        //记录技能释放后的结果
        public void RecordSkillResult(BattleUnit from, BattleUnitSkillResult battleUnitSkillResult)
        {
            if (provokeChip != null && provokeChip.NeedRecordSkillResult)
                provokeChip.RecordSkillResult(from, battleUnitSkillResult);

            if (damageChip != null && damageChip.NeedRecordSkillResult)
                damageChip.RecordSkillResult(from, battleUnitSkillResult);

            if (recoveryChip != null && recoveryChip.NeedRecordSkillResult)
                recoveryChip.RecordSkillResult(from, battleUnitSkillResult);

            if (rageChip != null && rageChip.NeedRecordSkillResult)
                rageChip.RecordSkillResult(from, battleUnitSkillResult);
        }

        //重置系统
        public void ResetSystem()
        {
            if (provokeChip != null)
                provokeChip.ResetChip();

            if (damageChip != null)
                damageChip.ResetChip();

            if (recoveryChip != null)
                recoveryChip.ResetChip();

            if (rageChip != null)
                rageChip.ResetChip();
        }
    }
}