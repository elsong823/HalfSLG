using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace ELGame
{
    public class ELEditorTools
    {
        private const string PrefabPath = "Assets/HalfSLG/Prefabs/UIView";
        private const string OutputPath = "Assets/HalfSLG/ScriptableObjects/UIView";
        //遍历存储界面prefab的文件夹，生成SO_UIViewConfig
        [MenuItem("EL_Tools/Process UI config")]
        public static void ProcessUIViewConfig()
        {
            string prefabPath = UtilityHelper.ConvertToObsPath(PrefabPath);
            string[] allUIPrefab = Directory.GetFiles(
                prefabPath,
                "*.prefab",
                SearchOption.TopDirectoryOnly)
                 .Where(
                    file => file.EndsWith(".prefab") && file.ToLower().Contains("uiview_")).ToArray();

            List<string> newConfigs = new List<string>();
            int count = 0;
            foreach (var item in allUIPrefab)
            {
                string fileName = Path.GetFileNameWithoutExtension(item);
                string soPath = UtilityHelper.ConvertToObsPath(string.Format("{0}/{1}.asset", OutputPath, fileName));
                ++count;
                if (!File.Exists(soPath))
                {
                    //不存在这个so，创建新的
                    SO_UIViewConfig newConfig = ScriptableObject.CreateInstance<SO_UIViewConfig>();
                    newConfig.assetName = fileName;
                    AssetDatabase.CreateAsset(newConfig, string.Format("{0}/{1}.asset", OutputPath, fileName));
                    newConfigs.Add(string.Format(string.Format("(新建) {0}. {1} -> {2} ", count, fileName, OutputPath)));
                }
                else
                {
                    newConfigs.Add(string.Format(string.Format("(已存在) {0}. {1} -> {2} ", count, fileName, OutputPath)));
                }
            }
            if (newConfigs.Count > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                //显示界面
                UIViewConfigHelperWindow.Show("刷新界面配置文件", newConfigs, new Rect(200, 200, 600, newConfigs.Count * 22 + 50));
            }
            else
            {
                EditorUtility.DisplayDialog("Complete", "There isn't any view config need to be created.", "OK");
            }
        }
    }

}