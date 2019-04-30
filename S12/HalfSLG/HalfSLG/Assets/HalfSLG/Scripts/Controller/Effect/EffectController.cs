using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public enum EffectPlayType
    {
        WorldPosition,  //指定一个世界位置然后播放
        Holder,         //用于绑定某个对象
    }

    public class EffectController
        : BaseBehaviour, IELPoolObject
    {
        public string effectName = string.Empty;

        [Range(-1f, 10f)] public float effectLength = -1;
        private WaitForSeconds waitForLength = null;

        [SerializeField] protected SortingOrderHelper sortingOrderHelper;

        [HideInInspector] public EffectPlayType playType = EffectPlayType.WorldPosition;

        [HideInInspector] public EffectHolder effectHolder;

        private Coroutine removeTimer = null;

        public virtual int SortingLayer
        {
            get
            {
                return -1;
            }
            set
            {
                if (sortingOrderHelper != null)
                    sortingOrderHelper.RefreshOrder(value, SortingOrder);
            }
        }

        public virtual int SortingOrder
        {
            get
            {
                return -1;
            }
            set
            {
                if (sortingOrderHelper != null)
                    sortingOrderHelper.RefreshOrder(SortingLayer, value);
            }
        }

        //归还特效
        public void Return()
        {
            if (effectHolder != null)
            {
                //避免循环调用
                var holder = effectHolder;
                effectHolder = null;
                holder.RemoveEffect(this);
            }

            //被定时器归还
            if (removeTimer != null)
            {
                StopCoroutine(removeTimer);
                removeTimer = null;
            }

            //归还给管理器
            if (EffectManager.Instance != null)
                EffectManager.Instance.ReturnEffect(this);
        }

        //是否激活状态
        public virtual bool PoolObjActive
        {
            get
            {
                return gameObject.activeSelf;
            }
            set
            {
                gameObject.SetActive(value);
            }
        }

        private WaitForSeconds WaitForLength
        {
            get
            {
                if (waitForLength == null)
                    waitForLength = new WaitForSeconds(effectLength);

                return waitForLength;
            }
        }

        //播放特效
        public virtual void Play()
        {
            //为自动移除的特效
            if (effectLength > 0)
                removeTimer = StartCoroutine(AutoReturn());
        }

        //定时移除
        IEnumerator AutoReturn()
        {
            yield return WaitForLength;
            removeTimer = null;
            Return();
        }

        private void Reset()
        {
            //将名字设置为prefab名字
            effectName = gameObject.name;
        }
    }
}