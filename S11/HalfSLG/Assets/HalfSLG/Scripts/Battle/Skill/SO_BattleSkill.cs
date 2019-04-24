using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public enum BattleSkillDamageType
    {
        Physical,   //物理伤害
        Magic,      //魔法伤害
        Heal,       //恢复类
    }

    public enum BattleSkillTargetType
    {
        BattleUnit, //对某一个战斗单位
        GridUnit,   //对某一个地图格子(范围)
        Self,       //以自身为中心的
    }

    [CreateAssetMenu(menuName = "ScriptableObject/Battle skill")]
    public class SO_BattleSkill
        : ScriptableObject
    {
        public int skillID;                //技能id
        public string skillName;           //技能名字
        [SerializeField] private int releaseRadius;          //技能释放半径
        public int effectRadius;            //技能影响半径idu
        public BattleSkillDamageType damageType;    //伤害类型
        public BattleSkillTargetType targetType;    //目标类型
        public int mainValue;              //造成的伤害
        public int energyCost = 10;        //体力扣减
        public float hatredMultiple = 1f;   //仇恨倍数
        public float rageLevel = 0f;        //愤怒增加

        public int GetReleaseRadius(GridUnit gridUnit)
        {
            int addition = 0;
            if (gridUnit != null && gridUnit.gridUnitBuff != null && gridUnit.gridUnitBuff.buffType == GridUnitBuffType.Range)
                addition = gridUnit.gridUnitBuff.addition;

            return releaseRadius + addition;
        }

        //用于计算的释放半径(最大，考虑效果范围)
        public int GetMaxReleaseRadiusForCalculate(GridUnit gridUnit)
        {
            int addition = 0;
            if (gridUnit != null && gridUnit.gridUnitBuff != null && gridUnit.gridUnitBuff.buffType == GridUnitBuffType.Range)
                addition = gridUnit.gridUnitBuff.addition;

            switch (targetType)
            {
                case BattleSkillTargetType.BattleUnit:
                    return releaseRadius + addition;
                case BattleSkillTargetType.GridUnit:
                    return releaseRadius + effectRadius + addition;
                case BattleSkillTargetType.Self:
                    return effectRadius + addition;
                default:
                    return 0;
            }
        }
    }
}