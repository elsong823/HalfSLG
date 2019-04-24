using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ELGame.Resource
{
    //记录requester的请求
    public class AssetRequested
        : IRecyclable
    {
        private static LitePool<AssetRequested> pool = new LitePool<AssetRequested>();

        public static AssetRequested Get()
        {
            return pool.Get();
        }

        private string requester;
        public Dictionary<string, List<PrefabAsset>> prefabRequests = null;
        public HashSet<string> originalRequests = null;

        //直接获取AssetBundle
        public AssetBundle GetAssetBundle(AssetBundleInfoNode assetBundleInfoNode, string requester)
        {
            AssetBundle assetBundle = assetBundleInfoNode.GetAssetBundle(requester);
            if (assetBundle != null)
            {
                this.requester = requester;

                if (originalRequests == null)
                    originalRequests = new HashSet<string>(System.StringComparer.CurrentCultureIgnoreCase);

                if (!originalRequests.Contains(assetBundleInfoNode.bundleName))
                    originalRequests.Add(assetBundleInfoNode.bundleName);
            }
            return assetBundle;
        }

        //同步获取资源
        public Object GetAsset(AssetBundleInfoNode assetBundleInfoNode, string assetName, System.Type assetType, string requester)
        {
            var asset = assetBundleInfoNode.GetAsset(assetName, assetType, requester);
            if (asset != null)
            {
                this.requester = requester;

                if (originalRequests == null)
                    originalRequests = new HashSet<string>(System.StringComparer.CurrentCultureIgnoreCase);

                if (!originalRequests.Contains(assetBundleInfoNode.bundleName))
                    originalRequests.Add(assetBundleInfoNode.bundleName);
            }
            return asset;
        }

        //同步获取bundle中的全部资源
        public Object[] GetAssets(AssetBundleInfoNode assetBundleInfoNode, System.Type assetType, string requester)
        {
            var assets = assetBundleInfoNode.GetAssets(requester, assetType);
            if (assets != null && assets.Length > 0)
            {
                this.requester = requester;

                if (originalRequests == null)
                    originalRequests = new HashSet<string>(System.StringComparer.CurrentCultureIgnoreCase);

                if (!originalRequests.Contains(assetBundleInfoNode.bundleName))
                    originalRequests.Add(assetBundleInfoNode.bundleName);
            }
            return assets;
        }

        //直接clone Prefab
        public GameObject ClonePrefab(AssetBundleInfoNode assetBundleInfoNode, string asset, string requester)
        {
            PrefabAsset assetNode = assetBundleInfoNode.ClonePrefab(asset, requester);

            if (assetNode == null)
                return null;

            this.requester = requester;

            if (prefabRequests == null)
                prefabRequests = new Dictionary<string, List<PrefabAsset>>(System.StringComparer.CurrentCultureIgnoreCase);

            List<PrefabAsset> list = null;
            if (!prefabRequests.TryGetValue(assetBundleInfoNode.bundleName, out list))
            {
                list = new List<PrefabAsset>();
                prefabRequests.Add(assetBundleInfoNode.bundleName, list);
            }

            list.Add(assetNode);

            return assetNode.clone;
        }

        public void OnRecycle()
        {
            if (prefabRequests != null)
                prefabRequests.Clear();

            if (originalRequests != null)
                originalRequests.Clear();
        }

        private bool CheckEmpty()
        {
            bool empty = (prefabRequests == null || prefabRequests.Count == 0) && (originalRequests == null || originalRequests.Count == 0);
            if (empty)
                pool.Return(this);

            return empty;
        }

        //按照名字归还
        public bool ReturnBundle(string bundleName)
        {
            if (prefabRequests != null && prefabRequests.ContainsKey(bundleName))
            {
                for (int i = 0; i < prefabRequests[bundleName].Count; i++)
                {
                    prefabRequests[bundleName][i].Requester = string.Empty;
                }
                prefabRequests.Remove(bundleName);
            }

            if (originalRequests != null && originalRequests.Contains(bundleName))
            {
                var node = ResourceManager.Instance.Get(bundleName, false);
                if (node != null)
                    node.RemoveOriginalAssetRequester(requester);

                originalRequests.Remove(bundleName);
            }

            bool empty = CheckEmpty();

            return empty;
        }

        //归还所有
        public void ReturnAll()
        {
            //归还对prefab的占用
            if (prefabRequests != null && prefabRequests.Count > 0)
            {
                foreach (var item in prefabRequests)
                {
                    if (item.Value != null && item.Value.Count > 0)
                    {
                        for (int i = 0; i < item.Value.Count; i++)
                        {
                            item.Value[i].Requester = string.Empty;
                        }
                    }
                    item.Value.Clear();
                }
                prefabRequests.Clear();
            }

            //归还对original的占用
            if (originalRequests != null && originalRequests.Count > 0)
            {
                AssetBundleInfoNode node = null;
                foreach (var bundle in originalRequests)
                {
                    node = ResourceManager.Instance.Get(bundle, false);
                    if (node != null)
                    {
                        node.RemoveOriginalAssetRequester(requester);
                    }
                }
                originalRequests.Clear();
            }

            CheckEmpty();
        }

        public void GetDesc(StringBuilder stringBuilder)
        {
            if (stringBuilder == null)
                return;
            stringBuilder.AppendFormat("Requester => {0}\n", requester);
            if (prefabRequests != null)
            {
                stringBuilder.AppendFormat("Prefab requests:\n");
                int idx = 0;
                foreach (var item in prefabRequests)
                {
                    stringBuilder.AppendFormat("\t{0}.{1}\n", idx, item.Key);
                    ++idx;
                    int subIdx = 0;
                    foreach (var asset in item.Value)
                    {
                        stringBuilder.AppendFormat("\t\t{0}.{1}\n", subIdx, asset.assetNode.assetName);
                        ++subIdx;
                    }
                }
            }
            if (originalRequests != null)
            {
                int idx = 0;
                stringBuilder.AppendFormat("Origin requests:\n");
                foreach (var item in originalRequests)
                {
                    stringBuilder.AppendFormat("\t{0}.{1}\n", idx, item);
                    ++idx;
                }
            }
        }
    }
}