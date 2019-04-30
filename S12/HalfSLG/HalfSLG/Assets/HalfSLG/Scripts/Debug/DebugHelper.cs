using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class DebugHelper 
        : MonoBehaviourSingleton<DebugHelper>
    {
        private Vector3[] corners = new Vector3[4];
        //是否显示raycast边框
        [SerializeField] private bool showRaycastOutline = false;
        [Header("是否逐步播放")] public bool playBattleByStep = false;
        [Header("开启BBSysLog")] public bool debugBBSys = false;
        [Header("随机先手")] public bool randomFirstAction = true;

#if UNITY_EDITOR
        private void DrawRaycastOutline()
        {
            //设置一个颜色
            Gizmos.color = Color.cyan;

            var objs = GameObject.FindObjectsOfType<UnityEngine.UI.Graphic>();
            foreach (var obj in objs)
            {
                if (!obj.raycastTarget)
                    continue;

                RectTransform rt = obj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.GetWorldCorners(corners);
                    for (int i = 0; i < 4; ++i)
                    {
                        Gizmos.DrawLine(corners[i], corners[i + 1 >= 4 ? 0 : i + 1]);
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (showRaycastOutline)
                DrawRaycastOutline();
        }

#endif
    }
}