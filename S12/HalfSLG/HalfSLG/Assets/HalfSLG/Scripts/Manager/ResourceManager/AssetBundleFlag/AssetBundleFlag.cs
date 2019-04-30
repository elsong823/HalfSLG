using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame.Resource
{
    public enum BundleType
    {
        Folder,                      //.asset 所在目录，生成一个AssetBundle
        Single,                      //.asset 所在目录，深度遍历，将每一个文件生成一个AssetBundle
        SingleFolder,                //.asset 所在目录，将每一个文件夹生成一个AssetBundle
        SingleFolderAndTopFiles,     //.asset 所在目录，将每个文件夹、每个文件生成一个AssetBundle
    }
    
    public class AssetBundleFlag 
        : ScriptableObject
    {
        public bool ignore = false;                         //是否忽视
        public BundleType bundleType = BundleType.Folder;   //打包类型
        public string relativePath = string.Empty;          //相对路径
        public string rootBundleName = string.Empty;        //资源路径
        public string suffix = "*.*";                       //文件后缀
        public string categoryName = "";                    //目录名称(生成资源映射关系用)
    }
}