
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class Main
        : MonoBehaviourSingleton<Main>
    {
        private void Start()
        {
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

            UtilityHelper.Log("Main start");
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
        }
    }
}