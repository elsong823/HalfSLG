using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace ELGame.AI
{
    public class DecisionTreeCreator
    {

        //[MenuItem("GameObject/AI/CreateTreeAsset", false, 0)]
        [MenuItem("Assets/AI/CreateTreeAsset", false, 0)]
        public static void CreateTreeAsset()
        {
            GameObject go = Selection.activeGameObject;
            if (null == go)
            {
                return;
            }
            if (!go.name.StartsWith("DT_"))
            {
                Debug.LogError("root name error");
                return;
            }
            if(go.transform.childCount != 1)
            {
                Debug.LogError("no root or more than one root");
                return;
            }
            Transform root = go.transform.GetChild(0);

            DecisionTree decisionTree = ScriptableObject.CreateInstance<DecisionTree>();
            decisionTree.root = GameObjectToNode(root.gameObject);

            CreateNodeForTrans(root, decisionTree.root);

            Debug.Log("生成完毕\n" + decisionTree.DumpTree());

            string assetPath = string.Format("Assets/HalfSLG/Scripts/AI/Configs/DecisionTree/@{0}.asset", go.name);
            AssetDatabase.CreateAsset(decisionTree, assetPath);

            AssetDatabase.Refresh();
        }


        static void CreateNodeForTrans(Transform parentTrans, BaseNode parentNode)
        {
            foreach(Transform t in parentTrans)
            {
                var node = GameObjectToNode(t.gameObject);
                if(null != node)
                {
                    parentNode.AddChild(node);
                    CreateNodeForTrans(t,node);
                }
            }
        }

        static BaseNode GameObjectToNode(GameObject go)
        {
            string name = go.name;
            string className = "ELGame.AI." + go.name;
            Assembly ass = Assembly.Load("Assembly-CSharp");
            Type t = ass.GetType(className);
            if (null == t)
            {
                Debug.LogErrorFormat("name error [{0}]",name);
                return null;
            }

            System.Object o = Activator.CreateInstance(t);
            // 普通结点
            if (o is BaseNode)
            {
                BaseNode bn = o as BaseNode;
                return bn;
            }
            
            // action
            if (o is ActionCreatorBase)
            {
                ActionCreatorBase ac = o as ActionCreatorBase;
                NodeAction na = new NodeAction();
                na.actionCreator = ac;
                return na;
            }

            // condition
            if (o is ConditionDescriptorBase)
            {
                ConditionDescriptorBase cd = o as ConditionDescriptorBase;
                NodeCondition nc = new NodeCondition();
                nc.conditionDescriptor = cd;
                return nc;
            }

            return null;
        }

    }

}
