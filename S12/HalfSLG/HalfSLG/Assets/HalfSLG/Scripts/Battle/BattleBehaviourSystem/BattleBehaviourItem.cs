using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame.BattleBehaviourSystem
{
    [System.Serializable]
    public class BattleBehaviourItem
    {
        public BattleUnit targetBattleUnit;
        public BattleBehaviourType behaviourType;
        public float point;

        private BattleBehaviourItem() {}

        public static BattleBehaviourItem CreateInstance(BattleUnit target, BattleBehaviourType behaviourType)
        {
            if (target == null)
                return null;

            BattleBehaviourItem item = new BattleBehaviourItem();
            item.targetBattleUnit = target;
            item.behaviourType = behaviourType;
            item.point = 0f;

            return item;
        }
    }
}