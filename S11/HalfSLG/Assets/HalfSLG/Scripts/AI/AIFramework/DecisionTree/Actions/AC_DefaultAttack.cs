using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{
    public class AC_DefaultAttack : ActionCreatorBase
    {
        public override BattleFieldEvent TryCreateAction(Brain brain) 
        {
            BattleUnit battleUnit = brain.GetSelf();

            // 填充battleAction

            return null;
        }
    }
}
