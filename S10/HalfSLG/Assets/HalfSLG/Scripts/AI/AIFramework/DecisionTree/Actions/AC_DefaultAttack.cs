using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{
    public class AC_DefaultAttack : ActionCreatorBase
    {
        public override BattleAction TryCreateAction(Brain brain) 
        {
            BattleUnit battleUnit = brain.GetSelf();
            BattleAction battleAction = BattleUnitAction.Create(battleUnit);

            // 填充battleAction

            return battleAction;
        }
    }
}
