using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    [System.Serializable]
    public class EffectHolder
    {
        [SerializeField] private Transform effectNode;
        private List<EffectController> effectList = new List<EffectController>();

        //添加特效
        public void AddEffect(EffectController effect)
        {
            if (effectNode == null)
            {
                UtilityHelper.LogError("Add effect failed. Node is null.");
                return;
            }

            if (effect != null)
            {
                effect.transform.SetParent(effectNode);
                effect.transform.Normalize();
                effect.gameObject.SetActive(true);
                effect.effectHolder = this;
                effectList.Add(effect);
            }
        }

        //移除特效
        public void RemoveEffect(EffectController effect)
        {
            if (effect == null)
                return;
            
            //不做判断直接从列表中移除
            effectList.Remove(effect);

            //避免循环调用
            if (effect.effectHolder != null && effect.effectHolder.Equals(this))
                effect.Return();
        }

        //移除所有特效
        public void RemoveAllEffects()
        {
            for (int i = 0; i < effectList.Count; ++i)
            {
                if (effectList[i].effectHolder.Equals(this))
                {
                    effectList[i].effectHolder = null;
                    effectList[i].Return();
                }
            }
            effectList.Clear();
        }
    }
}