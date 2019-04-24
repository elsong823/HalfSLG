using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SObject = System.Object;

namespace ELGame.Resource
{
    //请求类型
    public enum AssetRequestType
    {
        Part,            //部分
        Part_SameType,   //部分-同种类型
        All,             //所有
    }

    public class AssetAsyncRequest
    {
        public AssetRequestType assetRequestType;
        public bool markCancel = false;     //被标记为取消了，真折腾
        public bool error = false;          //存在错误
        public string requester;
        public AssetBundleInfoNode assetBundleInfoNode;
        public string bundleName;
        public string[] assetNameArray;
        public System.Type[] assetTypeArray;
        public System.Type assetType;
        public System.Action<string, string[], System.Type> assetsLoadedCallbackSameType = null;
        public System.Action<string, string[], System.Type[]> assetsLoadedCallback = null;
        public System.Action<string> bundleLoadedCallback = null;
        public Dictionary<string, UnityEngine.Object> assetDic = null;
        
        private AssetAsyncRequest() { }

        private void Reset()
        {
            requester = null;
            assetBundleInfoNode = null;
            bundleName = null;
            assetNameArray = null;
            assetType = null;
            assetTypeArray = null;
            bundleLoadedCallback = null;
            assetsLoadedCallbackSameType = null;
            assetsLoadedCallback = null;
            assetDic = null;
            markCancel = false;
            error = false;
        }
        
        public static AssetAsyncRequest CreatePartRequest(
            string bundleName, 
            AssetBundleInfoNode assetBundleInfoNode, 
            string[] assetNameArray, 
            System.Type[] assetTypeArray,
            System.Action<string, string[], System.Type[]> assetsLoadedCallback,
            Dictionary<string, UnityEngine.Object> assetDic)
        {
            if (assetBundleInfoNode == null || string.IsNullOrEmpty(bundleName))
            {
                Debug.LogError("Create asset request failed ::: Bundle name or asset bundle is null");
                return null;
            }

            if (assetNameArray == null || assetTypeArray == null || assetNameArray.Length != assetTypeArray.Length)
            {
                Debug.LogError("Create asset request failed ::: NAME.count != TYPE.count");
                return null;
            }

            AssetAsyncRequest request = new AssetAsyncRequest();
            request.Reset();
            request.assetRequestType = AssetRequestType.Part;
            request.bundleName = bundleName;
            request.assetBundleInfoNode = assetBundleInfoNode;

            request.assetNameArray = assetNameArray;
            request.assetTypeArray = assetTypeArray;

            request.assetsLoadedCallback = assetsLoadedCallback;

            request.assetDic = assetDic;

            return null;
        }

        public static AssetAsyncRequest CreatePartRequest(
            string bundleName, 
            AssetBundleInfoNode assetBundleInfoNode, 
            string[] assetNameArray,
            System.Type assetType, 
            System.Action<string, string[], System.Type> assetsLoadedCallbackSameType,
            Dictionary<string, UnityEngine.Object> assetDic)
        {
            if (assetBundleInfoNode == null || string.IsNullOrEmpty(bundleName))
            {
                Debug.LogError("Create asset request failed ::: Bundle name or asset bundle is null");
                return null;
            }

            if (assetNameArray == null || assetType == null)
            {
                Debug.LogError("Create asset request failed");
                return null;
            }

            AssetAsyncRequest request = new AssetAsyncRequest();
            request.Reset();
            request.assetRequestType = AssetRequestType.Part_SameType;
            request.bundleName = bundleName;
            request.assetBundleInfoNode = assetBundleInfoNode;
            
            request.assetNameArray = assetNameArray;
            request.assetType = assetType;

            request.assetsLoadedCallbackSameType = assetsLoadedCallbackSameType;

            request.assetDic = assetDic;

            return null;
        }

        public static AssetAsyncRequest CreateBundleRequest(
            string bundleName, 
            AssetBundleInfoNode assetBundleInfoNode, 
            System.Action<string> bundleLoadedCallback,
            Dictionary<string, UnityEngine.Object> assetDic)
        {
            if (assetBundleInfoNode == null || string.IsNullOrEmpty(bundleName))
            {
                Debug.LogError("Create asset request failed ::: Bundle name or asset bundle is null");
                return null;
            }
            
            AssetAsyncRequest request = new AssetAsyncRequest();
            request.Reset();
            request.assetRequestType = AssetRequestType.All;
            request.bundleName = bundleName;
            request.assetBundleInfoNode = assetBundleInfoNode;

            request.bundleLoadedCallback = bundleLoadedCallback;

            request.assetDic = assetDic;

            return request;
        }
        
        //加载完毕
        public bool AssetsReady()
        {
            //如果选择的是全部加载
            //标记一下，下载速度会快些
            if (assetRequestType == AssetRequestType.All && assetBundleInfoNode != null)
                assetBundleInfoNode.allLoaded = true;

            bool hasError = this.error;

            if (markCancel)
            {
                Debug.LogWarning("Assets ready, but request has been canceled.... FUCK!!!");
                Reset();
                return hasError;
            }

            switch (assetRequestType)
            {
                case AssetRequestType.Part:
                    //通知一下
                    if (assetsLoadedCallback != null)
                    {
                        assetsLoadedCallback(bundleName, assetNameArray, assetTypeArray);
                        ResourceManager.Instance.MgrLog(string.Format("一个关于 ===> {0} <=== 的 多个 资源请求已经全部准备就绪。是否发生过错误：{1} ", bundleName, error ? "是" : "否"));
                    }
                    break;
                case AssetRequestType.All:
                    if (bundleLoadedCallback != null)
                    {
                        bundleLoadedCallback(bundleName);
                        ResourceManager.Instance.MgrLog(string.Format("一个关于 ===> {0} <=== 的 全部 资源请求已经全部准备就绪。是否发生过错误：{1} ", bundleName, error ? "是" : "否"));
                    }
                    break;
                case AssetRequestType.Part_SameType:
                    if (assetsLoadedCallbackSameType != null)
                    {
                        assetsLoadedCallbackSameType(bundleName, assetNameArray, assetType);
                        ResourceManager.Instance.MgrLog(string.Format("一个关于 ===> {0} <=== 的 多个 同类 资源请求已经全部准备就绪。是否发生过错误：{1} ", bundleName, error ? "是" : "否"));
                    }
                    break;
                default:
                    break;
            }

            Reset();

            return hasError;
        }

        public string Desc()
        {
            return string.Empty;
        }
    }
}