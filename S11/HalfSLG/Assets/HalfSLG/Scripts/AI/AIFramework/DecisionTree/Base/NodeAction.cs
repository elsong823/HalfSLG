using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{
    [Serializable]
    public class NodeAction : BaseNode
    {
        public ActionCreatorBase actionCreator;

        public override bool Do(Brain brain)
        {
            BattleFieldEvent battleAction = actionCreator.TryCreateAction(brain);
            if(battleAction != null)
            {
                brain.pendingBattleActions.Enqueue(battleAction);
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return actionCreator.GetType().Name;
        }

    }
}
