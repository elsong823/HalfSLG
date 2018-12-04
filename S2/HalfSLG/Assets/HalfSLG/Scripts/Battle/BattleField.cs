using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BattleField
        : ELBehaviour
    {
        private static BattleField instance;
        public static BattleField Instance
        {
            get
            {
                return instance;
            }
        }
        
        //当前显示的战斗信息
        private BattleData currentData;
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
                        EUtilityHelperL.LogError("Error: Can not find battle camera!");
                        return null;
                    }
                    battleCamera = objCamera.GetComponent<Camera>();
                }
                return battleCamera;
            }
        }

        //格子的模型，用来clone格子拼成地图
        [SerializeField] GridUnit gridUnitModel;
        [SerializeField] private Transform gridUnitsRoot;

        //当前地图上挂的格子
        GridUnit[,] gridUnits;
        
        //用来管理创建出来的格子
        List<GridUnit> gridPool;

        //当前点中的格子
        GridUnit selectedGrid = null;

        //加载战斗信息
        public void LoadBattleData(BattleData battleData)
        {
            if (currentData != null)
                UnloadBattleData();

            currentData = battleData;

            PrepareBattleMap();
        }

        //准备加载战场
        private void PrepareBattleMap()
        {
            if (currentData == null)
            {
                EUtilityHelperL.LogError("Prepare battle map failed. No battle data.");
                return;
            }
            gridUnits = new GridUnit[currentData.mapData.mapWidth, currentData.mapData.mapHeight];

            for (int r = 0; r < currentData.mapData.mapHeight; ++r)
            {
                for (int c = 0; c < currentData.mapData.mapWidth; ++c)
                {
                    GridUnitData gud = currentData.mapData.mapGrids[c, r];
                    if (gud != null)
                    {
                        //创建一个用于显示的格子对象
                        GridUnit gridUnit = CreateGrid();
                        if (gridUnit != null)
                        {
                            gridUnits[c, r] = gridUnit;
                            gridUnit.transform.localPosition = gud.localPosition;
                            gridUnit.name = string.Format("Grid_{0}_{1}", r, c);
                            gridUnit.gridData = gud;
                            gridUnit.RefreshColor();
                            gridUnit.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }

        //卸载战场
        private void UnloadBattleData()
        {
            RecycleAllGrids();
            currentData = null;
        }

        //创建格子
        private GridUnit CreateGrid()
        {
            if (gridPool == null)
                gridPool = new List<GridUnit>();

            for (int i = 0; i < gridPool.Count; ++i)
            {
                if (!gridPool[i].gameObject.activeSelf)
                    return gridPool[i];
            }

            var gu = Instantiate<GridUnit>(gridUnitModel);
            gu.transform.SetParent(gridUnitsRoot);
            gu.transform.localPosition = Vector3.zero;
            gu.transform.localScale = Vector3.one;
            gu.transform.localRotation = Quaternion.identity;

            gridPool.Add(gu);

            return gu;
        }

        //回收所有格子
        private void RecycleAllGrids()
        {
            if (gridPool == null)
                return;

            for (int i = 0; i < gridPool.Count; ++i)
            {
                gridPool[i].transform.localPosition = Vector3.zero;
                gridPool[i].name = "UNUSED";
                gridPool[i].gameObject.SetActive(false);
            }

            gridUnits = null;
        }

        private void Awake()
        {
            instance = this;
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
                GridUnit clicked = GetGridClicked(clickedWorldPos);
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
                GridUnit clicked = GetGridClicked(clickedWorldPos);
                //点中了格子
                if (clicked != null)
                {
                    ClearRendererType();
                    List<GridUnitData> rangeGrids = new List<GridUnitData>();
                    //测试区域
                    currentData.mapData.GetRangeGrids(clicked.gridData.row, clicked.gridData.column, radius, rangeGrids);

                    foreach (var item in rangeGrids)
                    {
                        gridUnits[item.column, item.row].GridRenderType = GridRenderType.Range;
                    }
                }
            }
        }

        /// <summar测试导航y>
        /// 测试导航
        /// </summary>
        //***********************************************
        private GridUnit from;
        private GridUnit to;
        private List<GridUnitData> path = new List<GridUnitData>();
        private List<GridUnitData> searched = new List<GridUnitData>();

        private void TestGridRender()
        {
            foreach (var item in searched)
            {
                var gu = gridUnits[item.column, item.row];
                if (!gu.Equals(from) && !gu.Equals(to))
                    gu.GridRenderType = GridRenderType.Searched;
            }

            foreach (var item in path)
            {
                var gu = gridUnits[item.column, item.row];
                if (!gu.Equals(from) && !gu.Equals(to))
                    gu.GridRenderType = GridRenderType.Path;
            }
        }

        private void ClearRendererType()
        {
            foreach (var item in gridUnits)
            {
                item.GridRenderType = GridRenderType.Normal;
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
                GridUnit clicked = GetGridClicked(clickedWorldPos);
                //点中了格子
                if (clicked != null)
                {
                    if (clicked.gridData.GridType == GridType.Obstacle)
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
                        EUtilityHelperL.TimerStart();
                        int navTimes = 999;
                        int count = navTimes;
                        while (count > 0)
                        {
                            //有起点有终点，开始导航
                            if (MapNavigator.Instance.Navigate(currentData.mapData, from.gridData, to.gridData, path, searched))
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
                        EUtilityHelperL.Log(string.Format("Nav times:{0}, timeCost{1:00}", navTimes, EUtilityHelperL.TimerEnd()));
                    }
                    else
                    {
                        from.GridRenderType = GridRenderType.Normal;
                        from = null;
                        to.GridRenderType = GridRenderType.Normal;
                        to = null;
                        foreach (var item in searched)
                        {
                            gridUnits[item.column, item.row].GridRenderType = GridRenderType.Normal;
                        }

                        foreach (var item in path)
                        {
                            gridUnits[item.column, item.row].GridRenderType = GridRenderType.Normal;
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
                        gridUnits[item.column, item.row].GridRenderType = GridRenderType.Normal;
                    }

                    foreach (var item in path)
                    {
                        gridUnits[item.column, item.row].GridRenderType = GridRenderType.Normal;
                    }
                }
            }
        }

        private void Update()
        {
            //测试点中
            TestSelected();

            //测试导航
            //TestNavigation();

            //测试半径显示
            //TestSelectedRange(2);
        }

        //***********************************************

        private GridUnit GetGridClicked(Vector3 clickedWorldPos)
        {
            //转换空间到格子组织节点(GridUnits)的空间
            clickedWorldPos = gridUnitsRoot.transform.InverseTransformPoint(clickedWorldPos);
            //初步判定所在行列
            int row = Mathf.FloorToInt((clickedWorldPos.y - EGameConstL.Map_GridOffsetY * 0.5f) / -EGameConstL.Map_GridOffsetY);
            int column = Mathf.FloorToInt((clickedWorldPos.x + EGameConstL.Map_GridWidth * 0.5f - ((row & 1) == (EGameConstL.Map_FirstRowOffset ? 1 : 0) ? 0f : (EGameConstL.Map_GridWidth * 0.5f))) / EGameConstL.Map_GridWidth);

            int testRow = 0;
            int testColumn = 0;
            //二次判定，判定周围格子
            GridUnit clickedGrid = null;
            float minDis = Mathf.Infinity;
            for (int r = -1; r <= 1; ++r)
            {
                for (int c = -1; c <= 1; ++c)
                {
                    testRow = row + r;
                    testColumn = column + c;
                    if (testRow < 0 || testRow >= currentData.mapData.mapHeight
                        || testColumn < 0 || testColumn >= currentData.mapData.mapWidth)
                    {
                        continue;
                    }
                    float distance = EUtilityHelperL.CalcDistanceInXYAxis(clickedWorldPos, currentData.mapData.mapGrids[testColumn, testRow].localPosition);
                    if (distance < minDis && distance < EGameConstL.Map_HexRadius)
                    {
                        minDis = distance;
                        clickedGrid = gridUnits[testColumn, testRow];
                    }
                }
            }
            return clickedGrid;
        }
    }
}