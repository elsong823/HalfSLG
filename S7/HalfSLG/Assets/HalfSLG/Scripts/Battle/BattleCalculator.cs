using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    //战斗技能使用的结果
    public class BattleSkillAnalysis
    {
        public bool valid = false;
        public BattleUnit releaser = null;
        public SO_BattleSkill battleSkill = null;
        public List<BattleUnit> suitableUnits = new List<BattleUnit>(10);       //可以被选中的单位
        public List<BattleUnit> teamLimit = new List<BattleUnit>(10);           //队伍不符（对敌或对我方）
        public List<BattleUnit> distanceLimit = new List<BattleUnit>(10);       //距离不符
        public List<BattleUnit> battleUnitInvalid = new List<BattleUnit>(10);   //战斗单位状态异常（无法战斗）

        //分析
        public bool Analysis(BattleUnit battleUnit, SO_BattleSkill skill)
        {
            Reset();

            if (battleUnit == null || skill == null)
                return false;

            releaser = battleUnit;
            battleSkill = skill;

            //队伍不符的
            BattleTeam team = battleUnit.battleField.GetBattleTeam(battleUnit, !(skill.damageType == BattleSkillDamageType.Heal));
            teamLimit.AddRange(team.battleUnits);
            
            //队伍相符的
            team = battleUnit.battleField.GetBattleTeam(battleUnit, skill.damageType == BattleSkillDamageType.Heal);
            for (int i = 0; i < team.battleUnits.Count; ++i)
            {
                //无法行动的
                if (!team.battleUnits[i].CanAction)
                    battleUnitInvalid.Add(team.battleUnits[i]);

                //超出距离的（大于释放距离 + 效果范围）
                else if (team.battleUnits[i].mapGrid.Distance(battleUnit.mapGrid) > skill.releaseRadius + (skill.targetType == BattleSkillTargetType.GridUnit ? skill.rangeRadius : 0))
                    distanceLimit.Add(team.battleUnits[i]);

                //合适的
                else
                    suitableUnits.Add(team.battleUnits[i]);
            }

            valid = true;

            return true;
        }

        //重置
        public void Reset()
        {
            releaser = null;
            battleSkill = null;

            suitableUnits.Clear();
            teamLimit.Clear();
            distanceLimit.Clear();
            battleUnitInvalid.Clear();

            valid = false;
        }
    }

    public class BattleCalculator
        :NormalSingleton<BattleCalculator>, IGameBase
    {
        private BattleSkillAnalysis battleSkillAnalyses = new BattleSkillAnalysis();

        public string Desc()
        {
            return string.Empty;
        }

        public void Init(params object[] args)
        {
            UtilityHelper.Log("Battle calculator inited.");
        }

        public BattleHeroSkillResult CalcSingle(BattleUnit from, BattleUnit to, SO_BattleSkill battleSkill)
        {
            BattleHeroSkillResult result = new BattleHeroSkillResult();
            result.battleUnit = to;
            result.battleSkill = battleSkill;
            result.syncAttribute = new BattleHeroSyncAttribute();
            //简单计算生命值
            switch (battleSkill.damageType)
            {
                case BattleSkillDamageType.Physical:
                case BattleSkillDamageType.Magic:
                    result.syncAttribute.hpChanged = -battleSkill.mainValue;
                    break;
                case BattleSkillDamageType.Heal:
                    result.syncAttribute.hpChanged = Mathf.Min(battleSkill.mainValue, to.maxHp - to.hp);
                    break;
                default:
                    break;
            }
            //hp变化
            to.hp += result.syncAttribute.hpChanged;
            to.hp = Mathf.Clamp(to.hp, 0, to.maxHp);
            //记录变化
            result.syncAttribute.currentHP = to.hp;
            return result;
        }

        /// <summary>
        /// 技能结果推测，用于战斗推算或界面展示
        /// </summary>
        /// <returns></returns>
        public BattleSkillAnalysis AnalyseBattleSkill(BattleUnit battleUnit, SO_BattleSkill battleSkill)
        {
            //分析啦~
            if (battleSkillAnalyses.Analysis(battleUnit, battleSkill))
                return battleSkillAnalyses;

            return null;
        }
    }
}