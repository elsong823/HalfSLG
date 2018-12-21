using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class EffectManager
        :BaseManager<EffectManager>
    {
        [SerializeField] EffectController effectUnitModel;
        [SerializeField] Transform poolNode;
        [SerializeField] ELStack<EffectController> pool;
        
        protected override void InitManager()
        {
            if (effectUnitModel == null || poolNode == null)
            {
                UtilityHelper.LogError(" Init effect manager failed.");
                return;
            }

            base.InitManager();

            pool = new ELStack<EffectController>(2, CreateEffect);
            UtilityHelper.Log("Effect manager inited.");
        }

        private EffectController CreateEffect()
        {
            EffectController effect = Instantiate<EffectController>(effectUnitModel);
            effect.transform.SetParent(poolNode);
            effect.transform.SetUnused(false);

            return effect;
        }

        public void ReturnEffect(EffectController effect)
        {
            if (effect != null)
            {
                effect.transform.SetParent(poolNode);
                effect.transform.SetUnused(false);
                pool.Return(effect);
            }
        }

        public EffectController GetEffectObject(string name)
        {
            return pool.Get();
        }
        
    }
}