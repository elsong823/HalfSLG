using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace ELGame
{

    public class UIViewManager
            : BaseManager<UIViewManager>
    {
        public override string MgrName => "UIViewManager";

        [SerializeField] private RectTransform screenUIRT;
        private Vector2 screenUICanvasRootSize;

        //配置文件
        private Dictionary<UIViewName, SO_UIViewConfig> uiViewConfig = new Dictionary<UIViewName, SO_UIViewConfig>(LiteSingleton<EnumUIViewNameComparer>.Instance);
        
        //屏幕UI的开启列表
        private List<UIViewBase> viewList = new List<UIViewBase>();
        private Dictionary<UIViewLayer, UIViewLayerController> viewDic = new Dictionary<UIViewLayer, UIViewLayerController>(LiteSingleton<EnumUIViewLayerComparer>.Instance);

        //临时缓冲区内的界面
        public int screenUITempCacheDepth = 0;

        [SerializeField] private List<UIViewLayerController> screenUIViewLayers;
        
        //常驻内存的界面
        private List<UIViewBase> screenUICache = new List<UIViewBase>();
        private List<UIViewBase> screenUITempCache = new List<UIViewBase>();
        
        //初始化管理器
        public override void InitManager()
        {
            base.InitManager();

            //获取根画布尺寸
            screenUICanvasRootSize = screenUIRT.sizeDelta;
            MgrLog(string.Format("Screen ui canvas root size = {0}", screenUICanvasRootSize.ToString()));

            //初始化配置
            InitViewConfig();
            //初始化层级
            InitViewLayers();

            //默认打开主界面
            ShowView(UIViewName.Main);
        }

        //展示界面
        public void ShowView(UIViewName viewName, params object[] args)
        {
            //获取界面配置
            SO_UIViewConfig config = GetConfig(viewName);
            if (config == null)
                return;

            UIViewBase view = null;

            //如果这个窗口是唯一的
            if (config.unique)
            {
                //如果界面是唯一打开的，则判断下是否打开过这个界面
                for (int i = 0; i < viewList.Count; ++i)
                {
                    if (viewList[i].config.viewName == viewName)
                    {
                        //我靠居然打开着呢！
                        view = viewList[i];
                        break;
                    }
                }
                //当前这个界面被打开了
                if (view != null)
                {
                    if (view.layerController == null)
                    {
                        UtilityHelper.LogError(string.Format("Show view error: {0}, not layer", viewName));
                        return;
                    }
                    //设置参数，重新放入窗口层级控制器
                    view.SetArguments(args);
                    view.layerController.Push(view);
                }
                else
                {
                    //没有，打开个新的吧
                    ShowViewFromCacheOrCreateNew(config, args);
                }

            }
            else
            {
                //开！！什么开...
                ShowViewFromCacheOrCreateNew(config, args);
            }

            //刷新显示、隐藏状态
            UpdateViewHideState();
        }

        //只关闭第一个同名界面
        public void HideView(UIViewName viewName)
        {
            for (int i = viewList.Count - 1; i >= 0; --i)
            {
                //关闭
                if (viewList[i].config.viewName == viewName)
                {
                    HideView(viewList[i]);
                    return;
                }
            }
        }

        //根据指定界面
        public void HideView(UIViewBase view)
        {
            if (view == null)
                return;

            //在窗口栈中的界面都可以关闭
            if(view.layerController != null)
            {
                viewList.Remove(view);
                view.layerController.Popup(view);
                SchemeViewCache(view);
                UpdateViewHideState();
            }
            else
            {
                UtilityHelper.LogError(string.Format("Attamp to hide a error view {0}, not in controller.", view.config.viewName));
            }
        }

        //关闭一层所有界面
        public void HideViews(UIViewLayer layer)
        {
            if (!viewDic.ContainsKey(layer))
            {
                UtilityHelper.LogError(string.Format("Hide views error: {0}", layer));
                return;
            }
            UIViewBase[] views = viewDic[layer].PopupAll();
            if (views != null)
            {
                for (int i = 0; i < views.Length; ++i)
                {
                    viewList.Remove(views[i]);
                    SchemeViewCache(views[i]);
                }
                UpdateViewHideState();
            }
        }

        //获取名字相同的第一个界面
        public T GetViewByName<T>(UIViewName viewName)
            where T : UIViewBase
        {
            for (int i = 0; i < viewList.Count; ++i)
            {
                if(viewList[i].config.viewName == viewName)
                {
                    return viewList[i] as T;
                }
            }
            return null;
        }

        //根据缓存类型处理界面
        private void SchemeViewCache(UIViewBase view)
        {
            if (view != null)
            {
                //根据缓存类型处理
                switch (view.config.cacheScheme)
                {
                    case UIViewCacheScheme.Cache:
                        CacheView(view);
                        break;
                    case UIViewCacheScheme.TempCache:
                        TempCacheView(view);
                        break;
                    case UIViewCacheScheme.AutoRemove:
                        ReleaseView(view);
                        break;
                    default:
                        break;
                }
            }
        }

        //释放界面
        private void ReleaseView(UIViewBase view)
        {
            if (view != null)
            {
                view.OnExit();
            }
        }

        //设置临时缓冲池的深度
        public int TempCacheSize
        {
            get
            {
                return screenUITempCacheDepth;
            }
            set
            {
                screenUITempCacheDepth = value;
                TidyTempCache();
            }
        }

        //临时缓存
        private void TempCacheView(UIViewBase view)
        {
            //没有设置池深度，直接释放
            if (screenUITempCacheDepth <= 0)
                ReleaseView(view);

            //放入临时池中
            screenUITempCache.Add(view);

            //整理临时缓存池
            TidyTempCache();
        }

        //整理临时缓存池
        private void TidyTempCache()
        {
            int removeCount = screenUITempCache.Count - screenUITempCacheDepth;
            while (removeCount > 0)
            {
                --removeCount;
                ReleaseView(screenUITempCache[0]);
                screenUITempCache.RemoveAt(0);
            }
        }

        //长期缓存
        private void CacheView(UIViewBase view)
        {
            if (!screenUICache.Contains(view))
            {
                screenUICache.Add(view);
            }
        }

        private SO_UIViewConfig GetConfig(UIViewName viewName)
        {
            if(uiViewConfig.ContainsKey(viewName) == false)
            {
                UtilityHelper.LogError(string.Format("Get view config error: {0}", viewName));
                return null;
            }
            return uiViewConfig[viewName];
        }

        private void PushViewToLayer(UIViewBase view, params object[] args)
        {
            if (view != null)
            {
                //设置参数
                view.SetArguments(args);
                //添加到相应的列表
                viewList.Add(view);
                //压入对应的层中
                viewDic[view.config.viewLayer].Push(view);
            }
        }

        //从池中获取界面
        private UIViewBase GetViewFromCache(SO_UIViewConfig config)
        {
            if (config == null)
                return null;

            UIViewBase view = null;
            List<UIViewBase> cache = null;

            switch (config.cacheScheme)
            {
                case UIViewCacheScheme.Cache:
                    cache = screenUICache;
                    break;
                case UIViewCacheScheme.TempCache:
                    cache = screenUITempCache;
                    break;
                default:
                    break;
            }

            if (cache != null)
            {
                for (int i = 0; i < cache.Count; ++i)
                {
                    if (cache[i].config.viewName == config.viewName)
                    {
                        view = cache[i];
                        //从缓冲区中移除
                        cache.RemoveAt(i);
                        break;
                    }
                }
            }

            return view;
        }

        //完全打开一个新的
        private UIViewBase ShowNewView(SO_UIViewConfig config)
        {
            if (!viewDic.ContainsKey(config.viewLayer))
            {
                UtilityHelper.LogError("Show new view failed. Layer error.");
                return null;
            }

            //加载
            UIViewBase view = CreateUIView(config);
            if (view)
            {
                //创建完毕，初始化
                view.Init();
                view.transform.SetParent(viewDic[config.viewLayer].transform);
                view.GetComponent<RectTransform>().Normalize();
            }
            return view;
        }

        //先尝试从缓存中打开，如果失败则打开一个新的
        private void ShowViewFromCacheOrCreateNew(SO_UIViewConfig config, params object[] args)
        {
            //先尝试从缓存区中读取
            UIViewBase view = GetViewFromCache(config);

            //缓存区内没有，打开新的
            if (view == null)
                view = ShowNewView(config);

            if (view != null)
                PushViewToLayer(view, args);
            else
                UtilityHelper.LogError(string.Format("Show view failed -> {0}", config.viewName));
        }

        //刷新界面的隐藏情况
        private void UpdateViewHideState()
        {
            //从最上层开始刷新
            bool covered = false;
            covered = viewDic[UIViewLayer.Debug].RefreshView(covered);
            covered = viewDic[UIViewLayer.Top].RefreshView(covered);
            covered = viewDic[UIViewLayer.Popup].RefreshView(covered);
            covered = viewDic[UIViewLayer.Base].RefreshView(covered);
            covered = viewDic[UIViewLayer.Background].RefreshView(covered);
        }

        //读入界面
        private UIViewBase CreateUIView(SO_UIViewConfig config)
        {
            string viewBundleName = TranslateViewBundleName(config.assetName);
            GameObject obj = ClonePrefab(viewBundleName, config.assetName);
            if (obj)
            {
                obj.transform.CleanName();
                var viewBase = obj.GetComponent<UIViewBase>();
                return viewBase;
            }

            UtilityHelper.LogError(string.Format("Load view error: no view : {0}", config.assetName));
            return null;
        }

        //初始化界面配置
        private void InitViewConfig()
        {
            Object[] config = GetAssetsFromBundle("scriptableobjects/uiview.unity3d", typeof(SO_UIViewConfig));
            if (config != null)
            {
                for (int i = 0; i < config.Length; ++i)
                {
                    SO_UIViewConfig uvConfig = config[i] as SO_UIViewConfig;
                    if (uvConfig == null)
                        continue;

                    if (uvConfig.cacheScheme == UIViewCacheScheme.Cache)
                        uvConfig.unique = true;
                    uiViewConfig.Add(uvConfig.viewName, uvConfig);
                }
            }

            MgrLog("View config inited.");
        }

        //初始化各层
        private void InitViewLayers()
        {
            for (int i = 0; i < screenUIViewLayers.Count; ++i)
            {
                //层级初始化
                screenUIViewLayers[i].Init();
                viewDic.Add(screenUIViewLayers[i].viewLayer, screenUIViewLayers[i]);
            }
        }
        
        //将世界坐标转换到根画布下坐标
        public Vector2 ConvertWorldPositionToRootCanvasPosition(Vector3 worldPosition)
        {
            Vector2 pos = BattleFieldRenderer.Instance.battleCamera.WorldToScreenPoint(worldPosition);

            //按照屏幕比例转换坐标到Canvas的Recttransform
            pos.x = pos.x / BattleFieldRenderer.Instance.battleCamera.pixelWidth * screenUICanvasRootSize.x - screenUICanvasRootSize.x * 0.5f;
            pos.y = pos.y / BattleFieldRenderer.Instance.battleCamera.pixelHeight * screenUICanvasRootSize.y - screenUICanvasRootSize.y * 0.5f;
            return pos;
        }
        
        //根据一个坐标获取其在CanvasRoot上的相对位置
        public Vector2 GetRelativePosition(Vector2 positionInCanvas)
        {
            return positionInCanvas / screenUICanvasRootSize;
        }

        private string TranslateViewBundleName(string assetName)
        {
            return string.Format("prefabs/uiview/{0}.unity3d", assetName);
        }
        
    }
} 