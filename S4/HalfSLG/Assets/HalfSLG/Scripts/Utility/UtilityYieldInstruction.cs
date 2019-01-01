using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class WaitForTouchScreen
        : CustomYieldInstruction
    {
        public override bool keepWaiting
        {
            get
            {
#if UNITY_EDITOR
                return !Input.GetKeyDown(KeyCode.Space);
#endif
                return true;
            }
        }
        
    }
}