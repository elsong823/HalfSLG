using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BattleCalculator
        :NormalSingleton<BattleCalculator>, IGameBase
    {

        public string Desc()
        {
            return string.Empty;
        }

        public void Init(params object[] args)
        {
            UtilityHelper.Log("Battle calculator inited.");
        }

        public BattleHeroSkillResult CalcSingle(BattleUnit from, BattleUnit to, int skillID)
        {
            BattleHeroSkillResult result = new BattleHeroSkillResult();
            result.battleUnit = to;
            result.syncAttribute = new BattleHeroSyncAttribute();
            //生命变化量暂定为攻击值
            result.syncAttribute.hpChanged = -from.atk;
            //hp变化
            to.hp += result.syncAttribute.hpChanged;
            to.hp = Mathf.Clamp(to.hp, 0, to.maxHp);
            //记录变化
            result.syncAttribute.currentHP = to.hp;
            return result;
        }
    }
}