using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ELGame.AI
{
    [CreateAssetMenu(menuName = "ScriptableObject/AI/DecisionTree")]
    public class DecisionTree : ScriptableObject
    {
        public BaseNode root;

        public void MakeDecision(Brain brain)
        {
            bool ret = root.Do(brain);
            Debug.Log("tree ret = " + ret);
        }


        public string DumpTree()
        {
            StringBuilder sb = new StringBuilder();
            root.DumpNode(sb,0);
            return sb.ToString();
        }

        [ContextMenu("LogInfo")]
        public void LogInfo()
        {
            Debug.Log(DumpTree());
        }


    }

}