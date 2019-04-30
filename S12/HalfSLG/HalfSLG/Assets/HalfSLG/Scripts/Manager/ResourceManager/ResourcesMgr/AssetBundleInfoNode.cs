using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace ELGame.Resource
{
    public class AssetBundleInfoNode
    {
        public string bundleName;           //bundle名
        private AssetBundle assetBundle;    //bundle文件
        public int assetCount;              //资源总数                     
        public bool allLoaded = false;      //是否全部加载                               

        private AssetBundle AssetBundle
        {
            get
            {
                if (assetBundle == null)
                {
                    assetBundle = AssetBundle.LoadFromFile(Path.Combine(ResourceConfig.ResourcePath, bundleName));
                    if (!assetBundle)
                        Debug.LogError(string.Format("Load bundle failed... None bundle named ===>>> {0} <<<===", bundleName));
                    else
                        assetCount = assetBundle.GetAllAssetNames().Length;
                }
                return assetBundle;
            }
        }
        
        private Dictionary<string, PrefabAssetNode> prefabAssetNodeDic = new Dictionary<string, PrefabAssetNode>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> originAssetRequesters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        //只是记录资源之间的关系！
        public List<AssetBundleInfoNode> dependentNode = new List<AssetBundleInfoNode>();     //引用到的节点
        public List<AssetBundleInfoNode> beDependentNode = new List<AssetBundleInfoNode>();   //被引用的节点
        
        private AssetBundleInfoNode() { }

        public AssetBundleInfoNode(string bundleName)
        {
            this.bundleName = bundleName.ToLower();
            assetCount = 0;
            allLoaded = false;
        }
        
        //直接获取bundle
        public AssetBundle GetAssetBundle(string requester)
        {
            if (!AssetBundle || !CheckDependencies())
            {
                Debug.LogError("依赖项检测失败，无法直接获取资源呢！");
                return null;
            }

            if (!string.IsNullOrEmpty(requester) && !originAssetRequesters.Contains(requester))
                originAssetRequesters.Add(requester);

            return AssetBundle;
        }

        //获取资源(同步)
        public UObject GetAsset(string assetName, System.Type assetType, string requester)
        {
            if (!AssetBundle || !CheckDependencies())
            {
                Debug.LogError("依赖项检测失败，无法直接获取资源呢！");
                return null;
            }

            //记录请求，没有请求者，也可以给资源，但是不安全
            if (!string.IsNullOrEmpty(requester) && !originAssetRequesters.Contains(requester))
            {
                //没有记录过这个请求者，记录一下
                originAssetRequesters.Add(requester);
            }

            return AssetBundle.LoadAsset(assetName, assetType);
        }

        //直接获取资源
        public T GetAsset<T>(string assetName, string requester)
            where T : UObject
        {
            if (!AssetBundle || !CheckDependencies())
            {
                Debug.LogError("依赖项检测失败，无法直接获取资源呢！");
                return null;
            }
            //记录请求，没有请求人，也可以给资源，但是不安全
            if (!string.IsNullOrEmpty(requester) && !originAssetRequesters.Contains(requester))
            {
                //没有记录过这个请求者，记录一下
                originAssetRequesters.Add(requester);
            }

            return AssetBundle.LoadAsset<T>(assetName);
        }

        //获取全部资源(同步)
        public UObject[] GetAssets(string requester, System.Type assetType)
        {
            if (!AssetBundle || !CheckDependencies())
            {
                Debug.LogError("依赖项检测失败，无法直接获取资源呢！");
                return null;
            }
            //记录请求，没有请求人，也可以给资源，但是不安全
            if (!string.IsNullOrEmpty(requester) && !originAssetRequesters.Contains(requester))
            {
                originAssetRequesters.Add(requester);
            }
            return AssetBundle.LoadAllAssets(assetType);
        }

        //获取资源的副本(同步)
        public PrefabAsset ClonePrefab(string assetName, string requester)
        {
            //检查资源
            if (string.IsNullOrEmpty(requester)
                || !AssetBundle
                || !AssetBundle.Contains(assetName))
                return null;

            PrefabAssetNode assetInfoNode = null;

            //没有clone过
            if (!prefabAssetNodeDic.TryGetValue(assetName, out assetInfoNode))
            {
                assetInfoNode = PrefabAssetNode.Create(this, assetName);

                if (assetInfoNode != null)
                    prefabAssetNodeDic.Add(assetName, assetInfoNode);
                else
                    Debug.LogError(string.Format("Clone prefab failed : {0}"));
            }

            if (assetInfoNode != null)
            {
                return assetInfoNode.Clone(requester);
            }

            return null;
        }

        public void OnPrefabAssetNodeRecycle(string assetName)
        {
            if (prefabAssetNodeDic.ContainsKey(assetName))
                prefabAssetNodeDic.Remove(assetName);

            if (prefabAssetNodeDic.Count == 0 && originAssetRequesters.Count == 0)
                Unload();
        }

        public void RemoveOriginalAssetRequester(string requester)
        {
            if (string.IsNullOrEmpty(requester))
                return;

            if (originAssetRequesters.Contains(requester))
            {
                originAssetRequesters.Remove(requester);

                if (prefabAssetNodeDic.Count == 0 && originAssetRequesters.Count == 0)
                {
                    Unload();
                }
            }
        }

        public void SetCloneCapacity(string assetName, int capacity)
        {
            if (capacity < 0)
                return;

            //检查资源
            if (!AssetBundle
                || !AssetBundle.Contains(assetName))
                return;

            PrefabAssetNode assetInfoNode = null;

            //没有clone过
            if (!prefabAssetNodeDic.TryGetValue(assetName, out assetInfoNode))
            {
                assetInfoNode = PrefabAssetNode.Create(this, assetName);

                if (assetInfoNode != null)
                    prefabAssetNodeDic.Add(assetName, assetInfoNode);
                else
                    Debug.LogError(string.Format("Clone prefab failed : {0}", assetName));
            }

            if (assetInfoNode != null)
                assetInfoNode.PoolCapacity = capacity;
        }

        //归还资源占用
        public bool CheckAssetInThisBundle(string[] assetName)
        {
            for (int i = 0; i < assetName.Length; ++i)
            {
                if (!AssetBundle.Contains(assetName[i]))
                {
                    Debug.LogError(string.Format("{0}不包含名为：{1}的资源！", bundleName, assetName[i]));
                    return false;
                }
            }
            return true;
        }

        //通常只发生在准备或直接加载资源时，需要先将依赖的AB文件加载
        public bool CheckDependencies()
        {
            for (int i = 0; i < dependentNode.Count; ++i)
            {
                if (dependentNode[i].AssetBundle == null)
                    return false;

                //这时需要为每一个依赖项进行标记
                if (!dependentNode[i].originAssetRequesters.Contains(bundleName))
                    dependentNode[i].originAssetRequesters.Add(bundleName);

                if (!dependentNode[i].CheckDependencies())
                    return false;
            }

            return true;
        }
        
        /// <summary>
        /// 卸载资源，彻底卸载
        /// </summary>
        /// <param name="unloadAll"></param>
        private void Unload()
        {
            //检测通过，可以移除自己的占用了（对于依赖项的占用）
            for (int i = 0; i < dependentNode.Count; ++i)
            {
                dependentNode[i].RemoveOriginalAssetRequester(bundleName);
            }

            //准备卸载
            ResourceManager.Instance.PushToRecycleBin(this);
        }

        //执行卸载，通常在检测通过后执行
        public void DoUnload()
        {
#if UNITY_EDITOR
            Debug.LogWarning(string.Format("<color=#00ff00>卸载bundle -> {0}</color>", bundleName));
#endif
            //删除clone
            foreach (var item in prefabAssetNodeDic)
                item.Value.Unload();

            prefabAssetNodeDic.Clear();
            originAssetRequesters.Clear();

            allLoaded = false;

            assetBundle.Unload(ResourceManager.Instance.unloadAllLoadedObjects);
            assetBundle = null;
        }
        
        public void GetDesc(StringBuilder stringBuilder)
        {
            if (stringBuilder == null)
                return;

            stringBuilder.AppendFormat("Bundle name: {0}\n", bundleName);

            if (dependentNode.Count > 0)
            {
                stringBuilder.AppendFormat("===========Dependent==========\n");
                for (int i = 0; i < dependentNode.Count; ++i)
                {
                    stringBuilder.AppendFormat("{0} : {1}\n", i, dependentNode[i].bundleName);
                }
            }

            if (beDependentNode.Count > 0)
            {
                stringBuilder.AppendFormat("===========Be dependent==========\n");
                for (int i = 0; i < beDependentNode.Count; ++i)
                {
                    stringBuilder.AppendFormat("{0} : {1}\n", i, beDependentNode[i].bundleName);
                }
            }

            if (prefabAssetNodeDic.Count > 0)
            {
                stringBuilder.AppendFormat("===========Prefab cloned==========\n");
                foreach (var item in prefabAssetNodeDic)
                {
                    item.Value.GetDesc(stringBuilder);
                }
            }

            if (originAssetRequesters.Count > 0)
            {
                stringBuilder.AppendFormat("===========Asset request==========\n");
                int idx = 0;
                foreach (var item in originAssetRequesters)
                {
                    stringBuilder.AppendFormat("{0} : {1}\n", idx, item);
                }
            }

            stringBuilder.AppendFormat("Assets count：{0}\n", assetCount);
            stringBuilder.AppendFormat("Asset bundle loaded？ {0}\n", assetBundle == null ? "no" : "yes");
            stringBuilder.AppendFormat("All assets loaded？ {0}\n", allLoaded ? "yes" : "no");
        }

        //弓虽卸
        public void ForceUnload()
        {
            //都没加过Assetbundle，填什么乱
            if (assetBundle == null)
                return;
            DoUnload();
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is AssetBundleInfoNode)
            {
                return string.Compare(((AssetBundleInfoNode)obj).bundleName, bundleName, true) == 0;
            }

            return false;
        }

        public AssetBundleRequest LoadAssetAsync(string assetName, System.Type assetType)
        {
            if (AssetBundle)
                return AssetBundle.LoadAssetAsync(assetName, assetType);
            return null;
        }

        public AssetBundleRequest LoadAllAssetsAsync()
        {
            if (AssetBundle)
                return AssetBundle.LoadAllAssetsAsync();
            return null;
        }

    }
}