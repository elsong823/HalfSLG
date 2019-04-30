using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace ELGame
{
    //技能效果影响
    public class BattleSkillEffectAnalysis
    {
        public SO_BattleSkill battleSkill;
        public List<BattleUnit> mainReceiver = new List<BattleUnit>(5);     //主要影响
        public List<BattleUnit> minorReceiver = new List<BattleUnit>(5);    //次要影响

        public void Reset()
        {
            battleSkill = null;
            mainReceiver.Clear();
            minorReceiver.Clear();

            //UtilityHelper.Log("Battle Skill Effect Analysis Reset.", LogColor.BLUE);
        }
    }

    public class BattleCalculator
        :NormalSingleton<BattleCalculator>, IGameBase
    {
        //手动释放分析器
        private BattleSkillManualReleaseAnalysisor manualReleaseAnalysisor;
        //技能被释放的影响结果
        private BattleSkillEffectAnalysis battleSkillEffectAnalysis;

        public string Desc()
        {
            return string.Empty;
        }

        public void Init(params object[] args)
        {
            manualReleaseAnalysisor = new BattleSkillManualReleaseAnalysisor();
            battleSkillEffectAnalysis = new BattleSkillEffectAnalysis();
            
            BattleManager.Instance.MgrLog("Battle calculator inited.");
        }

        //手动释放技能分析器
        public BattleSkillManualReleaseAnalysisor ManualReleaseAnalysisor
        {
            get
            {
                return manualReleaseAnalysisor;
            }
        }
        
        /// <summary>
        /// 计算单个效果
        /// </summary>
        /// <param name="releaser">释放者</param>
        /// <param name="target">目标</param>
        /// <param name="battleSkill">技能</param>
        /// <param name="mainEffect">是否主要伤害</param>
        /// <returns>技能结果</returns>
        public BattleUnitSkillResult CalcSingle(BattleUnit releaser, BattleUnit target, SO_BattleSkill battleSkill, bool mainEffect)
        {
            BattleUnitSkillResult result = new BattleUnitSkillResult();
            result.battleUnit = target;
            result.battleSkill = battleSkill;
            result.syncAttribute = new BattleUnitSyncAttribute();
            //简单计算生命值
            switch (battleSkill.damageType)
            {
                case BattleSkillDamageType.Physical:
                case BattleSkillDamageType.Magic:
                    result.syncAttribute.hpChanged = -(Mathf.Max(0, releaser.battleUnitAttribute.Atk - target.battleUnitAttribute.Def + battleSkill.mainValue));
                    //能量不变
                    result.syncAttribute.energyChanged = 0;
                    result.syncAttribute.currentEnergy = target.battleUnitAttribute.energy;
                    break;
                case BattleSkillDamageType.Heal:
                    result.syncAttribute.hpChanged = Mathf.Min(battleSkill.mainValue, target.battleUnitAttribute.maxHp - target.battleUnitAttribute.hp);
                    //能量不变
                    result.syncAttribute.energyChanged = 0;
                    result.syncAttribute.currentEnergy = target.battleUnitAttribute.energy;
                    break;
                default:
                    break;
            }
            //hp变化
            target.battleUnitAttribute.hp += result.syncAttribute.hpChanged;
            target.battleUnitAttribute.hp = Mathf.Clamp(target.battleUnitAttribute.hp, 0, target.battleUnitAttribute.maxHp);
            //记录变化
            result.syncAttribute.currentHP = target.battleUnitAttribute.hp;
            return result;
        }

        public BattleSkillEffectAnalysis AnalyseBattleSkillEffect(SO_BattleSkill battleSkill, BattleUnit releaser = null, BattleUnit targetBattleUnit = null, GridUnit targetGridUnit = null)
        {
            battleSkillEffectAnalysis.Reset();

            if (releaser == null)
            {
                UtilityHelper.LogError("Analyse Battle Skill Effect error.Releaser is none.");
                return null;
            }

            switch (battleSkill.targetType)
            {
                //对战斗单位
                case BattleSkillTargetType.BattleUnit:
                    if (targetBattleUnit == null)
                        return null;
                    else
                    {
                        battleSkillEffectAnalysis.battleSkill = battleSkill;
                        //记录主要影响
                        battleSkillEffectAnalysis.mainReceiver.Add(targetBattleUnit);
                        if (battleSkill.effectRadius > 0)
                        {
                            //暂时定为次要目标与主要目标同类
                            BattleTeam battleTeam = targetBattleUnit.battleField.GetBattleTeam(targetBattleUnit, true);
                            if (battleTeam != null)
                            {
                                for (int i = 0; i < battleTeam.battleUnits.Count; ++i)
                                {
                                    if (!battleTeam.battleUnits[i].CanAction || battleTeam.battleUnits[i].Equals(targetBattleUnit))
                                        continue;
                                    //有次要影响
                                    if (battleTeam.battleUnits[i].mapGrid.Distance(targetBattleUnit.mapGrid) <= battleSkill.effectRadius)
                                        battleSkillEffectAnalysis.minorReceiver.Add(battleTeam.battleUnits[i]);
                                }
                            }
                        }
                    }
                    break;

                //对自己
                case BattleSkillTargetType.Self:
                    battleSkillEffectAnalysis.battleSkill = battleSkill;
                    //记录主要影响
                    battleSkillEffectAnalysis.mainReceiver.Add(releaser);
                    if (battleSkill.effectRadius > 0)
                    {
                        //暂时定为次要目标与主要目标同类
                        BattleTeam battleTeam = releaser.battleField.GetBattleTeam(releaser, true);
                        if (battleTeam != null)
                        {
                            for (int i = 0; i < battleTeam.battleUnits.Count; ++i)
                            {
                                if (!battleTeam.battleUnits[i].CanAction || battleTeam.battleUnits[i].Equals(releaser))
                                    continue;
                                //有次要影响
                                if (battleTeam.battleUnits[i].mapGrid.Distance(releaser.mapGrid) <= battleSkill.effectRadius)
                                    battleSkillEffectAnalysis.minorReceiver.Add(battleTeam.battleUnits[i]);
                            }
                        }
                    }
                    break;
                
                //对某个地点
                case BattleSkillTargetType.GridUnit:
                    if (targetGridUnit == null)
                        return null;
                    else
                    {
                        battleSkillEffectAnalysis.battleSkill = battleSkill;
                        if (battleSkill.effectRadius > 0)
                        {
                            BattleTeam battleTeam = releaser.battleField.GetBattleTeam(releaser, battleSkill.damageType == BattleSkillDamageType.Heal);
                            if (battleTeam != null)
                            {
                                for (int i = 0; i < battleTeam.battleUnits.Count; ++i)
                                {
                                    if (!battleTeam.battleUnits[i].CanAction || battleTeam.battleUnits[i].Equals(releaser))
                                        continue;
                                    //记录主要影响
                                    if (battleTeam.battleUnits[i].mapGrid.Distance(targetGridUnit) <= battleSkill.effectRadius)
                                        battleSkillEffectAnalysis.mainReceiver.Add(battleTeam.battleUnits[i]);
                                }
                            }
                        }
                    }
                    break;

                default:
                    break;
            }

            return battleSkillEffectAnalysis;
        }
    }
}