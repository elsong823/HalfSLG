using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    [CreateAssetMenu(menuName = "ScriptableObject/Battle unit attributes")]
    public class SO_BattleUnitAttribute 
        : ScriptableObject
    {
        public bool manualOperation;      //手动操作
        public string battleUnitName;
        public int hp;
        public int maxHp;
        public int energy;
        public int maxEnergy;
        public int mobility;
        public int atk;
        public SO_BattleSkill[] battleSkills;
    }
}