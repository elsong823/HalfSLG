using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{
    [CreateAssetMenu(menuName = "ScriptableObject/AI/BattleState")]
    public class BattleState: ScriptableObject
    {

        #region 默认状态
        [NonSerialized]
        static BattleState defaultState;
        public static BattleState Default
        {
            get
            {
                if (null == defaultState)
                {
                    defaultState = new BattleState();
                }
                return defaultState;
            }
        }

        #endregion 默认状态

        [SerializeField]
        DecisionTree decisionTree;

        public void OnUpdate(Brain brain)
        {
            decisionTree.MakeDecision(brain);
        }

        public void OnEnter(Brain brain)
        {}

        public void OnExit(Brain brain)
        {}

    }


}
