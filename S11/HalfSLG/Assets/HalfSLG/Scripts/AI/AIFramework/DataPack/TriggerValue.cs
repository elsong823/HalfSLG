using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{

    public class TriggerValue<T> where T : IEquatable<T>
    {
        public event Action onValueChanged;

        T mValue;

        public T Value
        {
            get { return mValue; }
            set {
                if(!mValue.Equals(value))
                {
                    mValue = value;

                }
            }
        }

    }

}
