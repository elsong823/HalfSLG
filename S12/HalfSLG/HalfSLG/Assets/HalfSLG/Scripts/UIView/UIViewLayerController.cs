using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class UIViewLayerController
        :MonoBehaviour, IGameBase
    {
		public UIViewLayer viewLayer;
        //每一个界面的Order间隔
        private const int viewOrderStep = 100;
        //最上层Order值
		private int topOrder = 0;
        
        //保存这一层的窗口列表,索引越大越靠近上方
        private List<UIViewBase> views = new List<UIViewBase>();
        
        //压入一个新的窗口(设置为最大order)
        public void Push(UIViewBase view)
        {
            //判断是否本来就在这个队列中
            if (view.layerController != null)
            {
                if (view.ViewOrder == topOrder)
                    return;
                else
                {
                    views.Remove(view);
                    views.Add(view);
                    topOrder += viewOrderStep;
                    view.ViewOrder = topOrder;
                }
            }
            else
            {
                views.Add(view);
                topOrder += viewOrderStep;
                PushSingleView(view);
            }
        }

        //弹出一个指定窗口
        public void Popup(UIViewBase view)
        {
            if (view == null)
                return;

            bool err = true;
            for (int i = views.Count - 1; i >= 0; --i)
            {
                if (views[i].GetInstanceID() == view.GetInstanceID())
                {
                    views.RemoveAt(i);
                    PopupSingleView(view);
                    err = false;
                    break;
                }
            }

            if (err)
            {
                UtilityHelper.LogError(string.Format("Popup view failed. Can not find {0} in {1}", view.config.viewName, viewLayer));
                return;
            }

            //刷新最大order
            RefreshTopOrder();
        }

        //弹出最上层窗口
        public UIViewBase PopupTop()
        {
            UIViewBase view = null;
            if (views.Count > 0)
            {
                view = views[views.Count - 1];
                views.RemoveAt(views.Count - 1);
                PopupSingleView(view);
            }
            RefreshTopOrder();
            return view;
        }

        //移除全部
        public UIViewBase[] PopupAll()
        {
            if (views.Count == 0)
                return null;

            UIViewBase cur = null;
            UIViewBase[] allViews = views.ToArray();
            if (views.Count > 0)
            {
                for (int i = views.Count - 1; i >= 0; --i)
                {
                    cur = views[i];
                    views.RemoveAt(i);
                    PopupSingleView(cur);
                }
                topOrder = 0;
            }
            return allViews;
        }

        //刷新界面的显示
        public bool RefreshView(bool alreadyCovered)
        {
            //如果已经覆盖了全部的屏幕
            if (alreadyCovered)
            {
                for (int i = views.Count - 1; i >= 0; --i)
                {
                    if (views[i].config.alwaysUpdate)
                        views[i].OnShow();
                    else
                        views[i].OnHide();
                }
                return true;
            }
            //当前还没有覆盖整个屏幕
            else
            {
                bool covered = false;
                for (int i = views.Count - 1; i >= 0; --i)
                {
                    if (views[i].config.alwaysUpdate)
                        views[i].OnShow();
                    else
                    {
                        if (covered)
                            views[i].OnHide();
                        else
                            views[i].OnShow();
                    }

                    if (!covered)
                        covered = views[i].config.coverScreen;
                }
                return covered;
            }
        }

        //压入单个界面
        private void PushSingleView(UIViewBase view)
        {
            if (view != null)
            {
                view.layerController = this;
                view.OnPush();
                view.ViewOrder = topOrder;
            }
        }

        //弹出单个界面
        private void PopupSingleView(UIViewBase view)
        {
            if (view != null)
            {
                view.ViewOrder = 0;
                view.layerController = null;
                view.OnPopup();
            }
        }

        //刷新最大的order
        private void RefreshTopOrder()
        {
            if (views.Count == 0)
                topOrder = 0;
            else
                topOrder = views[views.Count - 1].ViewOrder;
        }
        
        public string Desc()
        {
            return string.Empty;
        }

        public void Init(params object[] args)
        {
            UIViewManager.Instance.MgrLog(string.Format("UI view controller {0} inited", viewLayer));
        }
    }
}