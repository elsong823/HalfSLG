using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
#if UNITY_EDITOR
using UnityEditor;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(SO_UIViewConfig))]
    public class SO_UIViewConfigEditor
        : Editor
    {
        public override void OnInspectorGUI()
        {
            //常规显示
            base.OnInspectorGUI();

            serializedObject.Update();

            if (GUI.changed)
            {
                //修正唯一按钮
                var pUnique = serializedObject.FindProperty("unique");
                var pCacheScheme = serializedObject.FindProperty("cacheScheme");
                //如果选择的是常驻内存，则必须勾选唯一
                if (pCacheScheme.enumValueIndex == 2 && pUnique.boolValue == false)
                {
                    //提醒一下
                    EditorUtility.DisplayDialog("Warning", "Cache scheme view must be UNIQUE !", "OK");
                    pUnique.boolValue = true;
                }

                serializedObject.ApplyModifiedProperties();
            }
        }
    }

#endif

    //添加后需要在初始化时注册
    public enum UIViewName
    {
        None,

        Debug,
        Main,                       //主界面
        BattleFieldPlayerActOption, //玩家操作选择栏
        BattleFieldUnitInfo,        //显示战斗单位信息
    }

    //添加后需要在初始化时注册
    public enum UIViewLayer
    {
        None,

        Background,
        Base,
        Popup,
        Top,
        Debug,
    }

    public enum UIViewCacheScheme
    {
        AutoRemove,         //自动移除
        TempCache,           //关闭后进入临时缓冲池
        Cache,              //关闭后常驻内存
    }

    [CreateAssetMenu(menuName = "ScriptableObject/UI view config")]
    public class SO_UIViewConfig 
        : ScriptableObject
    {
        public bool unique = true;                  //是否唯一打开
        public UIViewName viewName;                 //界面名称
        public UIViewLayer viewLayer;               //所在层
        public UIViewCacheScheme cacheScheme;       //缓存策略  
        public string assetName;                    //资源的名称
        public bool coverScreen;                    //是否遮挡了整个屏幕
        public bool alwaysUpdate;                   //被遮挡后是否需要更新
        public bool bgTriggerClose;                 //点击了背景是否关闭界面
        
        public string BundleName
        {
            get
            {
                return string.Format("prefabs/uiview/{0}", assetName);
            }
        }
    }
}