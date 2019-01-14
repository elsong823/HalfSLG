//战场显示器
//同时只有一个战场会被显示

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public enum ManualOperationState
    {
        None,
        Move,
        Skill,
    }

    public class BattleFieldRenderer
        : MonoBehaviourSingleton<BattleFieldRenderer>,
          IVisualRenderer<BattleField, BattleFieldRenderer>
    {
        //当前显示的战斗信息
        private BattleField battleField;
        private Camera battleCamera;
        public Camera BattleCamera
        {
            get
            {
                if (!battleCamera)
                {
                    var objCamera = GameObject.FindGameObjectWithTag(EGameConstL.Tag_BattleCamera);
                    if (!objCamera)
                    {
                        UtilityHelper.LogError("Error: Can not find battle camera!");
                        return null;
                    }
                    battleCamera = objCamera.GetComponent<Camera>();
                }
                return battleCamera;
            }
        }

        //格子的模型，用来clone格子拼成地图
        [SerializeField] private GridUnitRenderer gridUnitModel;
        [SerializeField] private Transform gridUnitsRoot;

        //战斗单位的模型
        [SerializeField] private BattleUnitRenderer battleUnitModel;
        [SerializeField] private Transform battleUnitsRoot;

        //用来管理创建出来的格子
        private List<GridUnitRenderer> gridPool = new List<GridUnitRenderer>();
        //用来管理创建出来的战斗单位
        private List<BattleUnitRenderer> battleUnitsPool = new List<BattleUnitRenderer>();

        //当前点中的格子
        private GridUnitRenderer selectedGrid = null;

        //当前手动点击的英雄
        private BattleUnitRenderer manualBattleUnitRenderer;
        private ManualOperationState manualOperationState = ManualOperationState.None;

        public void Init(System.Action initedCallback)
        {
            if (gridUnitModel == null
                || gridUnitsRoot == null
                || battleUnitModel == null
                || battleUnitsRoot == null)
            {
                UtilityHelper.LogError("Init battle field renderer failed!");
                return;
            }

            UtilityHelper.Log("Init battle field renderer.");

            //创建一定数量的格子和战斗单位渲染器，留作后面使用
            InitGridUnitRenderer(100);
            InitBattleUnitRenderer(20);

            UtilityHelper.Log("Battle field renderer inited.");

            //战场显示器初始化完成，通知回调
            if (initedCallback != null)
            {
                initedCallback();
            }
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

        //准备加载战场
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

        //准备加载战斗单位
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

        //卸载战斗单位
        private void UnloadBattleUnit()
        {

        }

        //创建格子
        private GridUnitRenderer CreateGridUnitRenderer()
        {
            var clone = Instantiate<GridUnitRenderer>(gridUnitModel);
            clone.transform.SetParent(gridUnitsRoot);
            clone.transform.SetUnused(false);
            gridPool.Add(clone);
            return clone;
        }

        //创建战斗单位
        private BattleUnitRenderer CreateBattleUnitRenderer()
        {
            var clone = Instantiate<BattleUnitRenderer>(battleUnitModel);
            clone.transform.SetParent(battleUnitsRoot);
            clone.transform.SetUnused(false);
            battleUnitsPool.Add(clone);
            return clone;
        }

        //获取没有使用的格子渲染器
        private GridUnitRenderer GetUnusedGridUnitRenderer()
        {
            for (int i = 0; i < gridPool.Count; ++i)
            {
                if (!gridPool[i].gameObject.activeSelf)
                    return gridPool[i];
            }
            return CreateGridUnitRenderer();
        }

        //获取没有使用的战斗单位渲染器
        private BattleUnitRenderer GetUnusedBattleUnitRenderer()
        {
            for (int i = 0; i < battleUnitsPool.Count; ++i)
            {
                if (!battleUnitsPool[i].gameObject.activeSelf)
                    return battleUnitsPool[i];
            }
            return CreateBattleUnitRenderer();
        }

        /// <summary>
        /// 测试点中的格子
        /// </summary>
        private void TestSelected()
        {
            //如果点击了鼠标左键
            if (Input.GetMouseButtonDown(0))
            {
                //计算点击位置
                Vector3 clickedWorldPos = BattleCamera.ScreenToWorldPoint(Input.mousePosition);
                clickedWorldPos.z = 0;
                //判断是否有格子被点中？
                GridUnitRenderer clicked = GetGridClicked(clickedWorldPos);
                //点中了格子
                if (clicked != null)
                {
                    if (selectedGrid != null)
                    {
                        if (selectedGrid.Equals(clicked))
                        {
                            //重复点中相同的格子
                            return;
                        }
                        else
                        {
                            selectedGrid.ResetGridRenderType();
                        }
                    }
                    selectedGrid = clicked;
                    selectedGrid.AppendGridRenderType(GridRenderType.Selected);
                }
                //没有点中格子，但是当前有选中，取消选中
                else if (selectedGrid != null)
                {
                    selectedGrid.ResetGridRenderType();
                    selectedGrid = null;
                }
            }
        }

        /// <summary>
        /// 测试选中区域
        /// </summary>
        private void TestSelectedRange(int radius)
        {
            //如果点击了鼠标左键
            if (Input.GetMouseButtonDown(0))
            {
                //计算点击位置
                Vector3 clickedWorldPos = BattleCamera.ScreenToWorldPoint(Input.mousePosition);
                clickedWorldPos.z = 0;
                //判断是否有格子被点中？
                GridUnitRenderer clicked = GetGridClicked(clickedWorldPos);
                //点中了格子
                if (clicked != null)
                {
                    SetRangeHighlightActive(true, clicked.gridUnit.row, clicked.gridUnit.column, radius);
                }
            }
        }

        private GridUnitRenderer from;
        private GridUnitRenderer to;
        private List<GridUnit> path = new List<GridUnit>();
        private List<GridUnit> searched = new List<GridUnit>();
        
        private void TestNavigation()
        {
            //如果点击了鼠标左键
            if (Input.GetMouseButtonDown(0))
            {
                //计算点击位置
                Vector3 clickedWorldPos = BattleCamera.ScreenToWorldPoint(Input.mousePosition);
                clickedWorldPos.z = 0;
                //判断是否有格子被点中？
                GridUnitRenderer clicked = GetGridClicked(clickedWorldPos);
                //点中了格子
                if (clicked != null)
                {
                    if (clicked.gridUnit.GridType == GridType.Obstacle)
                    {
                        //点中了障碍物！
                        Debug.Log("Clicked obstacle.");
                        return;
                    }
                    if (from == null)
                    {
                        //当前还没有选择起始地点
                        from = clicked;
                        from.AppendGridRenderType(GridRenderType.Start);
                    }
                    else if (to == null)
                    {
                        //两次点中了起点
                        if (from.Equals(clicked))
                            return;

                        //当前没有选择终点
                        to = clicked;
                        to.AppendGridRenderType(GridRenderType.End);
                        UtilityHelper.TimerStart();
                        int navTimes = 999;
                        int count = navTimes;
                        while (count > 0)
                        {
                            //有起点有终点，开始导航
                            if (MapNavigator.Instance.Navigate(battleField.battleMap, from.gridUnit, to.gridUnit, path, searched))
                            {
                            }
                            else
                            {
                                //没有找到路径
                                Debug.LogError("Navitation failed. No path.");
                                return;
                            }
                            --count;
                        }
                        SetPathHighlightActive(true, path.ToArray());
                        UtilityHelper.Log(string.Format("Nav times:{0}, timeCost{1:00}", navTimes, UtilityHelper.TimerEnd()));
                    }
                    else
                    {
                        from.ResetGridRenderType();
                        from = null;
                        to.ResetGridRenderType();
                        to = null;
                        foreach (var item in searched)
                        {
                            battleField.battleMap.mapGrids[item.column, item.row].gridUnitRenderer.ResetGridRenderType();
                        }

                        foreach (var item in path)
                        {
                            battleField.battleMap.mapGrids[item.column, item.row].gridUnitRenderer.ResetGridRenderType();
                        }
                    }
                }
                //没有点中格子
                else
                {
                    if (from != null)
                    {
                        from.ResetGridRenderType();
                        from = null;
                    }
                    if (to != null)
                    {
                        to.ResetGridRenderType();
                        to = null;
                    }

                    foreach (var item in searched)
                    {
                        battleField.battleMap.mapGrids[item.column, item.row].gridUnitRenderer.ResetGridRenderType();
                    }

                    foreach (var item in path)
                    {
                        battleField.battleMap.mapGrids[item.column, item.row].gridUnitRenderer.ResetGridRenderType();
                    }
                }
            }
        }

        private GridUnitRenderer nearestGrid;
        private void TestNearestGrid()
        {
            //如果点击了鼠标左键
            if (Input.GetMouseButtonDown(0))
            {
                //计算点击位置
                Vector3 clickedWorldPos = BattleCamera.ScreenToWorldPoint(Input.mousePosition);
                clickedWorldPos.z = 0;
                //判断是否有格子被点中？
                GridUnitRenderer clicked = GetGridClicked(clickedWorldPos);
                //点中了格子
                if (clicked != null)
                {
                    if (from == null || from.Equals(clicked))
                    {
                        from = clicked;
                        from.gridUnit.battleUnit = new BattleUnit();
                        from.AppendGridRenderType(GridRenderType.Start);
                    }
                    else if (to == null || to.Equals(clicked))
                    {
                        to = clicked;
                        to.gridUnit.battleUnit = new BattleUnit();
                        to.AppendGridRenderType(GridRenderType.End);
                        //求最近的格子
                        var nearest = battleField.battleMap.GetEmptyGrid(from.gridUnit, to.gridUnit, path, -1);
                        if (nearest == null)
                        {
                            UtilityHelper.LogError("Can not find out nearest grid.");
                            from.ResetGridRenderType();
                            to.ResetGridRenderType();
                            to.gridUnit.battleUnit = null;
                            from.gridUnit.battleUnit = null;
                            from = null;
                            to = null;
                        }
                        else
                        {
                            nearestGrid = nearest.gridUnitRenderer;
                            nearestGrid.AppendGridRenderType(GridRenderType.Range);
                            SetPathHighlightActive(true, path.ToArray());
                        }
                    }
                    else
                    {
                        from.ResetGridRenderType();
                        to.ResetGridRenderType();
                        nearestGrid.ResetGridRenderType();
                        to.gridUnit.battleUnit = null;
                        from.gridUnit.battleUnit = null;
                        from = null;
                        to = null;
                        nearestGrid = null;
                        SetPathHighlightActive(false, null);
                        path.Clear();
                    }
                }
                //没有点中格子
                else
                {

                }
            }
        }

        public void ClickedBattleField(Vector3 screenPosition)
        {
            //计算点击位置
            Vector3 clickedWorldPos = BattleCamera.ScreenToWorldPoint(screenPosition);
            clickedWorldPos.z = 0;
            //判断是否有格子被点中？
            GridUnitRenderer clicked = GetGridClicked(clickedWorldPos);
            if (clicked != null && clicked.gridUnit != null)
            {
                OnBattleUnitAndGridTouched(clicked.gridUnit, clicked.gridUnit.battleUnit);
            }
            else
            {
                //点到了地图外，关闭所有弹出层界面
                UIViewManager.Instance.HideViews(UIViewLayer.Popup);
            }
        }

        //更新战场点击的情况
        private void UpdateBattleFieldTouched()
        {
            //如果点击了鼠标左键
            if (Input.GetMouseButtonDown(0))
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    //点中了UI
                    return;
                }
                ClickedBattleField(Input.mousePosition);
            }
        }

        private void OnBattleUnitAndGridTouched(GridUnit gridTouched, BattleUnit battleUnitTouched)
        {
            //没有激活操作状态
            if (manualOperationState == ManualOperationState.None)
            {
                //点中了战斗单位
                if (battleUnitTouched != null)
                {
                    //点中了等待手动操作的战斗单位
                    if (battleUnitTouched.battleUnitRenderer.Equals(manualBattleUnitRenderer))
                    {
                        ShowManualActionList();
                    }
                    else
                    {
                        UIViewManager.Instance.ShowView(UIViewName.BattleFieldUnitInfo, gridTouched, battleUnitTouched);
                    }
                }
                //点中了地图
                else
                {
                    UIViewManager.Instance.ShowView(UIViewName.BattleFieldUnitInfo, gridTouched, battleUnitTouched);
                }
            }
            //选择移动状态
            else if (manualOperationState == ManualOperationState.Move)
            {
                //点中了战斗单位
                if (battleUnitTouched != null)
                {
                    //点中了等待手动操作的战斗单位
                    if (battleUnitTouched.battleUnitRenderer.Equals(manualBattleUnitRenderer))
                    {
                        //取消操作
                        manualOperationState = ManualOperationState.None;
                        SetRangeHighlightActive(false, 0, 0, 0);
                    }
                    else
                    {
                        UIViewManager.Instance.ShowView(UIViewName.BattleFieldUnitInfo, gridTouched, battleUnitTouched);
                    }
                }
                //点中了地图
                else
                {
                    if (gridTouched.GridType == GridType.Obstacle)
                    {
                        UIViewManager.Instance.ShowView(UIViewName.BattleFieldUnitInfo, gridTouched, battleUnitTouched);
                    }
                    else
                    {
                        //点击是否超出了范围
                        GridUnit fromGrid = manualBattleUnitRenderer.battleUnit.mapGrid;
                        if (fromGrid.Distance(gridTouched) > manualBattleUnitRenderer.battleUnit.mobility)
                        {
                            Debug.Log("超出了移动半径！");
                        }
                        else
                        {
                            bool result = MapNavigator.Instance.Navigate(
                                battleField.battleMap,
                                fromGrid,
                                gridTouched,
                                UtilityObjs.gridUnits,
                                null,
                                manualBattleUnitRenderer.battleUnit.mobility
                                );

                            //判断是否可以到达(导航成功且可以可以到达)
                            if (result && UtilityObjs.gridUnits[UtilityObjs.gridUnits.Count - 1].Equals(gridTouched))
                            {
                                //可以到达
                                ManualMoveTo(gridTouched, UtilityObjs.gridUnits.ToArray());
                                UtilityObjs.gridUnits.Clear();
                            }
                            else
                            {
                                //不可以到达
                                Debug.Log("点击位置不可到达！");
                            }
                        }
                    }
                }
            }
            //选择攻击目标状态
            else if (manualOperationState == ManualOperationState.Skill)
            {
                //点中了战斗单位
                if (battleUnitTouched != null)
                {
                    //点中了等待手动操作的战斗单位
                    if (battleUnitTouched.battleUnitRenderer.Equals(manualBattleUnitRenderer))
                    {
                        //取消操作
                        manualOperationState = ManualOperationState.None;
                        SetRangeHighlightActive(false, 0, 0, 0);
                    }
                    else
                    {
                        //判断是否可以被攻击
                        if (manualBattleUnitRenderer.battleUnit.battleTeam.Equals(battleUnitTouched.battleTeam))
                        {
                            //同一个队伍的
                            Debug.Log("攻击状态点中了同一个队伍的战斗单位：" + battleUnitTouched.ToString());
                        }
                        else if (battleUnitTouched.mapGrid.Distance(manualBattleUnitRenderer.battleUnit.mapGrid) > 1)
                        {
                            Debug.Log("不在攻击范围：" + battleUnitTouched.ToString());
                        }
                        else
                        {
                            ManualSkill(battleUnitTouched);
                        }
                    }
                }
                //点中了地图
                else
                {
                    Debug.Log("攻击状态点中了地图单位:" + gridTouched.ToString());
                }
            }
        }

        private void Update()
        {
            if (battleField == null)
                return;
            
            //TestSelected();
            //TestNavigation();
            //TestSelectedRange(2);
            //TestNearestGrid();

            UpdateBattleFieldTouched();
        }

        //***********************************************

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

        public void OnConnect(BattleField field)
        {
            battleField = field;
            //加载战场
            RefreshBattleMapGrids();
            //加载战斗单位
            RefreshBattleUnits();
        }

        //播放战场动作
        public IEnumerator PlayBattleByCoroutine(System.Action callback)
        {
            if (battleField == null
                || battleField.msgAction.battleActions == null
                || battleField.msgAction.battleActions.Count == 0)
            {
                UtilityHelper.LogError(string.Format("Play battle action failed. -> {0}", battleField.battleID));
                yield break;
            }

            UtilityHelper.Log("Play battle actions");

            //遍历所有战斗动作
            var msgAction = battleField.msgAction;
            while (battleField.currentIndex < msgAction.battleActions.Count)
            {
                if (msgAction.battleActions[battleField.currentIndex] == null)
                {
                    UtilityHelper.LogError(string.Format("Play action error. Action is none or type is none, index = {0}", battleField.currentIndex));
                    continue;
                }

                BattleHeroAction heroAction = null;
                //一个英雄动作
                if (msgAction.battleActions[battleField.currentIndex] is BattleHeroAction)
                {
                    heroAction = (BattleHeroAction)msgAction.battleActions[battleField.currentIndex];

                    //有对应的战斗单位，且这个战斗单位已经连接了战斗单位渲染器
                    if (heroAction.actionUnit != null && heroAction.actionUnit.battleUnitRenderer != null)
                    {
                        yield return heroAction.actionUnit.battleUnitRenderer.RunHeroAction(heroAction);
                    }
                }
                ++battleField.currentIndex;
            }

            UtilityHelper.Log("Play Msg Action fin");

            if (callback != null)
                callback();
        }

        public void PlayBattle(System.Action callback)
        {
            StartCoroutine(PlayBattleByCoroutine(callback));
        }

        public void BattleEnd()
        {
            var viewMain = UIViewManager.Instance.GetViewByName<UIViewMain>(UIViewName.Main);
            if (viewMain != null)
                viewMain.ShowBattleEnd();
        }

        public void OnDisconnect()
        {
            if (battleField != null)
            {
                battleField = null;
            }
        }

        //设置手动操作的英雄
        public void SetManualBattleUnit(BattleUnitRenderer battleUnitRenderer)
        {
            manualBattleUnitRenderer = battleUnitRenderer;
        }

        //弹出一个面板
        private void ShowManualActionList()
        {
            if (manualBattleUnitRenderer == null)
                return;

            BattleUnit battleUnit = manualBattleUnitRenderer.battleUnit;
            if (battleUnit == null)
            {
                UtilityHelper.LogError("Show manual asction list error : Battle unit is none.");
                return;
            }

            //判断可操作状态
            if (battleUnit.CheckManualState(ManualActionState.Move) || battleUnit.CheckManualState(ManualActionState.Skill))
            {
                UIViewManager.Instance.HideViews(UIViewLayer.Popup);

                UIViewManager.Instance.ShowView(UIViewName.BattleFieldPlayerActOption, 
                                                battleUnit,
                                                (UnityEngine.Events.UnityAction)BeforeManualMove,
                                                (UnityEngine.Events.UnityAction)BeforeManualSkill,
                                                (UnityEngine.Events.UnityAction)ManualOperationComplete);
            }
            else
            {
                manualBattleUnitRenderer = null;
                UIViewManager.Instance.HideView(UIViewName.BattleFieldPlayerActOption);
            }
        }
        
        private void BeforeManualMove()
        {
            //显示移动范围
            SetRangeHighlightActive(
                true,
                manualBattleUnitRenderer.battleUnit.mapGrid.row,
                manualBattleUnitRenderer.battleUnit.mapGrid.column,
                manualBattleUnitRenderer.battleUnit.mobility);

            //设定为移动状态
            manualOperationState = ManualOperationState.Move;
            UIViewManager.Instance.HideView(UIViewName.BattleFieldPlayerActOption);
        }

        private void ManualMoveTo(GridUnit grid, GridUnit[] path)
        {
            UtilityObjs.battleActions.Clear();
            //创建动作并追加
            manualBattleUnitRenderer.battleUnit.MoveToTargetGrid(UtilityObjs.battleActions, null, grid, path);
            battleField.AppendBattleActions(UtilityObjs.battleActions);
            UtilityObjs.battleActions.Clear();
            PlayBattle(AfterManualMove);
        }

        private void AfterManualMove()
        {
            //清空高亮显示
            SetRangeHighlightActive(false, 0, 0, 0);
            //切换状态
            manualOperationState = ManualOperationState.None;
            //通知移动完成
            manualBattleUnitRenderer.battleUnit.CompleteManualState(ManualActionState.Move);
        }
        
        private void BeforeManualSkill()
        {
            //显示攻击范围
            SetRangeHighlightActive(
                true,
                manualBattleUnitRenderer.battleUnit.mapGrid.row,
                manualBattleUnitRenderer.battleUnit.mapGrid.column,
                1);

            manualOperationState = ManualOperationState.Skill;
            UIViewManager.Instance.HideView(UIViewName.BattleFieldPlayerActOption);
        }

        private void ManualSkill(BattleUnit targetUnit)
        {
            if (targetUnit != null)
            {
                UtilityObjs.battleActions.Clear();
                manualBattleUnitRenderer.battleUnit.UseSkill(UtilityObjs.battleActions, targetUnit, 0);
                battleField.AppendBattleActions(UtilityObjs.battleActions);
                UtilityObjs.battleActions.Clear();
                //取消范围高亮显示
                SetRangeHighlightActive(false, 0, 0, 0);
                ManualOperationComplete();
            }
        }

        private void ManualOperationComplete()
        {
            //切换状态
            manualOperationState = ManualOperationState.None;
            //通知移动完成
            manualBattleUnitRenderer.battleUnit.CompleteManualState(ManualActionState.Skill);
            manualBattleUnitRenderer.battleUnit.CompleteManualState(ManualActionState.Move);
            //清空手动操作单位
            manualBattleUnitRenderer = null;
            //关闭操作菜单
            UIViewManager.Instance.HideView(UIViewName.BattleFieldPlayerActOption);
            //显示后通知手动操作完成
            PlayBattle(battleField.ManualOperationComplete);
        }

        //范围高亮
        private List<GridUnit> rangeHighlightGridUnits = new List<GridUnit>(20);
        //路劲高亮
        private List<GridUnit> pathHighlightGridUnits = new List<GridUnit>(10);
        //设置地图的高亮显示
        public void SetRangeHighlightActive(bool active, int centerRow, int centerColumn, int radius)
        {
            if (!active)
            {
                for (int i = 0; i < rangeHighlightGridUnits.Count; ++i)
                {
                    if (rangeHighlightGridUnits[i].gridUnitRenderer != null)
                    {
                        rangeHighlightGridUnits[i].gridUnitRenderer.RemoveGridRenderType(GridRenderType.Range);
                    }
                }
                rangeHighlightGridUnits.Clear();
            }
            else
            {
                //当前存在上一个激活，先隐藏
                if (rangeHighlightGridUnits.Count > 0)
                    SetRangeHighlightActive(false, 0, 0, 0);

                if (battleField == null)
                {
                    UtilityHelper.LogError("Set range highlight active true error: none battle field.");
                    return;
                }
                battleField.battleMap.GetRangeGrids(centerRow, centerColumn, radius, rangeHighlightGridUnits);
                //设置高亮状态
                for (int i = 0; i < rangeHighlightGridUnits.Count; ++i)
                {
                    if (rangeHighlightGridUnits[i].gridUnitRenderer != null)
                    {
                        rangeHighlightGridUnits[i].gridUnitRenderer.AppendGridRenderType(GridRenderType.Range);
                    }
                }
            }
        }
        
        //设置路径高亮显示
        public void SetPathHighlightActive(bool active, GridUnit[] gridPath)
        {
            if (!active)
            {
                for (int i = 0; i < pathHighlightGridUnits.Count; ++i)
                {
                    if (pathHighlightGridUnits[i].gridUnitRenderer != null)
                    {
                        pathHighlightGridUnits[i].gridUnitRenderer.RemoveGridRenderType(GridRenderType.Path);
                    }
                }
                pathHighlightGridUnits.Clear();
            }
            else
            {
                //当前存在上一个激活，先隐藏
                if (pathHighlightGridUnits.Count > 0)
                    SetPathHighlightActive(false, null);

                if (battleField == null)
                {
                    UtilityHelper.LogError("Set path highlight active true error: none battle field.");
                    return;
                }
                else if (gridPath == null)
                {
                    UtilityHelper.LogError("Set path highlight active true warning: gridPath is none.");
                    return;
                }
                pathHighlightGridUnits.AddRange(gridPath);
                //设置高亮状态
                for (int i = 0; i < pathHighlightGridUnits.Count; ++i)
                {
                    if (pathHighlightGridUnits[i].gridUnitRenderer != null)
                    {
                        pathHighlightGridUnits[i].gridUnitRenderer.AppendGridRenderType(GridRenderType.Path);
                    }
                }
            }
        }
    }
}