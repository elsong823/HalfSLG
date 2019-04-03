
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class Main
        : MonoBehaviourSingleton<Main>
    {
        //初始化各个管理器
        private void PrepareManager()
        {
            //事件
            EventManager.Instance.InitManager();
            
            //界面
            UIViewManager.Instance.InitManager();
            
            //战斗相关
            BattleManager.Instance.InitManager();       //主
            BattleSkillManager.Instance.InitManager();  //战斗技能

            //特效管理器
            EffectManager.Instance.InitManager();

            
        }

        private void Start()
        {
            UtilityHelper.Log("Main start.");

            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

            //准备管理器
            PrepareManager();

        }
        
        private void OnDestroy()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
        }
    }
}