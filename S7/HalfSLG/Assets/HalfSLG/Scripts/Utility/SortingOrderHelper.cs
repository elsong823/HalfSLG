//用来控制各个层级的order值


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ELGame
{
#if UNITY_EDITOR
    [CustomEditor(typeof(SortingOrderHelper))]
    public class SortingOrderEditor
        : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(!Application.isPlaying)
            {
                if (GUILayout.Button("刷新"))
                {
                    SortingOrderHelper obj = (SortingOrderHelper)target;
                    obj.orderList = new List<SortingOrderItem>();
                    if (obj != null)
                    {
                        //记录所有层级
                        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                        foreach (var renderer in renderers)
                        {
                            SortingOrderItem item = new SortingOrderItem();
                            item.renderer = renderer;
                            item.value = renderer.sortingOrder;
                            obj.orderList.Add(item);
                        }
                    }
                }
            }
        }
    }
#endif

    [System.Serializable]
    public class SortingOrderItem
    {
        public Renderer renderer;
        public int value;
    }

    public class SortingOrderHelper 
        : BaseBehaviour
    {
        public List<SortingOrderItem> orderList;
        
        public void RefreshOrder(int layerID, int baseOrder)
        {
            if (orderList == null || orderList.Count == 0)
                return;

            for (int i = 0; i < orderList.Count; ++i)
            {
                if (orderList[i].renderer == null)
                    continue;

                if (orderList[i].renderer.sortingLayerID != layerID)
                    orderList[i].renderer.sortingLayerID = layerID;

                if (orderList[i].renderer.sortingOrder != baseOrder + orderList[i].value)
                    orderList[i].renderer.sortingOrder = baseOrder + orderList[i].value;
            }
        }
    }
}