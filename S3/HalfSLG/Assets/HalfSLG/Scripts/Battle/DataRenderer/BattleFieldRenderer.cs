//战场显示器
//同时只有一个战场会被显示

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BattleFieldRenderer
        : MonoBehaviourSingleton<BattleFieldRenderer>,
          IVisualRenderer<BattleField, BattleFieldRenderer>
    {
        //当前显示的战斗信息
        private BattleField battleField;
        private Camera battleCamera;
        private Camera BattleCamera
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
        List<GridUnitRenderer> gridPool = new List<GridUnitRenderer>();
        //用来管理创建出来的战斗单位
        List<BattleUnitRenderer> battleUnitsPool = new List<BattleUnitRenderer>();

        //当前点中的格子
        GridUnitRenderer selectedGrid = null;

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
            if(initedCallback != null)
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
                            Debug.Log("点中了相同的格子");
                            return;
                        }
                        else
                        {
                            selectedGrid.GridRenderType = GridRenderType.Normal;
                        }
                    }
                    selectedGrid = clicked;
                    selectedGrid.GridRenderType = GridRenderType.Selected;
                }
                //没有点中格子，但是当前有选中，取消选中
                else if (selectedGrid != null)
                {
                    selectedGrid.GridRenderType = GridRenderType.Normal;
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
                    ClearRendererType();
                    List<GridUnit> rangeGrids = new List<GridUnit>();
                    //测试区域
                    battleField.battleMap.GetRangeGrids(clicked.gridUnit.row, clicked.gridUnit.column, radius, rangeGrids);

                    foreach (var item in rangeGrids)
                    {
                        battleField.battleMap.mapGrids[item.column, item.row].gridUnitRenderer.GridRenderType = GridRenderType.Range;
                    }
                }
            }
        }

        /// <summar测试导航y>
        /// 测试导航
        /// </summary>
        //***********************************************
        private GridUnitRenderer from;
        private GridUnitRenderer to;
        private List<GridUnit> path = new List<GridUnit>();
        private List<GridUnit> searched = new List<GridUnit>();

        private void TestGridRender()
        {
            foreach (var item in searched)
            {
                var gridUnit = battleField.battleMap.mapGrids[item.column, item.row];
                if (!gridUnit.Equals(from.gridUnit) && !gridUnit.Equals(to.gridUnit))
                    gridUnit.gridUnitRenderer.GridRenderType = GridRenderType.Searched;
            }

            foreach (var item in path)
            {
                var gridUnit = battleField.battleMap.mapGrids[item.column, item.row];
                if (!gridUnit.Equals(from.gridUnit) && !gridUnit.Equals(to.gridUnit))
                    gridUnit.gridUnitRenderer.GridRenderType = GridRenderType.Path;
            }
        }

        private void ClearRendererType()
        {
            foreach (var item in battleField.battleMap.mapGrids)
            {
                item.gridUnitRenderer.GridRenderType = GridRenderType.Normal;
            }
        }

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
                        from.GridRenderType = GridRenderType.Start;
                    }
                    else if (to == null)
                    {
                        //两次点中了起点
                        if (from.Equals(clicked))
                            return;

                        //当前没有选择终点
                        to = clicked;
                        to.GridRenderType = GridRenderType.End;
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
                        TestGridRender();
                        UtilityHelper.Log(string.Format("Nav times:{0}, timeCost{1:00}", navTimes, UtilityHelper.TimerEnd()));
                    }
                    else
                    {
                        from.GridRenderType = GridRenderType.Normal;
                        from = null;
                        to.GridRenderType = GridRenderType.Normal;
                        to = null;
                        foreach (var item in searched)
                        {
                            battleField.battleMap.mapGrids[item.column, item.row].gridUnitRenderer.GridRenderType = GridRenderType.Normal;
                        }

                        foreach (var item in path)
                        {
                            battleField.battleMap.mapGrids[item.column, item.row].gridUnitRenderer.GridRenderType = GridRenderType.Normal;
                        }
                    }
                }
                //没有点中格子
                else
                {
                    if (from != null)
                    {
                        from.GridRenderType = GridRenderType.Normal;
                        from = null;
                    }
                    if (to != null)
                    {
                        to.GridRenderType = GridRenderType.Normal;
                        to = null;
                    }

                    foreach (var item in searched)
                    {
                        battleField.battleMap.mapGrids[item.column, item.row].gridUnitRenderer.GridRenderType = GridRenderType.Normal;
                    }

                    foreach (var item in path)
                    {
                        battleField.battleMap.mapGrids[item.column, item.row].gridUnitRenderer.GridRenderType = GridRenderType.Normal;
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
                        from.GridRenderType = GridRenderType.Start;
                    }
                    else if (to == null || to.Equals(clicked))
                    {
                        to = clicked;
                        to.gridUnit.battleUnit = new BattleUnit();
                        to.GridRenderType = GridRenderType.End;
                        //求最近的格子
                        var nearest = battleField.battleMap.GetEmptyGrid(from.gridUnit, to.gridUnit, path, -1);
                        if (nearest == null)
                        {
                            UtilityHelper.LogError("Can not find out nearest grid.");
                            from.GridRenderType = GridRenderType.Normal;
                            to.GridRenderType = GridRenderType.Normal;
                            to.gridUnit.battleUnit = null;
                            from.gridUnit.battleUnit = null;
                            from = null;
                            to = null;
                        }
                        else
                        {
                            nearestGrid = nearest.gridUnitRenderer;
                            nearestGrid.GridRenderType = GridRenderType.Range;
                            foreach (var item in path)
                            {
                                if (item.Equals(to.gridUnit) || item.Equals(nearestGrid.gridUnit))
                                    continue;
                                item.gridUnitRenderer.GridRenderType = GridRenderType.Path;
                            }
                        }
                    }
                    else
                    {
                        from.GridRenderType = GridRenderType.Normal;
                        to.GridRenderType = GridRenderType.Normal;
                        nearestGrid.GridRenderType = GridRenderType.Normal;
                        to.gridUnit.battleUnit = null;
                        from.gridUnit.battleUnit = null;
                        from = null;
                        to = null;
                        nearestGrid = null;
                        foreach (var item in path)
                        {
                            item.gridUnitRenderer.GridRenderType = GridRenderType.Normal;
                        }
                        path.Clear();
                    }
                }
                //没有点中格子
                else
                {

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
        public IEnumerator PlayBattleActions()
        {
            if (battleField == null 
                || battleField.msgAction.battleActions == null 
                || battleField.msgAction.battleActions.Count == 0)
            {
                UtilityHelper.LogError(string.Format("Play battle action failed. -> {0}", battleField.battleID));
                yield break;
            }

            UtilityHelper.Log("Play battle actions");

            yield return StartCoroutine(PlayMsgAction(battleField.msgAction));
        }

        private IEnumerator PlayMsgAction(MsgAction msgAction)
        {
            //遍历所有战斗动作
            for (int i = 0; i < msgAction.battleActions.Count; ++i)
            {
                if (msgAction.battleActions[i] == null)
                {
                    UtilityHelper.LogError(string.Format("Play action error. Action is none or type is none, index = {0}", i));
                    continue;
                }

                BattleHeroAction heroAction = null;
                //一个英雄动作
                if(msgAction.battleActions[i] is BattleHeroAction)
                {
                    heroAction = (BattleHeroAction)msgAction.battleActions[i];

                    //有对应的战斗单位，且这个战斗单位已经连接了战斗单位渲染器
                    if(heroAction.actionUnit != null && heroAction.actionUnit.battleUnitRenderer != null)
                    {
                        yield return heroAction.actionUnit.battleUnitRenderer.RunHeroAction(heroAction);
                    }
                }
            }

            UtilityHelper.Log("Play Msg Action fin");
        }

        public void OnDisconnect()
        {
            if(battleField != null)
            {
                battleField = null;
            }
        }
    }
}