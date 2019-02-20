using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class EffectManager
        :BaseManager<EffectManager>
    {
        [SerializeField] Transform worldRoot;
        [SerializeField] Dictionary<string, ELStack<EffectController>> effectDic = new Dictionary<string, ELStack<EffectController>>();
        
        protected override void InitManager()
        {
            if (worldRoot == null)
            {
                UtilityHelper.LogError(" Init effect manager failed.");
                return;
            }

            base.InitManager();

            //TEMP
            string[] effectList = new string[2] {
                EGameConstL.Effect_HexFrame,
                EGameConstL.Effect_DamageLabel
            };

            //建立字典
            for (int i = 0; i < effectList.Length; i++)
            {
                effectDic.Add(effectList[i].ToLower(), new ELStack<EffectController>(2, effectList[i], CreateEffect));
            }
            UtilityHelper.Log("Effect manager inited.");
        }

        private EffectController CreateEffect(string effectName)
        {
            //创建特效
            //TODO:RES
            EffectController effect = null;
#if UNITY_EDITOR
            string assetPath = "Assets/HalfSLG/Prefabs/Effect";
            effect = UnityEditor.AssetDatabase.LoadAssetAtPath<EffectController>(string.Format("{0}/{1}.prefab", assetPath, effectName));
            if (effect == null)
            {
                UtilityHelper.LogError(string.Format("Create effect {0} failed.", effectName));
                return null;
            }
#endif
            effect = Instantiate<EffectController>(effect);
            effect.transform.SetParent(worldRoot);
            effect.transform.SetUnused(false, effectName);
            effect.Init();

            return effect;
        }

        public void ReturnEffect(EffectController effect)
        {
            if (effect != null)
            {
                effect.transform.SetParent(worldRoot);
                effect.transform.SetUnused(false, effect.effectName);
                effectDic[effect.effectName.ToLower()].Return(effect);
            }
        }

        //创建一个特效
        public T CreateEffectByName<T>(string effectName, EffectPlayType playType)
            where T : EffectController
        {
            effectName = effectName.ToLower();
            if (!effectDic.ContainsKey(effectName))
            {
                UtilityHelper.LogError(string.Format("Create effect by name error! -> {0}", effectName));
                return null;
            }
            EffectController effect = effectDic[effectName].Get();
            if (effect != null && effect is T)
            {
                if (effect is T)
                {
                    //成功创建了特效
                    effect.playType = playType;
#if UNITY_EDITOR
                    effect.name = effect.effectName;
#endif
                    return (T)effect;
                }
                else
                {
                    UtilityHelper.LogError(string.Format("Create effect by name error! Type error -> {0},{1}", effectName, effect.GetType().ToString()));
                    ReturnEffect(effect);
                }
            }
            return null;
        }

        //快速创建一个在世界位置播放的定时回收特效
        public EffectController CreateWorldPositionEffect(string effectName, Vector3 worldPosition)
        {
            EffectController effect = CreateEffectByName<EffectController>(effectName, EffectPlayType.WorldPosition);
            if (effect != null)
            {
                effect.transform.position = worldPosition;
                effect.Play();
            }
            return effect;
        }
    }
}