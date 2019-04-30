using UnityEngine;

namespace ELGame.Resource
{
    public class PrefabAsset
        :IRecyclable
    {
        private static LitePool<PrefabAsset> pool = new LitePool<PrefabAsset>();

        private string requester;
        public PrefabAssetNode assetNode;
        public GameObject clone;

        public PrefabAsset() { }

        //创建一个新的
        public static PrefabAsset Get(PrefabAssetNode assetNode)
        {
            if (assetNode == null || assetNode.originPrefab == null)
                return null;

            PrefabAsset clone = pool.Get();

            clone.assetNode = assetNode;
            clone.requester = null;
            clone.clone = GameObject.Instantiate<GameObject>(assetNode.originPrefab, assetNode.ParentNode);
            clone.Reset();

            return clone;
        }
        
        //判断激活状态
        public bool IsActive
        {
            get
            {
                return !string.IsNullOrEmpty(requester) || clone.activeSelf;
            }
        }
        
        public void Reset()
        {
            if (clone == null)
            {
                Debug.LogError(string.Format("恭喜你：尝试重置名为 {0} 的asset，失败，此obj为空，推测已被Destroy！", assetNode.assetName));
                requester = null;
                return;
            }

            clone.transform.SetParent(assetNode.ParentNode);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localScale = Vector3.one;
            clone.transform.localRotation = Quaternion.identity;
            clone.name = assetNode.assetName;
            requester = null;
            clone.SetActive(false);

            //WooEngine.LuaHelper.ResLuaBehaviour(m_objClone);
            //WooEngine.EventTriggerListener.ClearAllListener(m_objClone);
        }

        public string Requester
        {
            set
            {
                requester = value;

                if (string.IsNullOrEmpty(requester))
                {
                    Reset();
                    //不知道自己还有没有存在的意义和价值，需要他的父级节点来判断
                    assetNode.OnPrefabAssetReset();
                }
            }
            get
            {
                return requester;
            }
        }
        
        public void OnRecycle()
        {
            requester = string.Empty;
            assetNode = null;
            if (clone != null)
            {
                GameObject.Destroy(clone);
                clone = null;
            }
        }

        public void Unload()
        {
            pool.Return(this);
        }
    }
}