using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ELGame.AI
{
    public class Transition
    {
        public BattleState fromState = BattleState.Default;
        public BattleState toState = BattleState.Default;

        public Transition(BattleState fs, BattleState ts)
        {
            if(fs == ts)
            {
                Debug.LogError(""); 
            }
            fromState = fs;
            toState = ts;
        }

        public static bool IsValid(Transition transition)
        {
            if(null == transition)
            {   
                return false; 
            }
            if (transition.fromState == null)
            {
                return false; 
            }
            if (transition.toState == null)
            {
                return false;
            }
            if(transition.fromState == transition.toState)
            {
                return false;
            }
            return true;
        }

        public void DoTransition(Brain brain)
        {
            if (fromState != brain.currentState)
            {
                return;
            }
            fromState.OnExit(brain);
            brain.currentState = toState;
            toState.OnEnter(brain);
        }
    }
}
