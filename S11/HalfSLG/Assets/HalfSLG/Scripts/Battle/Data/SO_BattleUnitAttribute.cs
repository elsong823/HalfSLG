using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ELGame
{
#if UNITY_EDITOR
    [CustomEditor(typeof(SO_BattleUnitAttribute))]
    [CanEditMultipleObjects]
    public class SO_BattleUnitAttributeCustomEditor
        :Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Reset name."))
            {
                SO_BattleUnitAttribute instance = (SO_BattleUnitAttribute)target;
                instance.battleUnitName = instance.name;
            }
        }
    }
#endif

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
        public int stopDistance = 1;

        public int baseAtk;
        public int atkRandRange;
        private int atk;

        public int baseDef;
        public int defRandRange;
        private int def;

        public int Atk
        {
            get
            {
                if (hostBattleUnit.mapGrid.gridUnitBuff != null && hostBattleUnit.mapGrid.gridUnitBuff.buffType == GridUnitBuffType.Atk)
                    return atk + hostBattleUnit.mapGrid.gridUnitBuff.addition;

                return atk;
            }
        }

        public int Def
        {
            get
            {
                if (hostBattleUnit.mapGrid.gridUnitBuff != null && hostBattleUnit.mapGrid.gridUnitBuff.buffType == GridUnitBuffType.Def)
                    return def + hostBattleUnit.mapGrid.gridUnitBuff.addition;

                return def;
            }
        }
        
        public SO_BattleSkill[] battleSkills;

        public BattleUnit hostBattleUnit;
        public BattleBehaviourSystem.BattleBehaviourSystem battleBehaviourSystem;

        public void Reset()
        {
            hp = maxHp;
            energy = 0;
        }

        public void RandomAttributes()
        {
            atk = baseAtk + Random.Range(0, atkRandRange);
            def = baseDef + Random.Range(0, defRandRange);
        }
    }
}