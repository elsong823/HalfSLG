using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ELGame
{
    //当前ui的显示状态
    public enum UIViewState
    {
        Nonvisible, //不可见的
        Visible,    //可见的
        Cache,      //在缓存中
    }

    public class UIViewBase
        : MonoBehaviour
    {
        public SO_UIViewConfig config;

        [HideInInspector]
        public UIViewLayerController layerController;   //所属的层
        
        [SerializeField]
        protected List<GameObject> UIObjects = new List<GameObject>();

        private UIViewState viewState;                   //当前界面的状态

        private bool dirty = false;                      //是否需要刷新了

        protected Canvas canvas;
        
        public UIViewState ViewState
        {
            get
            {
                return viewState;
            }
            set
            {
                viewState = value;
#if UNITY_EDITOR
                switch (viewState)
                {
                    case UIViewState.Nonvisible:
                        name = string.Format("{0}(HIDE)", config.assetName);
                        break;
                    case UIViewState.Visible:
                        name = config.assetName;
                        break;
                    case UIViewState.Cache:
                        if(config.cacheScheme == UIViewCacheScheme.Cache)
                            name = string.Format("{0}(CACHE)", config.assetName);
                        else if(config.cacheScheme == UIViewCacheScheme.TempCache)
                            name = string.Format("{0}(TEMP)", config.assetName);
                        break;
                    default:
                        break;
                }
#endif
            }
        }

        //设置界面层级
        public int ViewOrder
        {
            get
            {
                return canvas.sortingOrder;
            }
            set
            {
                canvas.sortingOrder = value;
            }
        }

        private void InitCanvas()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            //添加射线检测
            GraphicRaycaster caster = GetComponent<GraphicRaycaster>();
            if (caster == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        public void SetArguments(params object[] args)
        {
            dirty = true;

            UpdateArguments(args);

            //当前还没有加入层级
            if (layerController == null)
                return;

            //如果当前这个界面就是显示的
            //或尽管隐藏也需要刷新
            //那么设定了参数直接就要刷新
            if(config.alwaysUpdate || viewState == UIViewState.Visible)
                UpdateView();
        }

        protected virtual void UpdateArguments(params object[] args){}

        //界面初始化
        public void Init()
        {
            //UtilityHelper.Log(string.Format("View Init : {0}, {1}", config.viewName, this.GetInstanceID()));
            InitCanvas();
            InitUIObjects();
            InitBG();
        }

        //初始化各UI对象
        protected virtual void InitUIObjects() { }

        protected void RegisterEventListener(string key, ELGame.GameEventHandler handler, int times = EGameConstL.Infinity)
        {
            EventManager.Instance.Register(key, this.gameObject.RequestorSTR(), handler, times);
        }

        protected void RemoveAllEventListeners()
        {
            EventManager.Instance.Unregister(this.gameObject.RequestorSTR());
        }

        //初始化背景(点击背景自动关闭界面)
        protected void InitBG()
        {
            if (config.bgTriggerClose)
            {
                Transform bgTran = transform.Find(EGameConstL.STR_BG);
                if (bgTran == null)
                {
                    GameObject bgObj = new GameObject(EGameConstL.STR_BG, typeof(RectTransform));
                    bgTran = bgObj.transform;
                    bgTran.SetParent(transform);
                    bgTran.SetAsFirstSibling();
                    RectTransform rt = bgObj.GetComponent<RectTransform>();
                    rt.Normalize();
                }
                //查看是否有图片
                Image img = bgTran.GetComponent<Image>();
                if (img == null)
                {
                    img = bgTran.gameObject.AddComponent<Image>();
                    img.color = EGameConstL.Color_Transparent;
                    CanvasRenderer cr = bgTran.GetComponent<CanvasRenderer>();
                    cr.cullTransparentMesh = true;
                }
                img.raycastTarget = true;
                //是否有事件点击
                EventTrigger eventTrigger = bgTran.GetComponent<EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = bgTran.gameObject.AddComponent<EventTrigger>();
                }
                //监听点击背景的事件
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerClick;
                entry.callback.AddListener(CloseWithEvent);
                eventTrigger.triggers.Add(entry);
            }
        }

        //更新层级和Order值
        private void UpdateLayer()
        {
            //界面在没有active的时候，重写是无效的
            if (!canvas.overrideSorting)
            {
                canvas.overrideSorting = true;
                switch (config.viewLayer)
                {
                    case UIViewLayer.Background:
                        canvas.sortingLayerID = EGameConstL.SortingLayer_UI_Background;
                        break;
                    case UIViewLayer.Base:
                        canvas.sortingLayerID = EGameConstL.SortingLayer_UI_Base;
                        break;
                    case UIViewLayer.Popup:
                        canvas.sortingLayerID = EGameConstL.SortingLayer_UI_Popup;
                        break;
                    case UIViewLayer.Top:
                        canvas.sortingLayerID = EGameConstL.SortingLayer_UI_Top;
                        break;
                    case UIViewLayer.Debug:
                        canvas.sortingLayerID = EGameConstL.SortingLayer_UI_Debug;
                        break;
                    default:
                        UtilityHelper.LogError(string.Format("Set Layer And Order failed: Error layer -> {0}", config.viewLayer));
                        break;
                }
            }
        }

        //被压入到窗口栈中
        public virtual void OnPush()
        {
            //UtilityHelper.Log(string.Format("View On Push : {0}, {1}", config.viewName, this.GetInstanceID()));
            ViewState = UIViewState.Nonvisible;
            UpdateLayer();
        }

        //被显示时
        public virtual void OnShow()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                UpdateLayer();
            }

            if (ViewState != UIViewState.Visible)
            {
                //将z坐标归0
                Vector3 pos = transform.localPosition;
                pos.z = 0;
                transform.localPosition = pos;

                ViewState = UIViewState.Visible;

                UIViewManager.Instance.MgrLog(string.Format("View On Show : {0}, {1}", config.viewName, this.GetInstanceID()));
            }

            if (dirty)
                UpdateView();
        }

        //更新
        public virtual void UpdateView()
        {
            dirty = false;
            UIViewManager.Instance.MgrLog(string.Format("Update View -> {0}, {1}", config.viewName, this.GetInstanceID()));
        }

        //被隐藏
        public virtual void OnHide()
        {
            if (ViewState == UIViewState.Visible)
            {
                //从相机的视域体内推出
                Vector3 pos = transform.localPosition;
                pos.z = -EGameConstL.Infinity;
                transform.localPosition = pos;

                ViewState = UIViewState.Nonvisible;
            }
        }

        //被移出窗口栈
        public virtual void OnPopup()
        {
            if (ViewState == UIViewState.Cache)
                return;
            
            //如果不是隐藏状态，需要先隐藏
            if(ViewState == UIViewState.Visible)
                OnHide();

            UIViewManager.Instance.MgrLog(string.Format("View On Popup : {0}, {1}", config.viewName, this.GetInstanceID()));
            ViewState = UIViewState.Cache;

        }

        //将被移除
        public virtual void OnExit()
        {
            //如果不是缓存池状态，则需要先弹出
            if (ViewState != UIViewState.Cache)
                OnPopup();

            UIViewManager.Instance.MgrLog(string.Format("View On Exit : {0}, {1}", config.viewName, this.GetInstanceID()));
        }
        
        //关闭窗口
        public void Close()
        {
            UIViewManager.Instance.HideView(this);
        }

        protected void ErrorClose(string error)
        {
            UIViewManager.Instance.HideView(this);
            UtilityHelper.LogError(error);
        }

        //关闭窗口(被用作点击背景自动关闭的回调)
        protected virtual void CloseWithEvent(BaseEventData eventData)
        {
            UIViewManager.Instance.HideView(this);
        }

        //获取prefab
        protected GameObject CloneAsset(string bundle, string asset)
        {
            return null;
        }

        //获取资源
        protected T GetAsset<T>(string bundle, string asset)
            where T : UnityEngine.Object
        {
            return null;
        }

        //获取通用的界面组成元素相关
        List<KeyValuePair<string, GameObject>> viewElementList = null;

        private string GetViewElementBundleName(string elementName)
        {
            return string.Format("prefabs/ViewElement/{0}", elementName);
        }

        private GameObject GetViewElement(string elementName)
        {
            string bundleName = GetViewElementBundleName(elementName);
            GameObject element = CloneAsset(bundleName, elementName);
            if (element)
            {
                if (viewElementList == null)
                    viewElementList = new List<KeyValuePair<string, GameObject>>();
                //记录下来
                viewElementList.Add(new KeyValuePair<string, GameObject>(elementName, element));
            }
            return element;
        }

        protected Button GetViewElement_Button(string name, UnityEngine.Events.UnityAction callback)
        {
            GameObject obj = GetViewElement(name);
            if (!obj)
            {
                UtilityHelper.LogError(string.Format("Get button {0} failed.", name));
                return null;
            }
            Button button = obj.GetComponent<Button>();
            if (!button)
            {
                UtilityHelper.LogError(string.Format("Get button {0} failed.No button component.", name));
                return null;
            }
            //显示按钮
            obj.SetActive(true);
            //移除按钮的点击
            button.onClick.RemoveAllListeners();
            //注册点击事件
            button.onClick.AddListener(callback);
            return button;
        }

        protected void SetNodeVisible(GameObject obj, bool visible)
        {
            if (obj != null)
            {
                if (obj.activeSelf != visible)
                    obj.SetActive(visible);
            }
        }

        public override int GetHashCode()
        {
            return this.GetInstanceID();
        }

        public override bool Equals(object other)
        {
            if (other != null && other is UIViewBase)
            {
                return ((UIViewBase)other).GetInstanceID() == GetInstanceID();
            }
            return false;
        }

        protected void SetObjectText(GameObject obj, string str)
        {
            TextMeshProUGUI text = obj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                text.text = str;
        }
        
    }
}