//战场显示器
//同时只有一个战场会被显示


//#define TEST_NAV
//#define TEST_RANGE
//#define TEST_REMOTE_RANGE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ELGame
{
    using Resource;

    public class BattleFieldRenderer
        : MonoBehaviourSingleton<BattleFieldRenderer>,
          IVisualRenderer<BattleField, BattleFieldRenderer>
    {
        //当前显示的战斗信息
        public BattleField battleField; //战场数据
        private int currentActionIndex = 0;
        public Camera battleCamera;     //渲染战斗的相机

        //格子的模型，用来clone格子拼成地图
        [SerializeField] private Transform gridUnitsRoot;

        //战斗单位的模型
        [SerializeField] private Transform battleUnitsRoot;

        //用来管理创建出来的对象
        private List<GridUnitRenderer> gridRenderersPool = new List<GridUnitRenderer>();            //格子
        private List<BattleUnitRenderer> battleUnitRenderersPool = new List<BattleUnitRenderer>();  //战斗单位

        //Helper:将战场显示器的部分功能分出去写
        private BattleFieldManualOperationHelper manualOperationHelper;     //手动操作的Helper

        private bool touch_0_valid = false; //第一次触碰是否有效

        //初始化
        public void Init()
        {
            if (gridUnitsRoot == null || battleUnitsRoot == null)
            {
                UtilityHelper.LogError("Init battle field renderer failed!");
                return;
            }

            //初始化Helper
            manualOperationHelper = new BattleFieldManualOperationHelper(this);

            //创建一定数量的格子和战斗单位渲染器，留作后面使用
            InitGridUnitRenderer(100);
            InitBattleUnitRenderer(10);

            BattleManager.Instance.MgrLog("Battle field renderer inited.");

            //战场显示器初始化完成，通知回调
            EventManager.Instance.Run(EGameConstL.EVENT_BATTLE_FIELD_RENDERER_READY, null);
        }

        private void InitGridUnitRenderer(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                CreateGridUnitRenderer();
            }
        }

        private void InitBattleUnitRenderer(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                CreateBattleUnitRenderer();
            }
        }

        //刷新格子
        private void RefreshBattleMapGrids()
        {
            if (battleField == null)
            {
                UtilityHelper.LogError("Prepare battle map failed. No battle data.");
                return;
            }

            for (int r = 0; r < battleField.battleMap.mapHeight; ++r)
            {
                for (int c = 0; c < battleField.battleMap.mapWidth; ++c)
                {
                    GridUnit gridUnitData = battleField.battleMap.mapGrids[c, r];
                    if (gridUnitData != null)
                    {
                        //创建一个用于显示的格子对象
                        GridUnitRenderer gridUnitRenderer = GetUnusedGridUnitRenderer();
                        if (gridUnitRenderer != null)
                            gridUnitData.ConnectRenderer(gridUnitRenderer);
                    }
                }
            }
        }

        //刷新战斗单位
        private void RefreshBattleUnits()
        {
            if (battleField == null)
            {
                UtilityHelper.LogError("Prepare battle units failed. No battle data.");
                return;
            }

            for (int i = 0; i < battleField.teams.Count; ++i)
            {
                BattleTeam team = battleField.teams[i];
                if (team.battleUnits != null)
                {
                    foreach (var battleUnitData in team.battleUnits)
                    {
                        BattleUnitRenderer battleUnitRenderer = GetUnusedBattleUnitRenderer();
                        battleUnitRenderer.teamColor = i == 0 ? TeamColor.Blue : TeamColor.Red;
                        if (battleUnitRenderer != null)
                            battleUnitData.ConnectRenderer(battleUnitRenderer);
                    }
                }
            }
        }

        //创建格子
        private GridUnitRenderer CreateGridUnitRenderer()
        {
            var clone = ClonePrefab("prefabs/battlemapunits/gridunit.unity3d", "gridunit");
            GridUnitRenderer renderer = clone.GetComponent<GridUnitRenderer>();
            clone.transform.SetParent(gridUnitsRoot);
            clone.transform.SetUnused(false, EGameConstL.STR_Grid);
            renderer.Init();
            gridRenderersPool.Add(renderer);
            return renderer;
        }

        //创建战斗单位
        private BattleUnitRenderer CreateBattleUnitRenderer()
        {
            var clone = ClonePrefab("prefabs/battlemapunits/battleunit.unity3d", "battleunit");
            BattleUnitRenderer renderer = clone.GetComponent<BattleUnitRenderer>();
            clone.transform.SetParent(battleUnitsRoot);
            clone.transform.SetUnused(false, EGameConstL.STR_BattleUnit);
            renderer.Init();
            battleUnitRenderersPool.Add(renderer);
            return renderer;
        }

        //获取没有使用的格子渲染器
        private GridUnitRenderer GetUnusedGridUnitRenderer()
        {
            for (int i = 0; i < gridRenderersPool.Count; ++i)
            {
                if (!gridRenderersPool[i].gameObject.activeSelf)
                    return gridRenderersPool[i];
            }
            return CreateGridUnitRenderer();
        }

        //获取没有使用的战斗单位渲染器
        private BattleUnitRenderer GetUnusedBattleUnitRenderer()
        {
            for (int i = 0; i < battleUnitRenderersPool.Count; ++i)
            {
                if (!battleUnitRenderersPool[i].gameObject.activeSelf)
                    return battleUnitRenderersPool[i];
            }
            return CreateBattleUnitRenderer();
        }

        private void OnBattleUnitUseItem(IGameEvent gameEvent)
        {
            if (gameEvent == null)
                return;

            if (gameEvent.Name.Equals(EGameConstL.EVENT_BATTLE_UNIT_USE_ITEM))
            {
                PackageItem item = gameEvent.Body as PackageItem;
                if (item != null)
                {

                }
            }
        }

        //战场连接
        public void OnConnect(BattleField field)
        {
            battleField = field;
            //加载战场
            RefreshBattleMapGrids();
            //加载战斗单位
            RefreshBattleUnits();

            EventManager.Instance.Register(EGameConstL.EVENT_BATTLE_UNIT_USE_ITEM,
                this.gameObject.RequestorSTR(),
                OnBattleUnitUseItem);
        }

        //战场取消连接
        public void OnDisconnect()
        {
            if (battleField != null)
            {
                battleField = null;
            }
            EventManager.Instance.Unregister(this.gameObject.RequestorSTR());
        }

        public void RefreshRenderer()
        {
            for (int r = 0; r < battleField.battleMap.mapHeight; ++r)
            {
                for (int c = 0; c < battleField.battleMap.mapWidth; ++c)
                {
                    GridUnit gridUnitData = battleField.battleMap.mapGrids[c, r];
                    if (gridUnitData != null && gridUnitData.gridUnitRenderer != null)
                    {
                        gridUnitData.gridUnitRenderer.RefreshRenderer();
                    }
                }
            }

            for (int i = 0; i < battleField.teams.Count; ++i)
            {
                BattleTeam team = battleField.teams[i];
                if (team.battleUnits != null)
                {
                    foreach (var battleUnitData in team.battleUnits)
                    {
                        if (battleUnitData.battleUnitRenderer != null)
                        {
                            battleUnitData.battleUnitRenderer.RefreshRenderer();
                        }
                    }
                }
            }
        }

        private void Update()
        {
            if (battleField == null)
                return;

            UpdateBattleFieldTouched();
        }

        private void MouseOperation()
        {
            //如果点击了鼠标左键
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    //点中了UI
                    UtilityHelper.Log("点中了UI ->" + UnityEngine.EventSystems.EventSystem.current.gameObject.name);
                    return;
                }
                ClickedBattleField(Input.mousePosition);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                //右键点击为取消
                manualOperationHelper.ClickedCancel();
            }
        }

        private void TouchOperation()
        {
            if (Input.touchCount > 0)
            {
                Touch touch_0 = Input.GetTouch(0);
                switch (touch_0.phase)
                {
                    case TouchPhase.Began:
                        if (!EventSystem.current.IsPointerOverGameObject(touch_0.fingerId))
                            touch_0_valid = true;
                        break;

                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        break;

                    case TouchPhase.Ended:
                        if (touch_0_valid)
                        {
                            ClickedBattleField(touch_0.position);
                            touch_0_valid = false;
                        }
                        break;

                    case TouchPhase.Canceled:
                        touch_0_valid = false;
                        break;

                    default:
                        break;
                }
            }
        }

        //获取战场点击的情况
        private void UpdateBattleFieldTouched()
        {
#if UNITY_EDITOR
            MouseOperation();
#else
            TouchOperation();
#endif
        }

        //通过屏幕点击了战场
        private void ClickedBattleField(Vector3 screenPosition)
        {
            //计算点击位置
            Vector3 clickedWorldPos = battleCamera.ScreenToWorldPoint(screenPosition);
            clickedWorldPos.z = 0;
            //判断是否有格子被点中？
            GridUnitRenderer clicked = GetGridClicked(clickedWorldPos);
            if (clicked != null && clicked.gridUnit != null)
            {
                //发生点击喽~
                OnBattleUnitAndGridTouched(clicked.gridUnit, clicked.gridUnit.battleUnit);
            }
            else
            {
                //点到了地图外，关闭所有弹出层界面
                UIViewManager.Instance.HideViews(UIViewLayer.Popup);
            }
        }

        //根据点击位置获取点中的格子
        private GridUnitRenderer GetGridClicked(Vector3 clickedWorldPos)
        {
            //转换空间到格子组织节点(GridUnits)的空间
            clickedWorldPos = gridUnitsRoot.transform.InverseTransformPoint(clickedWorldPos);
            //初步判定所在行列
            int row = Mathf.FloorToInt((clickedWorldPos.y - EGameConstL.Map_GridOffsetY * 0.5f) / -EGameConstL.Map_GridOffsetY);
            int column = Mathf.FloorToInt((clickedWorldPos.x + EGameConstL.Map_GridWidth * 0.5f - ((row & 1) == (EGameConstL.Map_FirstRowOffset ? 1 : 0) ? 0f : (EGameConstL.Map_GridWidth * 0.5f))) / EGameConstL.Map_GridWidth);

            int testRow = 0;
            int testColumn = 0;
            //二次判定，判定周围格子
            GridUnitRenderer clickedGrid = null;
            float minDis = Mathf.Infinity;
            for (int r = -1; r <= 1; ++r)
            {
                for (int c = -1; c <= 1; ++c)
                {
                    testRow = row + r;
                    testColumn = column + c;
                    if (testRow < 0 || testRow >= battleField.battleMap.mapHeight
                        || testColumn < 0 || testColumn >= battleField.battleMap.mapWidth)
                    {
                        continue;
                    }
                    float distance = UtilityHelper.CalcDistanceInXYAxis(clickedWorldPos, battleField.battleMap.mapGrids[testColumn, testRow].localPosition);
                    if (distance < minDis && distance < EGameConstL.Map_HexRadius)
                    {
                        minDis = distance;
                        clickedGrid = battleField.battleMap.mapGrids[testColumn, testRow].gridUnitRenderer;
                    }
                }
            }
            return clickedGrid;
        }

        //点击了地块、战斗单位
        private void OnBattleUnitAndGridTouched(GridUnit gridTouched, BattleUnit battleUnitTouched)
        {
#if TEST_NAV
            Test_Nav(gridTouched);
            return;
#endif
#if TEST_RANGE
            Test_Range(gridTouched);
            return;
#endif
#if TEST_REMOTE_RANGE
            Test_RemoteRange(gridTouched);
            return;
#endif
            if (battleUnitTouched != null && battleUnitTouched.battleBehaviourSystem != null)
            {
                battleUnitTouched.battleBehaviourSystem.Think();
            }

            UIViewManager.Instance.ShowView(UIViewName.BattleFieldUnitInfo, gridTouched, battleUnitTouched);

            //通知helper处理点击反馈逻辑
            manualOperationHelper.OnBattleUnitAndGridTouched(gridTouched, battleUnitTouched);
        }

        //设置手动操作的英雄
        public void SetManualBattleUnit(BattleUnitRenderer operatingBattleUnit)
        {
            manualOperationHelper.ManualOperatingBattleUnitRenderer = operatingBattleUnit;
        }

        //某个战斗单位点击了移动
        public void BattleUnitMove(BattleUnit battleUnit)
        {
            manualOperationHelper.BattleUnitMove(battleUnit);
        }

        //某个战斗单位点击了待命
        public void BattleUnitStay(BattleUnit battleUnit)
        {
            manualOperationHelper.BattleUnitStay(battleUnit);
        }

        //某个战斗单位点击了使用技能
        public void BattleUnitUseSkill(BattleUnit battleUnit, SO_BattleSkill skill)
        {
            manualOperationHelper.BattleUnitUseSkill(battleUnit, skill);
        }

        public void BattleUnitUseItem(BattleUnit battleUnit, SO_PackageItem item, int count)
        {
            manualOperationHelper.BattleUnitUseItem(battleUnit, item, count);
        }

        //设置某个圆形区域的显示状态
        public void SetCircularRangeRenderStateActive(bool active, GridRenderType gridRenderType, int centerRow = -1, int centerColumn = -1, int radius = -1)
        {
            manualOperationHelper.SetCircularRangeRenderStateActive(active, gridRenderType, centerRow, centerColumn, radius);
        }

        //设置路径显示状态
        public void SetGridsRenderStateActive(bool active, GridUnit[] gridPath = null)
        {
            manualOperationHelper.SetGridsRenderStateActive(active, gridPath);
        }

        //播放战场动作
        private IEnumerator PlayBattleByCoroutine(System.Action callback)
        {
            if (battleField == null
                || battleField.battleFieldEvents == null
                || battleField.battleFieldEvents.Count == 0)
            {
                UtilityHelper.LogError(string.Format("Play battle action failed. -> {0}", battleField.ID));
                yield break;
            }

            //遍历所有战斗动作
            while (currentActionIndex < battleField.battleFieldEvents.Count)
            {
                //一个英雄行动事件
                if (battleField.battleFieldEvents[currentActionIndex] is BattleUnitActionEvent)
                {
                    BattleUnitActionEvent actionEvent = (BattleUnitActionEvent)battleField.battleFieldEvents[currentActionIndex];

                    //有对应的战斗单位，且这个战斗单位已经连接了战斗单位渲染器
                    if (actionEvent.actionUnit != null && actionEvent.actionUnit.battleUnitRenderer != null)
                    {
                        yield return actionEvent.actionUnit.battleUnitRenderer.RunHeroAction(actionEvent);
                    }
                }
                //一个格子事件
                else if (battleField.battleFieldEvents[currentActionIndex] is GridUnitEvent)
                {
                    GridUnitEvent gridUnitEvent = (GridUnitEvent)battleField.battleFieldEvents[currentActionIndex];
                    if (gridUnitEvent.grid != null && gridUnitEvent.grid.gridUnitRenderer != null)
                    {
                        yield return gridUnitEvent.grid.gridUnitRenderer.RunGridEvent(gridUnitEvent);
                    }
                }
                ++currentActionIndex;
            }

            if (callback != null)
                callback();
        }

        //播放战斗(异步的方式)
        public void PlayBattle(System.Action callback)
        {
            StartCoroutine(PlayBattleByCoroutine(callback));
        }

        //战斗结束
        public void BattleEnd()
        {
            var viewMain = UIViewManager.Instance.GetViewByName<UIViewMain>(UIViewName.Main);
            if (viewMain != null)
                viewMain.ShowBattleEnd();
        }

        public void ResetBattleField()
        {
            StopAllCoroutines();
            battleField.ResetBattle();
            currentActionIndex = 0;
            SetCircularRangeRenderStateActive(false, GridRenderType.SkillEffectRange);
            SetGridsRenderStateActive(false);
            battleField.Run();
        }


#if TEST_NAV
        List<GridUnit> lastPath = new List<GridUnit>();
        List<GridUnit> lastSearched = new List<GridUnit>();
        GridUnit start;
        GridUnit end;
        //点击了地块、战斗单位
        private void Test_Nav(GridUnit gridTouched)
        {
            if (start == null)
            {
                start = gridTouched;
                start.gridUnitRenderer.AppendGridRenderType(GridRenderType.Start);
            }
            else if (end == null)
            {
                end = gridTouched;
                end.gridUnitRenderer.AppendGridRenderType(GridRenderType.End);

                //Nav

                UtilityHelper.TimerStart();
                for (int i = 0; i < 1000; i++)
                {
                    //SetCircularRangeRenderStateActive(true, GridRenderType.SkillEffectRange, start.row, start.column, 3);
                    MapNavigator.Instance.Navigate(battleField.battleMap,
                                                  start,
                                                  end,
                                                  lastPath, null, -1, 2);
                }

                Debug.Log("TimeCost:" + UtilityHelper.TimerEnd());
                //foreach (var item in lastSearched)
                //{
                //    item.gridUnitRenderer.AppendGridRenderType(GridRenderType.Searched);
                //}
                foreach (var item in lastPath)
                {
                    item.gridUnitRenderer.AppendGridRenderType(GridRenderType.Path);
                }
            }
            else
            {
                start.gridUnitRenderer.ResetGridRenderType();
                end.gridUnitRenderer.ResetGridRenderType();
                foreach (var item in lastPath)
                {
                    item.gridUnitRenderer.ResetGridRenderType();
                }
                foreach (var item in lastSearched)
                {
                    item.gridUnitRenderer.ResetGridRenderType();
                }
                start = null;
                end = null;
                SetCircularRangeRenderStateActive(false, GridRenderType.SkillEffectRange);
            }
        }
#endif

#if TEST_RANGE
        private List<GridUnit> lastUnits = new List<GridUnit>();
        private void Test_Range(GridUnit gridTouched)
        {

            if(lastUnits.Count > 0)
                foreach (var item in lastUnits)
                {
                    item.gridUnitRenderer.ResetGridRenderType();
                }

            UtilityHelper.TimerStart();
            for (int i = 0; i < 100; i++)
            {
                battleField.battleMap.GetCircularGrids(gridTouched.row, gridTouched.column, 3, 0, false, lastUnits);
            }

            Debug.Log("TimeCost:" + UtilityHelper.TimerEnd());
            foreach (var item in lastUnits)
            {
                item.gridUnitRenderer.AppendGridRenderType(GridRenderType.MoveRange);
            }
        }
#endif

#if TEST_REMOTE_RANGE
        GridUnit releaserGrid;
        GridUnit targetGrid;
        List<GridUnit> releaseRange = new List<GridUnit>();
        List<GridUnit> skillRange = new List<GridUnit>();
        int motionRadius = 0;
        int effectRadius = 2;

        public void Test_RemoteRange(GridUnit gridUnit)
        {
            if (releaserGrid == null)
            {
                releaserGrid = gridUnit;
                releaserGrid.gridUnitRenderer.AppendGridRenderType(GridRenderType.Start);

                battleField.battleMap.GetCircularGrids(releaserGrid.row, releaserGrid.column,
                    motionRadius, 0, true, releaseRange);

                foreach (var item in releaseRange)
                {
                    item.gridUnitRenderer.AppendGridRenderType(GridRenderType.SkillReleaseRange);
                }
            }
            else if (targetGrid == null)
            {
                UtilityHelper.TimerStart();

                targetGrid = gridUnit;
                targetGrid.gridUnitRenderer.AppendGridRenderType(GridRenderType.End);

                for (int i = 0; i < 1000; ++i)
                {
                    battleField.battleMap.GetCircularGrids(releaserGrid.row, releaserGrid.column,
                        motionRadius, 0, true, skillRange, delegate (GridUnit _gridUnit)
                        {
                            return _gridUnit.Distance(targetGrid) <= effectRadius;
                        });
                }

                foreach (var item in skillRange)
                {
                    item.gridUnitRenderer.AppendGridRenderType(GridRenderType.SkillEffectRange);
                }

                UtilityHelper.Log("Test_RemoteRange cost:" + UtilityHelper.TimerEnd());
            }
            else
            {
                releaserGrid.gridUnitRenderer.ResetGridRenderType();
                targetGrid.gridUnitRenderer.ResetGridRenderType();
                foreach (var item in releaseRange)
                {
                    item.gridUnitRenderer.ResetGridRenderType();
                }
                foreach (var item in skillRange)
                {
                    item.gridUnitRenderer.ResetGridRenderType();
                }
                releaserGrid = null;
                targetGrid = null;
                releaseRange.Clear();
                skillRange.Clear();
            }
        }
#endif

    }
}