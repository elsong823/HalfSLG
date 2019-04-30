using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{
    public abstract class ConditionDescriptorBase
    {
        [SerializeField]
        protected CustomParamSet customParamSet;


        public abstract bool JudgeCondition(Brain brain);

    }
}
