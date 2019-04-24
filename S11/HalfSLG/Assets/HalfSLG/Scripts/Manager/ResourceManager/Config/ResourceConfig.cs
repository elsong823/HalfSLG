using UnityEngine;

namespace ELGame.Resource
{
    public class ResourceConfig 
    {
        public static string GameName = "HalfSLG";
        public static string UserLanguage = "EN";
        public static string AssetBundleNameSuffix = ".unity3d";         //Bundle文件后缀
        public static string AssetBundlePathPrefix = "assets/halfslg/";    //资源路径前缀
        public static string AssetBundleFlagNamePrefix = "ABF_";
        public const int ResMgrInitRelationCapacity = 500;
        public const int ResMgrInitRequestCapacity = 20;
        public const int ResMgrAsyncListCapacity = 10;
        public const int ResMgrRecycleBinCapacity = 5;
        public static string ResourcePath
        {
            get
            {
                return Application.streamingAssetsPath;
            }
        }
    }
}
