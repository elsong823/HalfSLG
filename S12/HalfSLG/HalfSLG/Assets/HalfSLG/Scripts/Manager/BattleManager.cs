using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ELGame
{
    public class BattleManager
        : BaseManager<BattleManager>
    {
        public override string MgrName => "BattleManager";

        [Header("地图尺寸")]
        [SerializeField] Vector2Int mapSize;
        [Header("障碍物的数量")]
        [SerializeField] int obstacleCount;
        [Header("障碍物之间的间隙")]
        [SerializeField] int obstacleGap;
        [Header("buff数量")]
        [SerializeField] int buffCount;
        [Header("道具数量")]
        [SerializeField] int itemCount;

        [SerializeField] Camera battleCamera;

        [SerializeField] int battleTestCount;
        [SerializeField] List<SO_BattleUnitAttribute> teamA;
        [SerializeField] List<SO_BattleUnitAttribute> teamB;

        private BattleField singleBattle;

        [HideInInspector]
        public string brPath = string.Empty;
        
        public override void InitManager()
        {
            base.InitManager();
            
            //初始化与战斗有关的单例控制器
            BattleCalculator.Instance.Init();           //战斗计算器
            MapNavigator.Instance.Init();               //导航器
            BattleFieldRenderer.Instance.Init();        //初始化战场显示器

#if UNITY_EDITOR
            //收集战斗数据

            //保存路径
            string brFolder = UtilityHelper.ConvertToObsPath(string.Format("{0}/../BR", Application.dataPath));
            if (!Directory.Exists(brFolder))
                Directory.CreateDirectory(brFolder);

            brPath = string.Format("{0}/{1}_{2}_{3}_{4}_{5}.csv",
                brFolder,
                System.DateTime.Now.Month,
                System.DateTime.Now.Day,
                System.DateTime.Now.Hour,
                System.DateTime.Now.Minute,
                System.DateTime.Now.Second);
#endif
        }
        
        private void ResetBattleCamera()
        {
            if (battleCamera)
            {
                battleCamera.orthographic = true;
                battleCamera.transform.position = new Vector3(
                    mapSize.x * EGameConstL.Map_GridWidth * 0.5f,
                    -mapSize.y * EGameConstL.Map_GridOffsetY * 0.5f,
                    -10
                    );
                battleCamera.orthographicSize = mapSize.x * EGameConstL.Map_GridWidth * 0.5f;
            }
        }
        
        private IEnumerator RunTest()
        {
            //创建战斗(数据)
            singleBattle = BattleFieldCreator.Instance.Create(
                mapSize.x, mapSize.y,
                obstacleCount, obstacleGap, buffCount, itemCount,
                teamA, teamB);

            for (int i = 0; i < battleTestCount; ++i)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("战斗计算", string.Format("战斗场次计算进度 {0}/{1}", i + 1, battleTestCount), (i + 1) * 1f / battleTestCount);
#endif

                singleBattle.ResetBattle();

                singleBattle.Run();

                //如果运算出现问题，播放一下有问题的场
                if (singleBattle.battleState == BattleState.Exception)
                {
                    singleBattle.ConnectRenderer(BattleFieldRenderer.Instance);
                    BattleFieldRenderer.Instance.PlayBattle(null);
                    yield return EGameConstL.WaitForTouchScreen;
                }

                yield return null;
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
        }

        public void RunManualTest()
        {
            //创建战斗(数据)
            singleBattle = BattleFieldCreator.Instance.Create(
                mapSize.x, mapSize.y,
                obstacleCount, obstacleGap, buffCount, itemCount,
                teamA, teamB);

            //重置相机和尺寸
            ResetBattleCamera();

            singleBattle.ConnectRenderer(BattleFieldRenderer.Instance);

            singleBattle.Run();
            
        }

        public void RunAutoTest()
        {
            //关闭调试bbsys的日志输出
            DebugHelper.Instance.debugBBSys = false;
            StartCoroutine(RunTest());
        }

        private IEnumerator PlayerAction()
        {
            if (singleBattle != null)
                singleBattle.battleState = BattleState.Fighting;

            yield return EGameConstL.WaitForTouchScreen;
            yield return null;
        }
    }
}