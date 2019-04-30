using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using LitJson;

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
                    EditorUtility.SetDirty(newConfig);
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

        private const string JsonFolder = "Assets/HalfSLG/Config/JsonConfig";
        private const string TextureRootFolder = "Assets/HalfSLG/Textures";
        private const string SO_ItemFolder = "Assets/HalfSLG/ScriptableObjects/PackageItem";
        private const string SO_SkillFolder = "Assets/HalfSLG/ScriptableObjects/BattleSkill";
        [MenuItem("EL_Tools/Refresh config object")]
        public static void RefreshConfigObject()
        {
            RefreshItemObject();
            RefreshSkillObject();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "配置刷新完成", "谢谢");
        }

        private static T GetTextureAsset<T>(string bundleFolderName, string assetName)
            where T : Object
        {
            string path = string.Format("{0}/{1}", TextureRootFolder, bundleFolderName);
            string[] assets = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(delegate (string fileName)
            {
                if (path.ToLower().Contains(".meta"))
                    return false;

                fileName = Path.GetFileNameWithoutExtension(fileName.ToLower());
                return string.Compare(fileName, assetName, true) == 0;
            }).ToArray();

            if (assets != null && assets.Length > 0)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(assets[0]);
                return asset;
            }
            return null;
        }


        public static int ParseFromJson(LitJson.JsonData jsonData, string key, int defaultVal)
        {
            if (jsonData == null)
                return defaultVal;
            if (!jsonData.ContainsKey(key))
                return defaultVal;
            int rst = 0;
            if (int.TryParse(jsonData[key].ToString(), out rst))
                return rst;
            return defaultVal;
        }

        public static string ParseFromJson(LitJson.JsonData jsonData, string key, string defaultVal)
        {
            if (jsonData == null)
                return defaultVal;
            if (!jsonData.ContainsKey(key))
                return defaultVal;
            return jsonData[key].ToString();
        }

        public static float ParseFromJson(LitJson.JsonData jsonData, string key, float defaultVal)
        {
            if (jsonData == null)
                return defaultVal;
            if (!jsonData.ContainsKey(key))
                return defaultVal;
            float rst = 0f;
            if (float.TryParse(jsonData[key].ToString(), out rst))
                return rst;
            return defaultVal;
        }

        //TODO：刷新Item和skill在逻辑上非常重复，以后提出通用部分

        private static void RefreshItemObject()
        {
            //先保存已有的
            Dictionary<string, SO_PackageItem> exists = new Dictionary<string, SO_PackageItem>(System.StringComparer.CurrentCultureIgnoreCase);
            List<string> newList = new List<string>();
            List<string> removeList = new List<string>();
            
            string[] existObject = Directory.GetFiles(SO_ItemFolder, "*.asset", SearchOption.AllDirectories).Where(delegate(string filePath)
            {
                if (filePath.ToLower().Contains("meta"))
                    return false;
                return true;
            }).ToArray();
            foreach (var item in existObject)
            {
                SO_PackageItem packageItem = AssetDatabase.LoadAssetAtPath<SO_PackageItem>(item);
                if (packageItem != null)
                    exists.Add(packageItem.name, packageItem);
            }

            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(string.Format("{0}/item.json", JsonFolder));
            var items = JsonMapper.ToObject(textAsset.text);
            if (items.IsArray)
            {
                foreach (var item in items)
                {
                    JsonData itemData = item as JsonData;
                    if (itemData == null)
                        continue;

                    string objName = ParseFromJson(itemData, "objName", string.Empty);
                    SO_PackageItem assetItem = null;
                    //尝试从存在的列表中获取
                    if (exists.ContainsKey(objName))
                    {
                        assetItem = exists[objName];
                        //移除，后面要删除没有找到的
                        exists.Remove(objName);
                    }

                    if (assetItem == null)
                    {
                        newList.Add(objName);
                        string assetPath = string.Format("{0}/{1}.asset", SO_ItemFolder, objName);
                        assetItem = ScriptableObject.CreateInstance<SO_PackageItem>();
                        AssetDatabase.CreateAsset(assetItem, assetPath);
                    }

                    assetItem.itemID = ParseFromJson(itemData, "ID", 0);
                    assetItem.itemName = ParseFromJson(itemData, "itemName", string.Empty);

                    //设置图片
                    string bundleName = ParseFromJson(itemData, "IconBundle", string.Empty);
                    string assetName = ParseFromJson(itemData, "itemIcon", string.Empty);
                    assetItem.icon = GetTextureAsset<Sprite>(bundleName, assetName);

                    string itemType = ParseFromJson(itemData, "itemType", string.Empty);
                    if (string.Compare(itemType, "Recovery", true) == 0)
                        assetItem.itemType = PackageItemType.Recover;

                    assetItem.hpRecovery = ParseFromJson(itemData, "HPRecovery", 0);
                    assetItem.energyRecovery = ParseFromJson(itemData, "EnergyRecovery", 0);

                    EditorUtility.SetDirty(assetItem);
                }
            }

            //需要删除没有的
            if (exists.Count > 0)
            {
                if (EditorUtility.DisplayDialog("警告", string.Format("存在{0}个配置外的道具, 是否删除", exists.Keys.Count), "是", "否"))
                {
                    removeList = new List<string>(exists.Keys);
                    foreach (var item in exists)
                    {
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(item.Value));
                    }
                    exists.Clear();
                }
            }

            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
            stringBuilder.AppendFormat("道具生成结束，新增:{0},删除{1}\n", newList.Count, removeList.Count);
            stringBuilder.AppendFormat("  新增:\n");
            for (int i = 0; i < newList.Count; i++)
            {
                stringBuilder.AppendFormat("    {0}.{1}\n", i, newList[i]);
            }

            stringBuilder.AppendFormat("  删除:\n");
            for (int i = 0; i < removeList.Count; i++)
            {
                stringBuilder.AppendFormat("    {0}.{1}\n", i, removeList[i]);
            }
            Debug.Log(stringBuilder.ToString());
        }

        private static void RefreshSkillObject()
        {
            //先保存已有的
            Dictionary<string, SO_BattleSkill> exists = new Dictionary<string, SO_BattleSkill>(System.StringComparer.CurrentCultureIgnoreCase);
            List<string> newList = new List<string>();
            List<string> removeList = new List<string>();

            string[] existObject = Directory.GetFiles(SO_SkillFolder, "*.asset", SearchOption.AllDirectories).Where(delegate (string filePath)
            {
                if (filePath.ToLower().Contains("meta"))
                    return false;
                return true;
            }).ToArray();

            foreach (var item in existObject)
            {
                SO_BattleSkill skillItem = AssetDatabase.LoadAssetAtPath<SO_BattleSkill>(item);
                if (skillItem != null)
                    exists.Add(skillItem.name, skillItem);
            }

            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(string.Format("{0}/BattleSkill.json", JsonFolder));
            var items = JsonMapper.ToObject(textAsset.text);
            if (items.IsArray)
            {
                foreach (var item in items)
                {
                    JsonData itemData = item as JsonData;
                    if (itemData == null)
                        continue;

                    string objName = ParseFromJson(itemData, "objName", string.Empty);
                    SO_BattleSkill assetItem = null;
                    string assetPath = string.Format("{0}/{1}.asset", SO_SkillFolder, objName);
                    //尝试从存在的列表中获取
                    if (exists.ContainsKey(objName))
                    {
                        assetItem = exists[objName];
                        //移除，后面要删除没有找到的
                        exists.Remove(objName);
                    }
                    else
                    {
                        assetItem = ScriptableObject.CreateInstance<SO_BattleSkill>();
                        AssetDatabase.CreateAsset(assetItem, assetPath);
                        newList.Add(objName);
                    }

                    assetItem.skillID = ParseFromJson(itemData, "ID", 0);
                    assetItem.skillName = ParseFromJson(itemData, "skillName", string.Empty);
                    assetItem.effectRadius = ParseFromJson(itemData, "effectRadius", 0);
                    assetItem.releaseRadius = ParseFromJson(itemData, "releaseRadius", 0);
                    assetItem.rageLevel = ParseFromJson(itemData, "rageLevel", 0f);
                    assetItem.hatredMultiple = ParseFromJson(itemData, "hatredMultiple", 0f);
                    assetItem.energyCost = ParseFromJson(itemData, "energyCost", 0);
                    assetItem.mainValue = ParseFromJson(itemData, "mainValue", 0);

                    //类型
                    string targetType = ParseFromJson(itemData, "targetType", string.Empty);
                    if (string.Compare(targetType, "BattleUnit", true) == 0)
                        assetItem.targetType = BattleSkillTargetType.BattleUnit;
                    else if (string.Compare(targetType, "GridUnit", true) == 0)
                        assetItem.targetType = BattleSkillTargetType.GridUnit;
                    else if (string.Compare(targetType, "Self", true) == 0)
                        assetItem.targetType = BattleSkillTargetType.Self;
                    else
                    {
                        Debug.LogError(string.Format("未知的目标类型 => {0} | {1} | {2}", targetType, assetItem.skillID, assetItem.skillName));
                    }

                    //类型
                    string damageType = ParseFromJson(itemData, "damageType", string.Empty);
                    if (string.Compare(damageType, "Physical", true) == 0)
                        assetItem.damageType = BattleSkillDamageType.Physical;
                    else if (string.Compare(damageType, "Magic", true) == 0)
                        assetItem.damageType = BattleSkillDamageType.Magic;
                    else if (string.Compare(damageType, "Heal", true) == 0)
                        assetItem.damageType = BattleSkillDamageType.Heal;
                    else
                    {
                        Debug.LogError(string.Format("未知的伤害类型 => {0} | {1} | {2}", damageType, assetItem.skillID, assetItem.skillName));
                    }

                    EditorUtility.SetDirty(assetItem);
                }
            }

            //需要删除没有的
            if (exists.Count > 0)
            {
                if (EditorUtility.DisplayDialog("警告", string.Format("存在{0}个配置外的技能, 是否删除", exists.Keys.Count), "是", "否"))
                {
                    removeList = new List<string>(exists.Keys);
                    foreach (var item in exists)
                    {
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(item.Value));
                    }
                    exists.Clear();
                }
            }

            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
            stringBuilder.AppendFormat("技能生成结束，新增:{0},删除{1}\n", newList.Count, removeList.Count);
            stringBuilder.AppendFormat("  新增:\n");
            for (int i = 0; i < newList.Count; i++)
            {
                stringBuilder.AppendFormat("    {0}.{1}\n", i, newList[i]);
            }

            stringBuilder.AppendFormat("  删除:\n");
            for (int i = 0; i < removeList.Count; i++)
            {
                stringBuilder.AppendFormat("    {0}.{1}\n", i, removeList[i]);
            }
            Debug.Log(stringBuilder.ToString());
        }
    }
}