using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BaseBehaviour
        : MonoBehaviour
    {
        private string requestorSTR = null;
        public string RequestorSTR
        {
            get
            {
                if (string.IsNullOrEmpty(requestorSTR))
                    requestorSTR = gameObject.RequestorSTR();

                return requestorSTR;
            }
        }
        
        public override bool Equals(object other)
        {
            if (other != null && other is BaseBehaviour)
                return ((BaseBehaviour)other).GetInstanceID() == this.GetInstanceID();

            return false;
        }

        public GameObject ClonePrefab(string bundleName, string assetName)
        {
            if (Resource.ResourceManager.Instance)
                return Resource.ResourceManager.Instance.ClonePrefab(bundleName, assetName, RequestorSTR);

            return null;
        }

        public Object[] GetAssetsFromBundle(string bundleName, System.Type assetType)
        {
            if (Resource.ResourceManager.Instance)
                return Resource.ResourceManager.Instance.GetAssets(bundleName, RequestorSTR, assetType);

            return null;
        }

        public void SetClonePoolCapacity(string bundle, string asset, int capacity)
        {
            if (Resource.ResourceManager.Instance)
                Resource.ResourceManager.Instance.SetPrefabClonePoolCapacity(bundle, asset, capacity);

        }

        public AssetBundle GetAssetBundle(string bundle)
        {
            if (Resource.ResourceManager.Instance)
                return Resource.ResourceManager.Instance.GetAssetBundle(bundle, RequestorSTR);

            return null;
        }

        //归还requester请求的对应的bundle资源
        public void ReturnBundle(string bundle)
        {
            if (Resource.ResourceManager.Instance)
                Resource.ResourceManager.Instance.ReturnBundleByName(RequestorSTR, bundle);
        }

        //归还requester请求的全部资源
        public void ReturnAllRequests()
        {
            if (Resource.ResourceManager.Instance)
                Resource.ResourceManager.Instance.ReturnAllByRequester(RequestorSTR);
        }


        private void OnDestroy()
        {
            if (Resource.ResourceManager.Instance)
                Resource.ResourceManager.Instance.ReturnAllByRequester(RequestorSTR);
        }
    }
}
