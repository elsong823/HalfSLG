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
    }

    [CreateAssetMenu(menuName = "ScriptableObject/Battle skill")]
    public class SO_BattleSkill 
        : ScriptableObject
    {
        public int skillID;                //技能id
        public string skillName;           //技能名字
        public int releaseRadius;          //技能释放半径
        public int rangeRadius;            //技能影响半径
        public BattleSkillDamageType damageType;    //伤害类型
        public BattleSkillTargetType targetType;    //目标类型
        public int mainValue;              //造成的伤害
    }
}