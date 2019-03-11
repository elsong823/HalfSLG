using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ELGame
{
    //技能分析结果
    public class BattleSkillAutoReleaseAnalysisItem
        : IRecyclable
    {
        public SO_BattleSkill battleSkill;
        public BattleUnit targetBattleUnit; //目标战斗单位
        public GridUnit targetGridUnit;     //目标地块单位
        public int score;                   //释放这个技能的得分
        public int effectedCount;           //这个技能可以影响的人数

        public void Recycle()
        {
            battleSkill = null;
            targetBattleUnit = null;
            targetGridUnit = null;
            score = 0;
            effectedCount = 0;
        }
    }

    //自动释放分析器
    public class BattleSkillAutoReleaseAnalysisor
    {
        public BattleUnit releaser = null;
        public BattleUnit target = null;

        private SingletonDyncRecyclableList<BattleSkillAutoReleaseAnalysisItem> skillReleaseAnalysisItem = null;

        //分析用的临时容器
        private List<GridUnit> analysisTempGrids = new List<GridUnit>(5);

        //分析结果单位的初始容量
        public BattleSkillAutoReleaseAnalysisor(int capacity)
        {
            skillReleaseAnalysisItem = new SingletonDyncRecyclableList<BattleSkillAutoReleaseAnalysisItem>(10);
        }

        //分析结果
        public void Ananlysis(BattleUnit releaser, BattleUnit target, SO_BattleSkill[] skills)
        {
            Reset();

            if (releaser == null || target == null || skills == null)
                return;

            this.releaser = releaser;
            this.target = target;

            //有效技能数
            int validSkillCount = 0;
            for (int i = 0; i < skills.Length; ++i)
            {
                //判断能量是否够
                if (skills[i].energyCost > releaser.battleUnitAttribute.energy)
                    continue;

                //技能都不在释放范围内，考虑你妹啊
                if (!WithInSkillReleaseRange(releaser, target, skills[i]))
                    continue;

                //释放这个技能的主要影响目标和次要影响目标
                BattleSkillEffectAnalysis effectAnalysis = null;
                //捎带手保存一下释放技能的地点
                GridUnit targetGridUnit = null;

                switch(skills[i].targetType)
                {
                    //对目标单位
                    case BattleSkillTargetType.BattleUnit:
                        effectAnalysis = BattleCalculator.Instance.AnalyseBattleSkillEffect(
                            skills[i],
                            releaser,
                            target);
                        break;

                    case BattleSkillTargetType.Self:
                        effectAnalysis = BattleCalculator.Instance.AnalyseBattleSkillEffect(
                            skills[i],
                            releaser);
                        break;

                    case BattleSkillTargetType.GridUnit:
                        analysisTempGrids.Clear();
                        //如果目标在范围内
                        if(skills[i].releaseRadius > 0 && releaser.mapGrid.Distance(target.mapGrid) <= skills[i].releaseRadius)
                        {
                            effectAnalysis = BattleCalculator.Instance.AnalyseBattleSkillEffect(
                                skills[i],
                                releaser,
                                null,
                                target.mapGrid);

                            //以地块为目标的技能，保存一下目标地块
                            targetGridUnit = target.mapGrid;
                        }
                        //自身为中心
                        else if(skills[i].releaseRadius <= 0 && releaser.mapGrid.Distance(target.mapGrid) <= skills[i].effectRadius)
                        {
                            effectAnalysis = BattleCalculator.Instance.AnalyseBattleSkillEffect(
                                skills[i],
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
                                skills[i].releaseRadius, 0, true, 
                                analysisTempGrids,
                                delegate (GridUnit grid)
                                {
                                    return grid.Distance(target.mapGrid) <= skills[i].effectRadius;
                                }
                            );
                            //如果有可以使用技能的位置
                            if (analysisTempGrids.Count > 0)
                            {
                                targetGridUnit = analysisTempGrids[Random.Range(0, analysisTempGrids.Count)];
                                //随机一个位置作为释放目标点
                                effectAnalysis = BattleCalculator.Instance.AnalyseBattleSkillEffect(
                                    skills[i],
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
                    continue;
                
                //起码可以释放
                ++validSkillCount;
                //计算释放这个技能的得分
                BattleSkillAutoReleaseAnalysisItem analysisItem = skillReleaseAnalysisItem.Get();
                analysisItem.battleSkill = skills[i];
                analysisItem.targetBattleUnit = target;
                analysisItem.targetGridUnit = targetGridUnit;

                //主要影响
                for (int j = 0; j < effectAnalysis.mainReceiver.Count; ++j)
                {
                    analysisItem.score += analysisItem.battleSkill.mainValue;
                    ++analysisItem.effectedCount;
                }

                //次要影响
                for (int j = 0; j < effectAnalysis.minorReceiver.Count; ++j)
                {
                    analysisItem.score += analysisItem.battleSkill.mainValue;
                    ++analysisItem.effectedCount;
                }
                
                analysisItem.score = Mathf.CeilToInt(1000 * analysisItem.score / analysisItem.battleSkill.energyCost);
            }

            //排序
            skillReleaseAnalysisItem.Sort(LiteSingleton<BattleSkillReleaseAnalysisItemComparer>.Instance);
        }

        //获取最优解
        public BattleSkillAutoReleaseAnalysisItem GetResult()
        {
            return skillReleaseAnalysisItem.GetFirst();
        }

        //重置
        public void Reset()
        {
            releaser = null;
            target = null;
            skillReleaseAnalysisItem.Reset();

            //UtilityHelper.Log("Battle Skill Auto Release Analysisor Reset.", LogColor.BLUE);
        }

        //打印详情
        public string Desc()
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendFormat("Release analysis : {0} -> {1}\n",
                                    releaser == null ? "None" : releaser.ToString(),
                                    target == null ? "None" : target.ToString());

            List<BattleSkillAutoReleaseAnalysisItem> validList = skillReleaseAnalysisItem.GetUsed();
            if (validList == null)
            {
                strBuilder.AppendFormat("Empty......");
            }
            else
            {
                for (int i = 0; i < validList.Count; ++i)
                {
                    if (validList[i].battleSkill != null)
                    {
                        strBuilder.AppendFormat("{0}. {1} : score = {2}, count = {3}\n",
                                                i,
                                                validList[i].battleSkill.skillName,
                                                validList[i].score,
                                                validList[i].effectedCount);
                    }
                }
            }

            return strBuilder.ToString();
        }

        //判断目标是否在某个技能范围内
        private bool WithInSkillReleaseRange(BattleUnit releaser, BattleUnit target, SO_BattleSkill skill)
        {
            if (skill == null || releaser == null || target == null)
                return false;

            return releaser.mapGrid.Distance(target.mapGrid) <= skill.MaxReleaseRadiusForCalculate;
        }
    }

}