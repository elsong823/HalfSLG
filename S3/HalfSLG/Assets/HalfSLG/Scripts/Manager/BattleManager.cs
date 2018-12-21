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

        private bool battleFiledRendererIsReady = false;

        protected override void InitManager()
        {
            base.InitManager();

            //初始化战场数据创建器
            var battleCreater = BattleCreator.Instance;
            //战斗计算器
            var battleCalculator = BattleCalculator.Instance;
            //初始化战场显示器
            BattleFieldRenderer.Instance.Init(OnBattleFieldReady);

            UtilityHelper.Log("Battle manager inited.");
        }

        private void OnBattleFieldReady()
        {
            battleFiledRendererIsReady = true;
            UtilityHelper.Log("Battle field renderer ready.");
        }
        
        private void Update()
        {
        }

        private void OnGUI()
        {
            if (!battleFiledRendererIsReady)
                return;

            if (singleBattle == null)
            {
                if (GUI.Button(new Rect(0, 0, 150, 100), "Create battle"))
                {
                    if (testCount > 0)
                    {
                        StartCoroutine(RunTest());
                        return;
                    }
                    //创建战斗(数据)
                    singleBattle = BattleCreator.Instance.CreateBattle(
                        mapSize.x,mapSize.y, 
                        obstacleCount, obstacleGap, 
                        battleUnitCount);

                    //重置相机和尺寸
                    ResetBattleCamera();

                    //战斗计算(保留战斗过程)
                    singleBattle.Run(true);

                    //连接到显示器
                    singleBattle.ConnectRenderer(BattleFieldRenderer.Instance);

                    //显示战斗过程
                    StartCoroutine(BattleFieldRenderer.Instance.PlayBattleActions());
                }
            }
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

        public int testCount = 10;

        private IEnumerator RunTest()
        {
            for (int i = 0; i < testCount; ++i)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayProgressBar("战斗计算", string.Format("战斗场次计算进度 {0}/{1}", i + 1, testCount), (i + 1) * 1f / testCount);
#endif
                //创建战斗(数据)
                singleBattle = BattleCreator.Instance.CreateBattle(
                    mapSize.x, mapSize.y,
                    obstacleCount, obstacleGap,
                    battleUnitCount);

                var state = singleBattle.Run(true);

                //如果运算出现问题，播放一下有问题的场
                if (state == -1)
                {
                    singleBattle.ConnectRenderer(BattleFieldRenderer.Instance);
                    yield return BattleFieldRenderer.Instance.PlayBattleActions();
                    yield return EGameConstL.WaitForTouchScreen;
                }

                yield return null;
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
        }
    }
}