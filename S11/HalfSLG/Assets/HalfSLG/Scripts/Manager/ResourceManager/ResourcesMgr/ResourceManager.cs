using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System.Text;
using UnityEngine.U2D;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

namespace ELGame.Resource
{
#if UNITY_EDITOR
    public class CloneNodeInEditorMode
    {
        public GameObject cloned;
        public string requester;
    }
#endif

    public class ResourceManager
        : BaseManager<ResourceManager>
    {
        public override string MgrName => "ResourceManager";

        [HideInInspector]
        [SerializeField] public bool editorMode = true;    //在编辑器上直接读取

        [Space(),HideInInspector]
        public bool unloadAllLoadedObjects = true;  //在Unload时是否彻底卸载资源

        [Header("Async setting:"), Space(), HideInInspector]
        public int asyncLoadLimitPerFrame = 1;  //每帧允许添加到异步加载队列中的数量

        //保存bundle信息的节点
        private Dictionary<string, AssetBundleInfoNode> assetBundleInfoNodes = new Dictionary<string, AssetBundleInfoNode>(ResourceConfig.ResMgrInitRelationCapacity, System.StringComparer.OrdinalIgnoreCase);
        //记录requester的请求信息
        private Dictionary<string, AssetRequested> requesterData = new Dictionary<string, AssetRequested>(ResourceConfig.ResMgrInitRequestCapacity, System.StringComparer.OrdinalIgnoreCase);

        //异步加载队列
        private List<AssetAsyncRequest> asyncWaitingList = new List<AssetAsyncRequest>(ResourceConfig.ResMgrAsyncListCapacity); //等待队列
        private List<AssetAsyncRequest> asyncLoadingList = new List<AssetAsyncRequest>(ResourceConfig.ResMgrAsyncListCapacity); //加载队列
        
        //回收
        private List<RecycleBinItem> recycleBin = new List<RecycleBinItem>(ResourceConfig.ResMgrRecycleBinCapacity); //资源回收站
        [Header("Recycle bin setting:"), Space()]
        [Range(0, 10), HideInInspector] public int recycleDepth = 0;
        [Range(-1f, 1f), HideInInspector] public float recycleUpdateInterval = 0.01f;
        [Range(1, 10), HideInInspector] public int recycleCountLimit = 2;   //每次回收的数量
        private float recycleTimer = 0f;

        #region Tools function
        public string ManifestPath
        {
            get
            {
                return string.Format("{0}/StreamingAssets", Application.streamingAssetsPath);
            }
        }

        public override void InitManager()
        {
            base.InitManager();

            ResetManager();

            MgrLog("===========>> Resource manager INITED !!! <<=============");
            
            EventManager.Instance.Run(EGameConstL.EVENT_RESOURCE_MANAGER_READY, null);
        }
        
        private void SetupBundleRelationships()
        {
            var bundle = AssetBundle.LoadFromFile(ManifestPath);
            if (bundle)
            {
                AssetBundleManifest abManifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                if (abManifest == null)
                {
                    Debug.LogError("Setup bundle info nodes failed, none manifest file... => " + ManifestPath);
                    return;
                }

                //setup relationship
                assetBundleInfoNodes.Clear();
                string[] assets = abManifest.GetAllAssetBundles();
                foreach (var item in assets)
                {
                    if (item.Contains("lua/"))
                        continue;

                    //Create bundle info node
                    if (!assetBundleInfoNodes.ContainsKey(item.ToLower()))
                    {
                        AssetBundleInfoNode node = new AssetBundleInfoNode(item);
                        assetBundleInfoNodes.Add(item.ToLower(), node);
                    }
                }

                foreach (var item in assetBundleInfoNodes)
                {
                    //Get dependencies
                    string[] dependencies = abManifest.GetDirectDependencies(item.Key);

                    foreach (var dependence in dependencies)
                    {
                        //Ignore shader bundle
                        if (dependence.Contains("shaders.unity3d"))
                            continue;

                        if (assetBundleInfoNodes.ContainsKey(dependence.ToLower()))
                        {
                            AssetBundleInfoNode son = assetBundleInfoNodes[dependence];
                            AssetBundleInfoNode parent = item.Value;
                            if (parent.dependentNode.Contains(son) || son.beDependentNode.Contains(parent))
                            {
                                continue;
                            }
                            else
                            {
                                //Link
                                son.beDependentNode.Add(parent);
                                parent.dependentNode.Add(son);
                            }
                        }
                        else
                        {
                            Debug.LogError(string.Format("Bundle node error: can not find out dependence node, file name : {0}", dependence));
                        }
                    }
                }

                bundle.Unload(true);
                WarmUpShaders();

                Debug.Log("======>>  Bundle relationship compelted！！  <<==========");
            }
            else
            {
                Debug.LogError("Get manifest failed！！ => " + ManifestPath);
            }
        }
        
        private string ProcessEditorPath(string abName)
        {
            abName = abName.Replace(ResourceConfig.AssetBundleNameSuffix, string.Empty);
            return string.Format("Assets/{0}/Res/default/{1}", ResourceConfig.GameName, abName).ToLower();
        }

        //尝试获取bundle节点
        public AssetBundleInfoNode Get(string abName, bool considerRecycleBin)
        {
            AssetBundleInfoNode node = null;

            if (considerRecycleBin)
            {
                var recycleBinItem = GetFromRecycleBin(abName);
                if (recycleBinItem != null)
                    node = recycleBinItem.Cancel();

                if (node != null)
                    return node;
            }

            if (assetBundleInfoNodes.TryGetValue(abName, out node))
                return node;
            else
            {
                Debug.LogError(string.Format("Get bundle info ==>> {0} <<== failed!!!", abName));
                return null;
            }
        }
        
        public void WarmUpShaders()
        {
            Debug.Log("Warm up all shaders.");
        }

        public override void ResetManager()
        {
            if (editorMode)
            {
                Debug.LogWarning("Renew resource manager in editor mode.");
                return;
            }

            foreach (var item in assetBundleInfoNodes)
            {
                item.Value.ForceUnload();
            }
            Debug.LogWarning("Renew resource manager!");

            assetBundleInfoNodes.Clear();
            requesterData.Clear();
            asyncWaitingList.Clear();
            asyncLoadingList.Clear();

            SetupBundleRelationships();
        }
        
        private bool CheckIsInAsyncList(AssetBundleInfoNode infoNode)
        {
            for (int i = asyncWaitingList.Count - 1; i >= 0; --i)
            {
                if (string.Compare(infoNode.bundleName, asyncWaitingList[i].bundleName, true) == 0)
                    return true;
            }

            //加载中的标记取消
            for (int i = 0; i < asyncLoadingList.Count; ++i)
            {
                if (string.Compare(infoNode.bundleName, asyncLoadingList[i].bundleName, true) == 0)
                    return true;
            }

            return false;
        }

        #endregion

        #region Main function

        /// <summary>
        /// 异步请求bundle下所有资源 
        /// </summary>
        /// <param name="bundles">bundle数组</param>
        /// <param name="requester">请求者</param>
        /// <param name="bundleCompleteCallback">加载完毕回调</param>
        /// <param name="assetsDic">资源dic</param>
        /// <returns></returns>
        public bool RequestAllAssetsAsync(
            string[] bundles, 
            string requester, 
            System.Action<string> bundleCompleteCallback,
            Dictionary<string, UnityEngine.Object> assetsDic)
        {
            if (bundles == null || bundles.Length == 0)
                return false;

            if (editorMode)
            {
                //编辑器模式下不做检测，直接认为存在
                foreach (var item in bundles)
                {
                    bundleCompleteCallback(item);
                }
                return true;
            }

            bool error = true;
            
            //使用bundle方式请求全部资源
            for (int i = 0; i < bundles.Length; ++i)
            {
                AssetBundleInfoNode node = Get(bundles[i], true);

                if (node != null)
                {
                    //已经全部加载过了
                    if (node.allLoaded && bundleCompleteCallback != null)
                        bundleCompleteCallback(bundles[i]);
                    //可能并没有全部加载，开始加载吧
                    else
                        AppendRequest(bundles[i], node, null, null, null, requester, bundleCompleteCallback, null, null, assetsDic);
                }
                else
                    error = false;
            }

            return error;
        }

        /// <summary>
        /// 异步请求资源
        /// </summary>
        /// <param name="bundle">bundle名字</param>
        /// <param name="assets">资源名字</param>
        /// <param name="types">资源类型</param>
        /// <param name="requester">请求者</param>
        /// <param name="assetsCompleteCallback">资源加载完毕回调</param>
        /// <param name="assetsDic">存放资源的映射</param>
        /// <returns></returns>
        public bool RequestAssetsAsync(
            string bundle, 
            string[] assets, 
            System.Type[] types, 
            string requester,
            System.Action<string, string[], System.Type[]> assetsCompleteCallback,
            Dictionary<string, Object> assetsDic)
        {
            if (string.IsNullOrEmpty(bundle)
                || assets == null
                || types == null
                || assets.Length != types.Length)
                return false;

            if (editorMode)
            {
                //编辑器模式下不做检测，直接认为存在
                assetsCompleteCallback(bundle, assets, types);
                return true;
            }

            AssetBundleInfoNode node = Get(bundle, true);
            if (node != null)
            {
                if (!node.CheckAssetInThisBundle(assets))
                    return false;

                if (node.allLoaded && assetsCompleteCallback != null)
                {
                    assetsCompleteCallback(bundle, assets, types);
                }
                else
                {
                    AppendRequest(bundle, node, assets, null, types, requester, null, assetsCompleteCallback, null, assetsDic);
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// 异步请求资源（同种类型）
        /// </summary>
        /// <param name="bundle">bundle名字</param>
        /// <param name="assets">资源名字</param>
        /// <param name="types">资源类型</param>
        /// <param name="requester">请求者</param>
        /// <param name="assetsCompleteCallback">资源加载完毕回调</param>
        /// <param name="assetsDic">存放资源的映射</param>
        /// <returns></returns>
        public bool RequestAssetsAsync(
            string bundle, 
            string[] assets, 
            System.Type type, 
            string requester, 
            System.Action<string, string[], System.Type> assetsCompleteCallback,
            Dictionary<string, Object> assetsDic)
        {
            if (string.IsNullOrEmpty(bundle)
                || assets == null
                || type == null)
                return false;

            //本地下请求资源
            if (editorMode)
            {
                //编辑器模式下不做检测，直接认为存在
                assetsCompleteCallback(bundle, assets, type);
                return true;
            }

            AssetBundleInfoNode node = Get(bundle, true);
            if (node != null)
            {
                if (!node.CheckAssetInThisBundle(assets))
                    return false;

                if (node.allLoaded && assetsCompleteCallback != null)
                {
                    assetsCompleteCallback(bundle, assets, type);
                }
                else
                {
                    AppendRequest(bundle, node, assets, type, null, requester, null, null, assetsCompleteCallback, assetsDic);
                }

                return true;
            }
            return false;
        }

        //取消异步请求
        public void CancleRequest(string requester)
        {
            //本地下请求资源
            if (string.IsNullOrEmpty(requester))
                return;

            if (editorMode)
                return;

            //取消
            for (int i = asyncWaitingList.Count - 1; i >= 0; --i)
            {
                if (string.Compare(requester, asyncWaitingList[i].requester, true) == 0)
                    asyncWaitingList.RemoveAt(i);
            }

            //加载中的标记取消
            for (int i = 0; i < asyncLoadingList.Count; ++i)
            {
                asyncLoadingList[i].markCancel = true;
            }
        }

        //同步获取资源
        public Object GetAsset(string bundle, string asset, System.Type assetType, string requester)
        {
            if (bundle == string.Empty
                || asset == string.Empty
                || assetType == null)
                return null;

#if UNITY_EDITOR
            if (editorMode)
                return GetAssetInEditorMode(bundle, asset, assetType);
#endif

            AssetBundleInfoNode node = Get(bundle, true);
            if (node == null)
                return null;

            var assetRequested = GetAssetRequested(requester);

            return assetRequested.GetAsset(node, asset, assetType, requester);
        }

        //同步获取资源
        public T GetAsset<T>(string bundle, string asset, string requester)
            where T : Object
        {
            return (T)GetAsset(bundle, asset, typeof(T), requester);
        }

        //同步获取bundle中的全部资源
        public Object[] GetAssets(string bundle, string requester, System.Type assetType)
        {
            if (bundle == string.Empty)
                return null;

#if UNITY_EDITOR
            if (editorMode)
            {
                string path = string.Format("{0}{1}", ResourceConfig.AssetBundlePathPrefix, bundle);
                return GetAssetsInEditorMode(path, assetType);
            }
#endif

            AssetBundleInfoNode node = Get(bundle, true);
            if (node == null)
            {
                Debug.LogError(string.Format("尝试获取 {0} 全部资源失败，没有这个bundle", bundle));
                return null;
            }

            var assetRequested = GetAssetRequested(requester);

            return assetRequested.GetAssets(node, assetType, requester);
        }

        //直接clone Prefab
        public GameObject ClonePrefab(string bundle, string asset, string requester)
        {
            if (bundle == string.Empty
                || asset == string.Empty
                || string.IsNullOrEmpty(requester))
            {
                Debug.LogError(string.Format("克隆资源出错：{0} -> {1} from {2}", bundle, asset, requester.ToString()));
                return null;
            }

            //本地下请求资源
#if UNITY_EDITOR
            if (editorMode)
                return CloneGameObjInEditorMode(bundle, asset, requester);
#endif

            AssetBundleInfoNode node = Get(bundle, true);
            if (node == null)
            {
                Debug.LogError(string.Format("clone {0} 失败，没有这个bundle", bundle));
                return null;
            }
            
            var assetRequested = GetAssetRequested(requester);

            return assetRequested.ClonePrefab(node, asset, requester);
        }

        public void SetPrefabClonePoolCapacity(string bundle, string asset, int capacity)
        {
#if UNITY_EDITOR
            if (editorMode)
            {
                return;
            }
#endif

            if (bundle == string.Empty || asset == string.Empty)
                return;

            AssetBundleInfoNode node = Get(bundle, true);
            if (node == null)
            {
                Debug.LogError(string.Format("Capackty {0} 失败，没有这个bundle", bundle));
                return;
            }

            node.SetCloneCapacity(asset, capacity);
        }
        
        //归还requester请求的对应的bundle资源
        public void ReturnBundleByName(string requester, string bundle)
        {
#if UNITY_EDITOR
            if (editorMode)
            {
                for (int i = m_cloneNodes.Count - 1; i >= 0; --i)
                {
                    var item = m_cloneNodes[i];
                    if (string.Compare(item.requester, requester, true) == 0)
                    {
                        Destroy(item.cloned);
                        m_cloneNodes.RemoveAt(i);
                    }
                }
                return;
            }
#endif
            if (string.IsNullOrEmpty(requester) || string.IsNullOrEmpty(bundle))
                return;

            AssetRequested assetRequested = null;
            if (!requesterData.TryGetValue(requester, out assetRequested))
            {
                Debug.LogWarning(string.Format("尝试按照名字归还bundle失败：{0}，没有关联过这个requester {1}", bundle, requester));
                return;
            }

            //移除后这个requester已经没有任何资源的占用了，移除
            if (assetRequested.ReturnBundle(bundle))
            {
                requesterData.Remove(requester);
            }
        }

        //归还requester请求的全部资源
        public void ReturnAllByRequester(string requester)
        {
#if UNITY_EDITOR
            if (editorMode)
            {
                for (int i = m_cloneNodes.Count - 1; i >= 0; --i)
                {
                    var item = m_cloneNodes[i];
                    if (string.Compare(item.requester, requester, true) == 0)
                    {
                        Destroy(item.cloned);
                        m_cloneNodes.RemoveAt(i);
                    }
                }
                return;
            }
#endif

            if (string.IsNullOrEmpty(requester))
                return;

            AssetRequested assetRequested = null;
            if (!requesterData.TryGetValue(requester, out assetRequested))
                return;
            
            //移除后这个requester已经没有任何资源的占用了，移除
            assetRequested.ReturnAll();

            requesterData.Remove(requester);

            CancleRequest(requester);
        }

        public AssetBundle GetAssetBundle(string bundle, string requester)
        {
            if (editorMode)
            {
                Debug.LogWarning("Get asset bundle failed. It's in editor mode now.");
                return null;
            }

            AssetBundleInfoNode node = Get(bundle, true);
            if (node == null)
            {
                Debug.LogError(string.Format("Get Asset Bundle {0} failed", bundle));
                return null;
            }
            return node.GetAssetBundle(requester);
        }

#endregion

#if UNITY_EDITOR
        private List<CloneNodeInEditorMode> m_cloneNodes = new List<CloneNodeInEditorMode>();

        private GameObject CloneGameObjInEditorMode(string bundle, string asset, string requester)
        {
            var model = GetAssetInEditorMode(bundle, asset, typeof(GameObject));
            if (model == null)
                return null;

            GameObject clone = Instantiate(model as GameObject);
            if (clone != null)
            {
                clone.SetActive(false);
                CloneNodeInEditorMode node = new CloneNodeInEditorMode();
                node.cloned = clone;
                node.requester = requester;
                m_cloneNodes.Add(node);
            }

            Renderer[] renderers = clone.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                renderer.material.shader = Shader.Find(renderer.material.shader.name);
            }
            Image[] images = clone.GetComponentsInChildren<Image>(true);
            foreach (var item in images)
            {
                item.material.shader = Shader.Find(item.material.shader.name);
            }

            return clone;
        }

        private Object[] GetAssetsInEditorMode(string relativePath, System.Type assetType)
        {
            if (Path.HasExtension(relativePath))
                relativePath = relativePath.Replace(Path.GetExtension(relativePath), string.Empty);

            if (!Directory.Exists(relativePath))
            {
                relativePath = relativePath.Substring(0, relativePath.LastIndexOf('/'));
                if (!Directory.Exists(relativePath))
                {
                    Debug.LogError(string.Format("本地尝试获取所有资源出错，目标位置并非一个文件夹：{0}", relativePath));
                    return null;
                }
            }

            List<Object> objects = new List<Object>();
            if (assetType == typeof(GameObject))
            {
                string[] files = Directory.GetFiles(relativePath, "*.prefab", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; ++i)
                {
                    Object obj = AssetDatabase.LoadAssetAtPath(files[i].Replace(Application.dataPath, "Assets"), assetType);
                    if (obj != null)
                        objects.Add(obj);
                }
            }
            else if (assetType == typeof(Sprite) || assetType == typeof(Texture2D))
            {
                string[] files = Directory.GetFiles(relativePath, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".png") || s.EndsWith(".jpg")).ToArray();
                for (int i = 0; i < files.Length; ++i)
                {
                    Object obj = AssetDatabase.LoadAssetAtPath(files[i].Replace(Application.dataPath, "Assets"), assetType);
                    if (obj != null)
                        objects.Add(obj);
                }
            }
            else if (assetType == typeof(Material))
            {
                string[] files = Directory.GetFiles(relativePath, "*.mat", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; ++i)
                {
                    Object obj = AssetDatabase.LoadAssetAtPath(files[i].Replace(Application.dataPath, "Assets"), assetType);
                    if (obj != null)
                        objects.Add(obj);
                }
            }
            else if (assetType == typeof(Shader))
            {
                string[] files = Directory.GetFiles(relativePath, "*.shader", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; ++i)
                {
                    Object obj = AssetDatabase.LoadAssetAtPath(files[i].Replace(Application.dataPath, "Assets"), assetType);
                    if (obj != null)
                        objects.Add(obj);
                }
            }
            else if (assetType == typeof(ScriptableObject) || assetType.BaseType == typeof(ScriptableObject))
            {
                string[] files = Directory.GetFiles(relativePath, "*.asset", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; ++i)
                {
                    Object obj = AssetDatabase.LoadAssetAtPath(files[i].Replace(Application.dataPath, "Assets"), assetType);
                    if (obj != null)
                        objects.Add(obj);
                }
            }
            else if (assetType == typeof(SpriteAtlas))
            {
                string[] files = Directory.GetFiles(relativePath, "*.spriteatlas", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; ++i)
                {
                    Object obj = AssetDatabase.LoadAssetAtPath(files[i].Replace(Application.dataPath, "Assets"), assetType);
                    if (obj != null)
                        objects.Add(obj);
                }
            }
            return objects.ToArray();
        }

        private Object GetAssetInEditorMode(string relativePath, string fileName, System.Type assetType)
        {
            //先尝试移除后缀
            if (Path.HasExtension(relativePath))
                relativePath = relativePath.Replace(Path.GetExtension(relativePath), string.Empty);

            //做一次文件存在的判断
            string path = string.Format("{0}{1}", ResourceConfig.AssetBundlePathPrefix, relativePath);
            //这是一个文件夹，表示fileName是有用的
            if (Directory.Exists(path))
                path = string.Format("{0}{1}/{2}", ResourceConfig.AssetBundlePathPrefix, relativePath, fileName);

            //表示这是一个文件夹
            string suffix = string.Empty;
            if (assetType == typeof(Sprite) || assetType == typeof(Texture2D))
            {
                //图片需要多测试一次
                suffix = ".png";
                path += suffix;
                if (!File.Exists(path))
                    path = path.Replace(".png", ".jpg");
            }
            else if (assetType == typeof(GameObject))
            {
                suffix = ".prefab";
                path += suffix;
            }
            else if (assetType == typeof(Material))
            {
                suffix = ".mat";
                path += suffix;
            }
            else if (assetType == typeof(AudioClip))
            {
                suffix = ".ogg";
                path += suffix;
            }
            else if (assetType == typeof(Shader))
            {
                suffix = ".shader";
                path += suffix;
            }
            else
            {
                Debug.LogError(string.Format("本地尝试读取了错误的文件->{0},类型->{1}", fileName, assetType));
            }
            path = path.Replace(Application.dataPath, "Assets").Replace("\\", "/");
            var obj = AssetDatabase.LoadAssetAtPath(path, assetType);
            if (assetType == typeof(Material))
            {
                Material mat = obj as Material;
                mat.shader = Shader.Find(mat.shader.name);
            }
            return obj;
        }

#endif

        private AssetRequested GetAssetRequested(string requester)
        {
            AssetRequested assetRequested = null;
            if (!requesterData.TryGetValue(requester, out assetRequested))
            {
                assetRequested = AssetRequested.Get();
                requesterData.Add(requester, assetRequested);
            }
            return assetRequested;
        }
        
        private IEnumerator PerformAsyncLoad(AssetAsyncRequest request)
        {
            AssetBundleRequest req = null;
            switch (request.assetRequestType)
            {
                case AssetRequestType.Part:
                    for (int i = 0; i < request.assetTypeArray.Length; ++i)
                    {
                        req = request.assetBundleInfoNode.LoadAssetAsync(request.assetNameArray[i], request.assetTypeArray[i]);
                        yield return req;
                        if (req.asset == null)
                            request.error = true;
                        else if (request.assetDic != null)
                            request.assetDic[request.assetNameArray[i]] = req.asset;
                    }
                    break;

                case AssetRequestType.Part_SameType:
                    for (int i = 0; i < request.assetTypeArray.Length; ++i)
                    {
                        req = request.assetBundleInfoNode.LoadAssetAsync(request.assetNameArray[i], request.assetType);
                        yield return req;
                        if (req.asset == null)
                            request.error = true;
                        else if (request.assetDic != null)
                            request.assetDic[request.assetNameArray[i]] = req.asset;
                    }
                    break;

                case AssetRequestType.All:
                    req = request.assetBundleInfoNode.LoadAllAssetsAsync();
                    yield return req;
                    if (req.allAssets != null)
                    {
                        if (request.assetDic != null)
                        {
                            for (int j = 0; j < req.allAssets.Length; j++)
                            {
                                request.assetDic[req.allAssets[j].name] = req.allAssets[j];
                            }
                        }
                    }
                    else
                        request.error = true;
                    break;
                default:
                    break;
            }

            request.AssetsReady();

            asyncLoadingList.Remove(request);
        }

        //追加异步请求
        private void AppendRequest(
            string bundleName,
            AssetBundleInfoNode assetBundleInfoNode,
            string[] assets,
            System.Type type,
            System.Type[] types,
            string requester,
            System.Action<string> bundleCallback,
            System.Action<string, string[], System.Type[]> assetCallback,
            System.Action<string, string[], System.Type> assetCallbackSameType,
            Dictionary<string, UnityEngine.Object> assetDic)
        {
            AssetAsyncRequest req = null;
            if (assets != null && type != null)
                req = AssetAsyncRequest.CreatePartRequest(bundleName, assetBundleInfoNode, assets, type, assetCallbackSameType, assetDic);
            else if (assets != null && types != null)
                req = AssetAsyncRequest.CreatePartRequest(bundleName, assetBundleInfoNode, assets, types, assetCallback, assetDic);
            else
                req = AssetAsyncRequest.CreateBundleRequest(bundleName, assetBundleInfoNode, bundleCallback, assetDic);

            //加入异步加载队列
            if (req != null)
            {
                asyncWaitingList.Add(req);
                if (DebugMode)
                    MgrLog(string.Format("Append async request... ==> {0}", asyncWaitingList.Count));
            }
        }

        private void Update()
        {
            UpdateAsyncList();
            UpdateRecycleBin();
        }

        public void Desc()
        {
            StringBuilder stringBuilder = new StringBuilder(10000);
            stringBuilder.AppendFormat("======= Resource Manager detail ========\n");
            stringBuilder.AppendLine();
            foreach (var item in assetBundleInfoNodes)
            {
                item.Value.GetDesc(stringBuilder);
                stringBuilder.AppendLine();
            }
            Debug.LogWarning(stringBuilder.ToString());

            stringBuilder.Length = 0;
            stringBuilder.AppendFormat("======= Recycle bin detail ========\n");
            stringBuilder.AppendLine();

            stringBuilder.AppendFormat("Depth = {0}\n", ResourceConfig.ResMgrRecycleBinCapacity);
            for (int i = 0; i < recycleBin.Count; i++)
            {
                stringBuilder.AppendFormat("{0}.{1} ts = {2:0.0}\n", i, recycleBin[i].assetBundleInfoNode.bundleName, recycleBin[i].timeStamp);
                stringBuilder.AppendLine();
            }
            Debug.LogWarning(stringBuilder.ToString());

            stringBuilder.Length = 0;
            stringBuilder.AppendFormat("======= Reqeuster detail ========\n");
            stringBuilder.AppendLine();

            stringBuilder.AppendFormat("Depth = {0}\n", ResourceConfig.ResMgrRecycleBinCapacity);
            foreach (var item in requesterData)
            {
                item.Value.GetDesc(stringBuilder);
                stringBuilder.AppendLine();
            }
            Debug.LogWarning(stringBuilder.ToString());
        }

        private void UpdateAsyncList()
        {
            int count = 0;
            for (int i = asyncWaitingList.Count - 1; i >= 0; --i)
            {
                StartCoroutine(PerformAsyncLoad(asyncWaitingList[i]));
                asyncLoadingList.Add(asyncWaitingList[i]);
                asyncWaitingList.RemoveAt(i);

                ++count;

                //达到上限
                if (count >= asyncLoadLimitPerFrame)
                    break;
            }
        }

        private void UpdateRecycleBin()
        {
            //无需回收
            if (recycleBin.Count <= recycleDepth)
                return;

            if (recycleUpdateInterval > Mathf.Epsilon)
            {
                recycleTimer += Time.unscaledDeltaTime;
                if (recycleTimer < recycleUpdateInterval)
                    return;

                recycleTimer = 0f;
            }

            //需要卸载的数量
            int unloadCount = Mathf.Min(recycleBin.Count - recycleDepth, recycleCountLimit);
            int alreadyUnloaded = 0;

            recycleBin.Sort(LiteSingleton<RecycleBinItemComparer>.Instance);

            for (int i = recycleBin.Count - 1; i >= 0; --i)
            {
                if (alreadyUnloaded >= unloadCount)
                    break;

                //回收
                recycleBin[i].Clear();
                ++alreadyUnloaded;
                recycleBin.RemoveAt(i);
            }
        }

        //加入回收站，等待回收
        public void PushToRecycleBin(AssetBundleInfoNode assetBundleInfoNode)
        {
            if (assetBundleInfoNode == null)
                return;

            if (CheckIsInAsyncList(assetBundleInfoNode))
            {
                Debug.LogWarning(string.Format("Push to recycle bin failed.In async list ===> {0}", assetBundleInfoNode.bundleName));
                return;
            }
            
            RecycleBinItem recycleBinItem = RecycleBinItem.Get();

            recycleBinItem.assetBundleInfoNode = assetBundleInfoNode;
            recycleBinItem.timeStamp = Time.unscaledTime;                  //记录时间，用于排序

            recycleBin.Add(recycleBinItem);
        }

        //从回收站中移除，并获取
        private RecycleBinItem GetFromRecycleBin(string bundleName)
        {
            for (int i = 0; i < recycleBin.Count; ++i)
            {
                if (string.Compare(recycleBin[i].assetBundleInfoNode.bundleName, bundleName, true) == 0)
                {
                    RecycleBinItem item = recycleBin[i];
                    recycleBin.RemoveAt(i);
                    return item;
                }
            }
            return null;
        }
    }
}