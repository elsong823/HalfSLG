using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public abstract class UIViewElement 
        : MonoBehaviour
    {
        protected abstract void UpdateElement();
    }
}