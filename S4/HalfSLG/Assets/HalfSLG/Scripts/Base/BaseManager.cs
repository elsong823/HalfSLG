using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BaseManager<T>
        : MonoBehaviourSingleton<T>
        where T : BaseManager<T>
    {
        public void Start()
        {
            InitManager();
        }

        protected virtual void InitManager()
        {

        }
    }
}