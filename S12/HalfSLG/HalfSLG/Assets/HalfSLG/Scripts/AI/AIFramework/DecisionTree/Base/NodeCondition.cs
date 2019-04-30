using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{
    [Serializable]
    public class NodeCondition : BaseNode
    {
        public ConditionDescriptorBase conditionDescriptor;
        
        public override bool Do(Brain brain)
        {
            return conditionDescriptor.JudgeCondition(brain);
        }

        public override string ToString()
        {
            return conditionDescriptor.GetType().Name;
        }
    }
}
