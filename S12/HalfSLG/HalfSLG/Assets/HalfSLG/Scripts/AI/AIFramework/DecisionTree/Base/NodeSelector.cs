using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{
    [Serializable]
    public class NodeSelector : BaseNode
    {
        public override bool Do(Brain brain)
        {
            bool ret = false;
            foreach (var node in children)
            {
                if (node.Do(brain))
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }
    }
}
