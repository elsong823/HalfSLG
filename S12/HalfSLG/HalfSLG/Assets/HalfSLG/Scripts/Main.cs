using UnityEngine;

namespace ELGame
{
    using Resource;
    using UnityEngine.U2D;

    public class Main
        : MonoBehaviourSingleton<Main>
    {
        //初始化各个管理器
        private void PrepareBaseManager()
        {
            //事件
            EventManager.Instance.InitManager();

            SceneManager.Instance.InitManager();

            //注册事件
            EventManager.Instance.Register(EGameConstL.EVENT_RESOURCE_MANAGER_READY, this.gameObject.RequestorSTR(), StartGame, 1);

            //资源
            ResourceManager.Instance.InitManager();
        }

        private void PrepareViewManager()
        {
            GameObject viewRoot = ClonePrefab("prefabs/uiview/viewroot.unity3d", "viewroot");
            if (viewRoot)
            {
                viewRoot.transform.SetParent(transform);
                viewRoot.transform.Normalize();
                viewRoot.transform.CleanName();
                viewRoot.SetActive(true);
            }
        }

        private void PrepareBattle()
        {
            Debug.Log("Prepare Battle");
            //界面
            UIViewManager.Instance.InitManager();

            //战斗相关
            BattleManager.Instance.InitManager();       //主
            BattleSkillManager.Instance.InitManager();  //战斗技能

            //特效管理器
            EffectManager.Instance.InitManager();

            //道具
            PackageItemManager.Instance.InitManager();
        }

        private void SceneLoading(float progress)
        {

        }

        private void SceneLoaded(string sceneName)
        {
            if(sceneName.ToLower().Contains("scenebattle"))
                PrepareBattle();
        }

        private void StartGame(IGameEvent e)
        {
            PrepareViewManager();
            SceneManager.Instance.LoadSceneAsync("scenebattle", SceneLoading, SceneLoaded);
        }

        private void Start()
        {
            //先自保
            DontDestroyOnLoad(this);

            UtilityHelper.Log("Main start.");

            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

            //准备管理器
            PrepareBaseManager();
        }
        
        private void OnDestroy()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
        }
    }
}