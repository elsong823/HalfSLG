using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ELGame.Resource
{
    public static class AssetBundlePackageHelper
    {
        private static List<AssetBundleBuild> abMaps = new List<AssetBundleBuild>();

        #region tools
        //标准化路径信息
        public static string NormalizePathFormat(string path)
        {
            return path.Replace("//", "/").Replace("\\", "/");
        }

        //从相对路径转换为绝对路径
        public static string TranslateToRelativePath(string absolutePath)
        {
            string prefix = NormalizePathFormat(Application.dataPath).ToLower();
            absolutePath = NormalizePathFormat(absolutePath).ToLower();
            return absolutePath.Replace(prefix, "assets");
        }

        //从绝对路径转换为相对路径
        public static string TranslateToAbsolutePath(string relativePath)
        {
            relativePath = NormalizePathFormat(relativePath).ToLower();
            string prefix = NormalizePathFormat(Application.dataPath).ToLower();
            if (relativePath.Contains(prefix))
            {
                string path = string.Format("{0}/{1}", prefix, relativePath);
                return NormalizePathFormat(path);
            }
            return relativePath;
        }

        //转换flag为abf名字
        public static string TranslateAssetBundleFlagName(string flag)
        {
            return string.Format("{0}{1}", ResourceConfig.AssetBundleFlagNamePrefix, flag);
        }
        
        //获取所有ABF
        public static List<AssetBundleFlag> GetAssetBundleFlags(string path)
        {
            List<AssetBundleFlag> list = new List<AssetBundleFlag>();
            if (Directory.Exists(path))
            {
                DirectoryInfo direction = new DirectoryInfo(path);

                //查找ABF
                FileInfo[] files = direction.GetFiles("*.asset", SearchOption.AllDirectories)
                    .Where(fileInfo => fileInfo.Name.ToLower().StartsWith(ResourceConfig.AssetBundleFlagNamePrefix.ToLower())
                        && fileInfo.Name.ToLower().EndsWith(".asset")).ToArray();
                
                for (int i = 0; i < files.Length; i++)
                {
                    string relativePath = TranslateToRelativePath(files[i].FullName);
                    AssetBundleFlag info = UnityEditor.AssetDatabase.LoadAssetAtPath<AssetBundleFlag>(relativePath);
                    if (info && !info.ignore)
                        list.Add(info);
                }
            }
            else
            {
                Debug.LogError(string.Format("Get asset bundle flags failed. Invalid path ====>>> {0} <<<===", path));
                return null;
            }
            return list;
        }

        private static void ResetFolder(string absolutePath)
        {
            if (Directory.Exists(absolutePath))
                Directory.Delete(absolutePath, true);

            Directory.CreateDirectory(absolutePath);
        }

        //获取所选文件夹（相对路径）
        private static string[] GetSeletedPaths()
        {
            Object[] objs = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            List<string> dirs = new List<string>();
            foreach (var item in objs)
            {
                dirs.Add(AssetDatabase.GetAssetPath(item));
            }
            return dirs.ToArray();
        }

        private static string[] GetFiles(string path, string suffix, SearchOption searchOption)
        {
            path = path.TrimEnd('/');
            //不包含meta、ds_store、AA_BundleFlag 及 中文字符
            string[] files = Directory.GetFiles(path, suffix, searchOption)
                .Where(file => !file.EndsWith(".meta")
                && file.IndexOf(ResourceConfig.AssetBundleFlagNamePrefix) == -1
                && file.IndexOf(".ds_store") < 0
                && !HasChinese(file)).ToArray();

            return files;
        }

        public static bool HasChinese(string str)
        {
            return Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
        }

        //将绝对路径处理为相对路径
        private static void ProcessPath(string[] paths)
        {
            string appPath = NormalizePathFormat(Application.dataPath);
            for (int i = 0; i < paths.Length; ++i)
            {
                paths[i] = TranslateToRelativePath(paths[i]);
            }
        }

        //把一个文件夹打成bundle( 完整路径 )
        private static AssetBundleBuild AddFolderToBundleBuild(string folderPath, AssetBundleFlag flag, string assetBundleName)
        {
            folderPath = NormalizePathFormat(folderPath);
            AssetBundleBuild abb = new AssetBundleBuild();

            abb.assetBundleName = assetBundleName + ResourceConfig.AssetBundleNameSuffix;
            abb.assetBundleName = NormalizePathFormat(abb.assetBundleName);

            var files = GetFiles(folderPath, flag.suffix, SearchOption.AllDirectories);
            ProcessPath(files);
            abb.assetNames = files;

            return abb;
        }

        private static string RemoveSuffix(string path)
        {
            return path.Replace(Path.GetExtension(path), string.Empty);
        }

        //每个文件单独达成bundle( 完整路径 )
        private static AssetBundleBuild[] AddFilesToBundleBuilds(string[] files, AssetBundleFlag flag)
        {
            //  d:/../../../Assets/Dow/Res/Default/Prefabs/EffRes/xx.prefab
            //  ---> prefabs/effres/xx.prefab
            ProcessPath(files);
            List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
            for (int i = 0; i < files.Length; ++i)
            {
                AssetBundleBuild abb = new AssetBundleBuild();

                string filePath = RemoveSuffix(files[i]);
                filePath = NormalizePathFormat(filePath);
                abb.assetBundleName = filePath.Replace(flag.relativePath.Trim('/'), flag.rootBundleName) + ResourceConfig.AssetBundleNameSuffix;
                abb.assetNames = new string[1] { files[i] };
                builds.Add(abb);
            }
            return builds.ToArray();
        }

        private static void AddTopFolderToBuildMap(string rootPath, AssetBundleFlag flag, List<AssetBundleBuild> buildMap)
        {
            string[] subFolders = Directory.GetDirectories(rootPath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var item in subFolders)
            {
                //相对路径
                string relativePath = TranslateToRelativePath(item).ToLower();
                string assetBundleName = relativePath.Replace(flag.relativePath.TrimEnd('/'), flag.rootBundleName);

                buildMap.Add(AddFolderToBundleBuild(item, flag, assetBundleName));
            }
        }

        private static void AddFlagToBuildMap(AssetBundleFlag flag, List<AssetBundleBuild> buildMap)
        {
            if (buildMap == null)
                return;

            //获取所有文件夹
            string[] files = null;
            string absolutePath = TranslateToAbsolutePath(flag.relativePath);
            switch (flag.bundleType)
            {
                case BundleType.Folder:
                    //将整个文件夹做成bundle
                    buildMap.Add(AddFolderToBundleBuild(absolutePath, flag, flag.rootBundleName));
                    break;

                case BundleType.Single:
                    //将单个的文件做成bundle
                    files = GetFiles(absolutePath, flag.suffix, SearchOption.AllDirectories);
                    buildMap.AddRange(AddFilesToBundleBuilds(files, flag));
                    break;

                case BundleType.SingleFolder:
                    AddTopFolderToBuildMap(absolutePath, flag, buildMap);
                    break;

                case BundleType.SingleFolderAndTopFiles:
                    //子文件夹
                    AddTopFolderToBuildMap(absolutePath, flag, buildMap);
                    //获取所有单独文件
                    files = GetFiles(absolutePath, flag.suffix, SearchOption.TopDirectoryOnly);
                    buildMap.AddRange(AddFilesToBundleBuilds(files, flag));
                    break;
            }
        }

        #endregion

        [MenuItem("Assets/Create/AssetBundleFlag/Create  %&b")]
        static void CreateBundleFlag()
        {
            var path = GetSeletedPaths();
            foreach (var item in path)
            {
                //获取文件名
                string folderName = Path.GetFileNameWithoutExtension(item);
                string relativeName = TranslateToRelativePath(item).ToLower() ;
                string fileName = TranslateAssetBundleFlagName(folderName);
                string assetFileName = Path.Combine(item, fileName);
                FileInfo fileInfo = new FileInfo(assetFileName + ".asset");
                bool isCreate = true;
                if (fileInfo.Exists)
                {
                    isCreate = EditorUtility.DisplayDialog(fileName + " already exists.", "Do you want to overwrite the old file ?", "Yes", "No");
                }
                if (isCreate)
                {
                    AssetBundleFlag bundleFlagInfo = ScriptableObject.CreateInstance<AssetBundleFlag>();
                    bundleFlagInfo.relativePath = relativeName + "/";
                    bundleFlagInfo.rootBundleName = bundleFlagInfo.relativePath.Replace(ResourceConfig.AssetBundlePathPrefix, string.Empty).TrimEnd('/');
                    bundleFlagInfo.categoryName = folderName.ToLower();
                    AssetDatabase.CreateAsset(bundleFlagInfo, assetFileName + ".asset");
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        //生成bundle
        public static void BuildAssetBundle()
        {
            BuildAssetBundleToFolder(Application.streamingAssetsPath, BuildTarget.Android);
        }

        public static void BuildAssetBundleToFolder(string outputPath, BuildTarget buildTarget)
        {
            //重置文件夹
            ResetFolder(outputPath);
            abMaps.Clear();

            List<AssetBundleFlag> bundleFlags = GetAssetBundleFlags(Application.dataPath);
            Debug.Log(string.Format("Add asset bundle flag to build map"));
            foreach (var flag in bundleFlags)
                AddFlagToBuildMap(flag, abMaps);

            //打Bundle
            //None ==> Build assetBundle without any special option. ()
            //UncompressedAssetBundlee ==> Don't compress the data when creating the asset bundle.
            //DisableWriteTypeTreee ==> Do not include type information within the AssetBundle.
            //DeterministicAssetBundlee ==> Builds an asset bundle using a hash for the id of the object stored in the asset bundle.
            //ForceRebuildAssetBundlee ==> Force rebuild the assetBundles.
            //IgnoreTypeTreeChangese ==> Ignore the type tree changes when doing the incremental build check.
            //AppendHashToAssetBundleNamee ==> Append the hash to the assetBundle name.
            //ChunkBasedCompressione ==> Use chunk - based LZ4 compression when creating the AssetBundle.
            //StrictModee ==> Do not allow the build to succeed if any errors are reporting during it.
            //DryRunBuilde ==> Do a dry run build.
            //DisableLoadAssetByFileNamee ==> Disables Asset Bundle LoadAsset by file name.
            //DisableLoadAssetByFileNameWithExtensione ==> Disables Asset Bundle LoadAsset by file name with extension.
            BuildPipeline.BuildAssetBundles(outputPath, abMaps.ToArray(), 
                BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.StrictMode, 
                buildTarget);

            AssetDatabase.Refresh();

            UnityEditor.EditorUtility.DisplayDialog("Ready", "Asset bundle build compeleted.", "OK");
        }
    }
}
