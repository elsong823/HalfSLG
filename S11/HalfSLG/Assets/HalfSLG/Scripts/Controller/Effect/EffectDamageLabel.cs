using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ELGame
{
    
    public class EffectDamageLabel
        : EffectController
    {
        [SerializeField] private Animator animator;

        [SerializeField] private TextMeshPro tmpDamage;

        public void SetDamage(int value, BattleSkillDamageType damageType)
        {
            //设置数字
            tmpDamage.text = value.ToString();

            //目前设置为 播放普通伤害动画
            switch (damageType)
            {
                case BattleSkillDamageType.Physical:
                    animator.SetTrigger(EGameConstL.HashACKey_NormalDamage);
                    break;
                case BattleSkillDamageType.Magic:
                    animator.SetTrigger(EGameConstL.HashACKey_NormalMagicDamage);
                    break;
                case BattleSkillDamageType.Heal:
                    animator.SetTrigger(EGameConstL.HashACKey_NormalHeal);
                    break;
                default:
                    break;
            }

            //根据动画长度开启自动回收
            Play();
        }

        public override int SortingLayer
        {
            get
            {
                return tmpDamage.sortingLayerID;
            }

            set
            {
                if(tmpDamage.sortingLayerID != value)
                    tmpDamage.sortingLayerID = value;
            }
        }

        public override int SortingOrder
        {
            get
            {
                return tmpDamage.sortingOrder;
            }

            set
            {
                if(tmpDamage.sortingOrder != value)
                    tmpDamage.sortingOrder = value;
            }
        }
    }
}