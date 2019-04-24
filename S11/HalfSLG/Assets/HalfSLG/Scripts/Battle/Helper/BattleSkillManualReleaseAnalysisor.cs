//手动释放技能分析器

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BattleSkillManualReleaseAnalysisor
    {
        public BattleUnit releaser = null;
        public SO_BattleSkill battleSkill = null;
        public List<BattleUnit> suitableUnits = new List<BattleUnit>(10);       //可以被选中的单位
        public List<BattleUnit> teamLimit = new List<BattleUnit>(10);           //队伍不符（对敌或对我方）
        public List<BattleUnit> distanceLimit = new List<BattleUnit>(10);       //距离不符
        public List<BattleUnit> battleUnitInvalid = new List<BattleUnit>(10);   //战斗单位状态异常（无法战斗）

        //分析
        public void Analysis(BattleUnit battleUnit, SO_BattleSkill skill)
        {
            Reset();

            if (battleUnit == null || skill == null)
                return;

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

                //超出范围的
                else if (team.battleUnits[i].mapGrid.Distance(battleUnit.mapGrid) > skill.GetMaxReleaseRadiusForCalculate(releaser.mapGrid))
                    distanceLimit.Add(team.battleUnits[i]);

                //范围内的
                else
                    suitableUnits.Add(team.battleUnits[i]);
            }
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

            //UtilityHelper.Log("Battle Skill Manual Release Analysisor Reset.", LogColor.BLUE);
        }
    }
}