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

        public override bool Equals(object other)
        {
            if (other != null && other is BaseBehaviour)
                return ((BaseBehaviour)other).GetInstanceID() == this.GetInstanceID();

            return false;
        }
    }
}
