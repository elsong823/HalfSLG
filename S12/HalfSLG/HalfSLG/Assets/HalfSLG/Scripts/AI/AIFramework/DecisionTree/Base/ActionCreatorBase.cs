using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{
    public abstract class ActionCreatorBase
    {
        [SerializeField]
        protected CustomParamSet customParamSet;

        public abstract BattleFieldEvent TryCreateAction(Brain brain);
    }
}
