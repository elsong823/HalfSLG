using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{

    [CreateAssetMenu(menuName = "ScriptableObject/AI/BattleStrategy")]
    public class BattleStrategy :ScriptableObject
    {
        [SerializeField]
        StateConfigEntry[] stateConfigs;

        public void InitWithBrain(Brain brain)
        {
            // 对brain对数据 添加监听
            // 需要解决对问题： 先创建strategy 后在brain中添加relation 这些事件如何绑定
        }

    }




    [Serializable]
    struct StateConfigEntry
    {
        public string name;
        public BattleState fromState;
        [SerializeField]
        TransitionEntry[] transitions;
    }

    [Serializable]
    struct TransitionEntry
    {
        public string trigger;
        public BattleState toState;
    }



}

