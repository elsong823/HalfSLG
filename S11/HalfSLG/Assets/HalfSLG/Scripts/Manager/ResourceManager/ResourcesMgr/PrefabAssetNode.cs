using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ELGame.Resource
{
    public class PrefabAssetNode
        : IRecyclable
    {
        private static LitePool<PrefabAssetNode> nodePool = new LitePool<PrefabAssetNode>();

        private const string POOL_ROOT = "POOL_ROOT";
        private static Transform poolRoot = null;
        private static Transform PoolRoot
        {
            get
            {
                if (poolRoot == null)
                {
                    //如果没有池的根节点，则从GameManager下创建一个
                    GameObject resManagerObject = ResourceManager.Instance.gameObject;
                    if (resManagerObject)
                    {
                        poolRoot = new GameObject(POOL_ROOT).transform;
                        poolRoot.SetParent(resManagerObject.transform);
                        poolRoot.localPosition = Vector3.zero;
                        poolRoot.localScale = Vector3.one;
                        poolRoot.gameObject.SetActive(false);
                    }
                    else
                    {
                        Debug.LogError("Create pool node failed! None Game manager object!");
                        return null;
                    }
                }
                return poolRoot;
            }
        }
        
        private AssetBundleInfoNode infoNode;

        public string assetName;                    //资源名称
        public GameObject originPrefab = null;          //原始资源

        private Transform parentNode = null;         //如果这个资源可以克隆，此为节点
        private List<PrefabAsset> prefabPool = new List<PrefabAsset>();
        private int poolCapacity = 0;                //池子容量

        public PrefabAssetNode() { }

        public static PrefabAssetNode Create(AssetBundleInfoNode infoNode, string assetName)
        {
            if (infoNode == null)
            {
                Debug.LogError("Create AssetInfoNode failed. Empty infoNode...");
                return null;
            }

            GameObject original = infoNode.GetAsset<GameObject>(assetName, string.Format("PrefabAssetNode_{0}", assetName));
            if (original == null)
            {
                Debug.LogError("Create AssetInfoNode failed. Can not get gameobject asset ==> " + assetName);
                return null;
            }

            PrefabAssetNode node = nodePool.Get();
            node.infoNode = infoNode;
            node.assetName = assetName;
            node.originPrefab = original;

            return node;
        }
        
        public Transform ParentNode
        {
            get
            {
                if (parentNode == null)
                {
                    if (!PoolRoot)
                    {
                        Debug.LogError(string.Format("{0} clone failed. None parent node.", assetName));
                        return null;
                    }
                    else
                    {
                        parentNode = PoolRoot.Find(assetName.ToUpper());
                    }
                    if (parentNode == null)
                    {
                        parentNode = new GameObject(assetName.ToUpper()).transform;
                        parentNode.SetParent(PoolRoot);
                        parentNode.localPosition = Vector3.zero;
                        parentNode.localScale = Vector3.one;
                        parentNode.localRotation = Quaternion.identity;
                    }
                }
                return parentNode;
            }
        }
        
        public int PoolCapacity
        {
            set
            {
                poolCapacity = value;
                if (prefabPool == null)
                {
                    prefabPool = new List<PrefabAsset>();
                }

                //如果当前池里的数量已经比容量大
                if (prefabPool.Count > poolCapacity)
                {
                    //计算需要移除的
                    int count = prefabPool.Count - poolCapacity;
                    for (int i = prefabPool.Count - 1; i >= 0; --i)
                    {
                        if (!prefabPool[i].IsActive)
                        {
                            prefabPool[i].Unload();

                            prefabPool.RemoveAt(i);

                            --count;

                            if (count <= 0)
                                break;
                        }
                    }
                }
                else if (prefabPool.Count < poolCapacity)
                {
                    //分配足够多的
                    int count = poolCapacity - prefabPool.Count;
                    while (count > 0)
                    {
                        --count;
                        Clone();
                    }
                }

                //如果已经没有clone了，删掉父节点
                if (prefabPool.Count == 0)
                    Unload();
            }
        }

        //只是克隆一个新的对象放到池中
        private PrefabAsset Clone()
        {
            if(ParentNode == null)
            {
                Debug.LogError(string.Format("{0} clone failed. None parent node.", assetName));
                return null;
            }
            
            PrefabAsset newClone = PrefabAsset.Get(this);

            if (newClone == null)
            {
                Debug.LogError(string.Format("{0} clone failed. None model", assetName));
                return null;
            }
            
            prefabPool.Add(newClone);

            return newClone;
        }

        public PrefabAsset Clone(string requester)
        {
            if (string.IsNullOrEmpty(requester))
            {
                Debug.LogError(string.Format("{0} clone failed. None reqeuster.", assetName));
                return null;
            }

            if (ParentNode == null)
            {
                Debug.LogError(string.Format("{0} clone failed. None parent node.", assetName));
                return null;
            }

            if (prefabPool == null)
            {
                prefabPool = new List<PrefabAsset>();
            }

            if (prefabPool.Count > 0)
            {
                for (int i = 0; i < prefabPool.Count; ++i)
                {
                    if (!prefabPool[i].IsActive)
                    {
                        prefabPool[i].Requester = requester;
                        return prefabPool[i];
                    }
                }
            }

            //当前池中没有符合需求的
            PrefabAsset newClone = Clone();
            newClone.Requester = requester;

            return newClone;
        }
        
        //检测是否可以卸载
        public bool CheckCanUnload()
        {
            //只要是设置了池的容量，就不能释放
            if (poolCapacity > 0)
                return false;

            //有一个节点被激活，就不能卸载
            for (int i = 0; i < prefabPool.Count; ++i)
            {
                if (prefabPool[i].IsActive)
                    return false;
            }
            return true;
        }

        //不要了~
        public void Unload()
        {
            for (int i = 0; i < prefabPool.Count; ++i)
                prefabPool[i].Unload();

            prefabPool.Clear();

            if (infoNode != null)
                infoNode.OnPrefabAssetNodeRecycle(assetName);

            //自己还回对象池
            nodePool.Return(this);
        }

        public void GetDesc(StringBuilder stringBuilder)
        {
            for (int i = 0; i < prefabPool.Count; i++)
            {
                stringBuilder.AppendFormat("{0}.{1} ==> {2}\n", i, assetName, prefabPool[i].Requester);
            }
        }

        public void OnRecycle()
        {
            //归还
            infoNode.RemoveOriginalAssetRequester(string.Format("PrefabAssetNode_{0}", assetName));
            if (parentNode != null)
            {
                GameObject.Destroy(parentNode.gameObject);
                parentNode = null;
            }
            assetName = string.Empty;
            originPrefab = null;          
            prefabPool.Clear();
            poolCapacity = 0;
    }

        //当某个asset被重置(还回对象池)
        public void OnPrefabAssetReset()
        {
            PoolCapacity = poolCapacity;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is PrefabAssetNode)
                return string.Compare(((PrefabAssetNode)obj).assetName, assetName, true) == 0;

            return false;
        }

    }
}