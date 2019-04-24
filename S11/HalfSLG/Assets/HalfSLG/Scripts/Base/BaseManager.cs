using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{

    public abstract class BaseManager<T>
        : MonoBehaviourSingleton<T>
        where T : BaseManager<T>
    {
        [SerializeField] public bool DebugMode = false;

        public virtual string MgrName => "BaseManager";

        public virtual void InitManager()
        {
            UtilityHelper.Log(string.Format("--> {0} <-- inited.", MgrName));
        }

        public void MgrLog(string info)
        {
            if (DebugMode)
                Debug.LogFormat("{0} :::: {1}",
                    MgrName,
                    info);
        }

        public virtual void ResetManager() { }
    }
}