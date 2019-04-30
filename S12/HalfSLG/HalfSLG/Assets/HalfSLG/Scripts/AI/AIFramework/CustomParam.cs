using System;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame.AI
{
    [Serializable]
    public struct CustomParam
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class CustomParamSet: ISerializationCallbackReceiver
    {
        [NonSerialized]
        Dictionary<string, string> dict = new Dictionary<string, string>();

        [SerializeField]
        CustomParam[] customParams;

        public string this[string key]
        {
            get
            {
                string value = string.Empty;
                dict.TryGetValue(key, out value);
                return value;
            }
        }

        public void OnAfterDeserialize()
        {
            dict.Clear();
            foreach(var cp in customParams)
            {
                dict.Add(cp.key, cp.value);
            }
        }

        public void OnBeforeSerialize()
        {}
    }

}
