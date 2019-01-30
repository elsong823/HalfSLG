using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BattleManager
        : BaseManager<BattleManager>
    {
        //地图尺寸
        [SerializeField] Vector2Int mapSize;
        //障碍数量
        [SerializeField] int obstacleCount;
        //障碍之间的空隙
        [SerializeField] int obstacleGap;
        //单方对战单位数量
        [SerializeField] int battleUnitCount;

        [SerializeField] Camera battleCamera;

        private BattleField singleBattle;

        public bool manual = false;
        private bool battleFiledRendererIsReady = false;

        protected override void InitManager()
        {
            base.InitManager();

            //初始化与战斗有关的单例控制器
            var battleCreater = BattleCreator.Instance;         //初始化战场数据创建器
            var battleCalculator = BattleCalculator.Instance;   //战斗计算器
            var mapNavigator = MapNavigator.Instance;           //导航器

            //初始化战场显示器
            BattleFieldRenderer.Instance.Init(OnBattleFieldReady);

            UtilityHelper.Log("Battle manager inited.");
        }
        
        private void OnBattleFieldReady()
        {
            battleFiledRendererIsReady = true;
            UtilityHelper.Log("Battle field renderer ready.");

            UIViewMain viewMain = UIViewManager.Instance.GetViewByName<UIViewMain>(UIViewName.Main);
            if (viewMain)
                viewMain.BattleFieldReady();
        }
        
        private void Update()
        {
        }

        private void OnGUI()
        {
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

        //public int testCount = 10;

//        private IEnumerator RunTest()
//        {
//            for (int i = 0; i < testCount; ++i)
//            {
//#if UNITY_EDITOR
//                UnityEditor.EditorUtility.DisplayProgressBar("战斗计算", string.Format("战斗场次计算进度 {0}/{1}", i + 1, testCount), (i + 1) * 1f / testCount);
//#endif
//                //创建战斗(数据)
//                singleBattle = BattleCreator.Instance.CreateBattle(
//                    mapSize.x, mapSize.y,
//                    obstacleCount, obstacleGap,
//                    battleUnitCount);

//                singleBattle.Run();

//                //如果运算出现问题，播放一下有问题的场
//                if (singleBattle.battleState == BattleState.Exception)
//                {
//                    singleBattle.ConnectRenderer(BattleFieldRenderer.Instance);
//                    yield return BattleFieldRenderer.Instance.PlayBattleByCoroutine(null);
//                    yield return EGameConstL.WaitForTouchScreen;
//                }

//                yield return null;
//            }
//#if UNITY_EDITOR
//            UnityEditor.EditorUtility.ClearProgressBar();
//#endif
//        }

        public void RunManualTest()
        {
            //创建战斗(数据)
            singleBattle = BattleCreator.Instance.CreateBattle(
                mapSize.x, mapSize.y,
                obstacleCount, obstacleGap,
                battleUnitCount);

            //重置相机和尺寸
            ResetBattleCamera();

            singleBattle.ConnectRenderer(BattleFieldRenderer.Instance);

            singleBattle.Run();
            
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