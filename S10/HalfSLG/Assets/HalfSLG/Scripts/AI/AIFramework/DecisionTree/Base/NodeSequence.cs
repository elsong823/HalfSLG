using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{
    [Serializable]
    public class NodeSequence : BaseNode
    {
        public override bool Do(Brain brain)
        {
            bool ret = true;
            foreach (var node in children)
            {
                if (!node.Do(brain))
                {
                    ret = false;
                    break;
                }
            }

            return ret;
        }
    }
}
