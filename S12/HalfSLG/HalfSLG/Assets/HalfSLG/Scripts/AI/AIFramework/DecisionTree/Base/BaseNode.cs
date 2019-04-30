using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

namespace ELGame.AI
{

    public abstract class BaseNode
    {
        protected BaseNode[] children;

        protected DecisionTree tree;

        public abstract bool Do(Brain brain);

        public void DumpNode(StringBuilder stringBuilder, int level = 0)
        {
            if(stringBuilder == null)
            {
                return;
            }

            for(int i = 0; i < level; ++ i)
            {
                stringBuilder.Append('-');
            }

            stringBuilder.Append(ToString());
            stringBuilder.AppendLine();

            if(null == children)
            {
                return;
            }

            foreach(var node in children)
            {
                node.DumpNode(stringBuilder, level + 1);
            }
        }

        public override string ToString()
        {
            return GetType().Name;
        }

#if UNITY_EDITOR

        public void AddChild(BaseNode node)
        {
            if(children == null)
            {
                children = new BaseNode[1];
                children[0] = node;
            }
            else
            {
                Array.Resize(ref children,children.Length + 1);
                children[children.Length - 1] = node;
            }
        }

#endif

    }

}
