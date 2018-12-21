using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BaseBehaviour
        : MonoBehaviour, IGameBase
    {
        public string Desc()
        {
            return string.Empty;
        }

        public void Init(params object[] args)
        {
        }
    }
}
