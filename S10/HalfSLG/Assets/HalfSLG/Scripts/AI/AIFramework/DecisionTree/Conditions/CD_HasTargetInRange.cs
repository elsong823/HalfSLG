using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{
    public class CD_HasTargetInRange : ConditionDescriptorBase
    {
        public override bool JudgeCondition(Brain brain) 
        {
            // 判断在攻击范围内是否有目标
            return false;
        }
    }
}
