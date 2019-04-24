/// <summary>
/// 战斗地图信息
/// </summary>

using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BattleMap
        : CountableInstance
    {
        public BattleField battleField;

        //地图信息
        public int mapID;
        //地图宽高
        public int mapWidth = 0;
        public int mapHeight = 0;

        public int obstacleCount = 0;
        public int obstacleGap = 0;
        public int buffCount = 0;
        public int itemCount;

        //地图格子信息
        public GridUnit[,] mapGrids;
        //出生格子
        List<GridUnit> bornGrids = new List<GridUnit>();
        //普通格子
        List<GridUnit> normalGrids = new List<GridUnit>();
        //障碍格子
        List<GridUnit> obstacleGrids = new List<GridUnit>();

        //格子总数量
        public int GridCount
        {
            get
            {
                return mapWidth * mapHeight;
            }
        }

        //获取出生点数量(单方)
        public int BornCount
        {
            get
            {
                return (int)(bornGrids.Count * 0.5f);
            }
        }

        //战场中铺设格子
        public void Init(
            int width, int height, 
            int obstacleCount, int gap, 
            int buffCount, int itemCount)
        {
            if (width <= 0 || height <= 0)
                return;
            
            //记录地图宽高
            mapWidth = width;
            mapHeight = height;
            //生成格子数组
            mapGrids = new GridUnit[mapWidth, mapHeight];

            this.obstacleCount = obstacleCount;
            this.obstacleGap = gap;
            this.buffCount = buffCount;
            this.itemCount = itemCount;

            //全部生成为普通格子
            for (int r = 0; r < mapHeight; ++r)
            {
                for (int c = 0; c < mapWidth; ++c)
                {
                    GridUnit gridUnitData = new GridUnit(this, r, c);
                    gridUnitData.localPosition = new Vector3(
                        c * EGameConstL.Map_GridWidth + ((r & 1) == (EGameConstL.Map_FirstRowOffset ? 0 : 1) ? (EGameConstL.Map_GridWidth * 0.5f) : 0f),
                        -r * EGameConstL.Map_GridOffsetY,
                        0
                        );
                    
                    //保存
                    mapGrids[c, r] = gridUnitData;
                }
            }

            ResetMap();
        }

        //整理格子
        private void TidyGridList()
        {
            //不能处理born的格子
            //否则会打乱排序

            //将各种格子放入存入对应的容器
            normalGrids.Clear();
            obstacleGrids.Clear();

            //整理格子
            foreach (var grid in mapGrids)
            {
                switch (grid.GridType)
                {
                    case GridType.Normal:
                        normalGrids.Add(grid);
                        break;
                    case GridType.Obstacle:
                        obstacleGrids.Add(grid);
                        break;
                    default:
                        break;
                }
            }
        }

        private void RefreshGridsRuntimePasses()
        {
            foreach (var grid in mapGrids)
            {
                grid.UpdateRuntimePasses(true);
            }
        }

        public GridUnit GetGridData(int row, int column)
        {
            if (row < 0 || row >= mapHeight || column < 0 || column >= mapWidth)
                return null;

            return mapGrids[column, row];
        }

        //根据格子行列获取某个方向的格子
        public GridUnit GetGridByDir(int row, int column, int dir)
        {
            switch (dir)
            {
                //9点钟方向格子
                case 0:
                    return GetGridData(row, column - 1);

                //7点钟方向格子
                case 1:
                    return GetGridData(row + 1, ((row & 1) == (EGameConstL.Map_FirstRowOffset ? 1 : 0)) ? column - 1 : column);

                //5点钟方向
                case 2:
                    return GetGridData(row + 1, ((row & 1) == (EGameConstL.Map_FirstRowOffset ? 1 : 0)) ? column : column + 1);

                //3点钟方向
                case 3:
                    return GetGridData(row, column + 1);

                //1点钟方向
                case 4:
                    return GetGridData(row - 1, ((row & 1) == (EGameConstL.Map_FirstRowOffset ? 1 : 0)) ? column : column + 1);

                //11点钟方向
                case 5:
                    return GetGridData(row - 1, ((row & 1) == (EGameConstL.Map_FirstRowOffset ? 1 : 0)) ? column - 1 : column);

                default:
                    return null;
            }
        }

        //放置一些障碍格子
        private void GenerateObstacle(int obstacleCount, int gap)
        {
            //随机范围
            List<GridUnit> randomRange = new List<GridUnit>();
            //减掉不能随机的格子
            List<GridUnit> reduction = new List<GridUnit>();

            //将普通格子放入
            foreach (var grid in mapGrids)
            {
                if (grid.GridType == GridType.Normal)
                    randomRange.Add(grid);
            }

            int count = obstacleCount;
            while (count > 0 && randomRange.Count > 0)
            {
                int randIdx = Random.Range(0, randomRange.Count);
                GridUnit randomGrid = randomRange[randIdx];
                randomGrid.GridType = GridType.Obstacle;
                //排除格子周围的格子
                GetCircularGrids(randomGrid.row, randomGrid.column, gap, 0, true, reduction);
                if (reduction.Count > 0)
                {
                    foreach (var item in reduction)
                    {
                        randomRange.Remove(item);
                    }
                }
                --count;
            }
        }

        //放置一些出生点
        private void GenerateBorn()
        {
            ///////0123456789
            // 0 //*x*x*x*x*x
            // 1 //**********
            // 2 //**********
            // 3 //*x*x*x*x*x
            // 4 //**********
            bornGrids.Clear();

            //两边生成
            for (int i = 1; i < mapWidth; i = i + 2)
            {
                mapGrids[i, 0].GridType = GridType.Born;
                bornGrids.Add(mapGrids[i, 0]);
            }

            for (int i = 1; i < mapWidth; i = i + 2)
            {
                mapGrids[i, mapHeight - 1].GridType = GridType.Born;
                bornGrids.Add(mapGrids[i, mapHeight - 1]);
            }
        }

        private void GenerateBuff(int buffCount)
        {
            int randomCount = Mathf.Min(buffCount, normalGrids.Count);
            if (randomCount <= 0)
                return;

            List<GridUnit> tempGrids = new List<GridUnit>(normalGrids);
            for (int i = 0; i < randomCount; i++)
            {
                //随机一种类型
                int randomType = Random.Range(1, (int)GridUnitBuffType.Range + 1);
                int randomIdx = Random.Range(0, tempGrids.Count);
                int randomAddition = 0;
                GridUnitBuffType gridUnitBuffType = (GridUnitBuffType)randomType;
                //不同类型不同随机范围
                switch (gridUnitBuffType)
                {
                    case GridUnitBuffType.None:
                    case GridUnitBuffType.Atk:
                    case GridUnitBuffType.Def:
                        randomAddition = Random.Range(1, 5);
                        break;
                    case GridUnitBuffType.Range:
                        randomAddition = Random.Range(1, 2);
                        break;
                }

                GridUnitBuff randoBuff = GridUnitBuff.CreateInstance(gridUnitBuffType, randomAddition);
                if (randoBuff != null)
                {
                    tempGrids[randomIdx].gridUnitBuff = randoBuff;
                    tempGrids.RemoveAt(randomIdx);
                }
            }
        }

        //移除所有道具
        public void RemoveAllItems()
        {
            for (int i = 0; i < normalGrids.Count; i++)
            {
                normalGrids[i].gridItem = null;
            }
        }

        //创建一些道具
        public void GenerateItems(int itemCount)
        {
            int randomCount = Mathf.Min(itemCount, normalGrids.Count);
            if (randomCount <= 0)
                return;

            List<GridUnit> tempGrids = new List<GridUnit>(normalGrids);
            for (int i = 0; i < randomCount; i++)
            {
                //随机一种道具类型
                int randomItemId = Random.Range(5000, 5002);
                int randomIdx = Random.Range(0, tempGrids.Count);
                SO_PackageItem item = PackageItemManager.Instance.GetItem(randomItemId);
                if (item == null)
                {
                    UtilityHelper.LogError(string.Format("Generate items failed. ID = {0} ", randomItemId));
                    continue;
                }
                else
                {
                    tempGrids[randomIdx].gridItem = new GridItem();
                    tempGrids[randomIdx].gridItem.item = item;
                    tempGrids[randomIdx].gridItem.count = 2;
                    tempGrids.RemoveAt(randomIdx);
                }
            }
        }

        //从某个格子到目标格
        public GridUnit GetEmptyGrid(GridUnit from, GridUnit to, List<GridUnit> path, int mobility)
        {
            //总需要一个容器来保存
            if (path == null)
                path = new List<GridUnit>();

            path.Clear();

            //发起向终点的一个导航
            if (MapNavigator.Instance.Navigate(this, from, to, path, null, mobility))
            {
                //表示可以到达
                if (path.Count > 1)
                {
                    if (path[path.Count - 1].Equals(to))
                    {
                        //移除最后一个，因为它是目标点
                        path.RemoveAt(path.Count - 1);
                        return path[path.Count - 1];
                    }
                    else
                        return path[path.Count - 1];
                }
                else if (path.Count > 0)
                {
                    //一步之遥，无需移动
                    path.Clear();
                    return from;
                }
                else
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// 根据圆心、内外圆半径，获取环形区域
        /// </summary>
        /// <param name="centerRow">中心所在行</param>
        /// <param name="centerColumn">中心所在列</param>
        /// <param name="outerRadius">外圆半径</param>
        /// <param name="innerRadius">内圆半径</param>
        /// <param name="containsCenter">是否包含中心</param>
        /// <param name="grids">结果格子</param>
        /// <param name="filter">过滤器</param>
        public void GetCircularGrids(
            int centerRow, int centerColumn,
            int outerRadius, int innerRadius, bool containsCenter,
            List<GridUnit> grids,
            System.Func<GridUnit, bool> filter = null)
        {
            if (grids == null)
                return;

            grids.Clear();

            outerRadius = outerRadius < 0 ? 0 : outerRadius;
            innerRadius = innerRadius < 0 ? 0 : innerRadius;

            //外圆半径更小
            if (outerRadius <= innerRadius && outerRadius > 0)
            {
                UtilityHelper.LogError(string.Format("Get Circular Grids failed.Outer radius = {0}, InnerRadius = {1}", outerRadius, innerRadius));
                return;
            }

            if (outerRadius == 0 && containsCenter && filter == null)
            {
                grids.Add(GetGridData(centerRow, centerColumn));
                return;
            }

            //按照行移动量来算
            for (int i = 0; i <= outerRadius; ++i)
            {
                //计算每一行的范围
                //行之间的差
                int rowGap = i;
                //移动行后所覆盖的最小、最大横坐标
                int minColumn = 0;
                int maxColumn = 0;

                //奇数行开始时
                if ((centerRow & 1) == (EGameConstL.Map_FirstRowOffset ? 0 : 1))
                {
                    minColumn = Mathf.Max(centerColumn - (rowGap / 2), 0);
                    maxColumn = centerColumn + ((rowGap + 1) / 2);
                }
                //偶数行开始时
                else
                {
                    minColumn = Mathf.Max(centerColumn - ((rowGap + 1) / 2), 0);
                    maxColumn = centerColumn + (rowGap / 2);
                }

                //列范围
                minColumn = Mathf.Max(0, minColumn - (outerRadius - i));
                maxColumn = Mathf.Min(mapWidth - 1, maxColumn + (outerRadius - i));
                //装入所有
                for (int c = minColumn; c <= maxColumn; ++c)
                {
                    if (i == 0)
                    {
                        //设置了内圆半径
                        if (innerRadius == 0
                            || (innerRadius > 0 && mapGrids[c, centerRow].Distance(mapGrids[centerColumn, centerRow]) > innerRadius))
                        {
                            if (c != centerColumn || containsCenter)
                            {
                                if (filter == null || filter(mapGrids[c, centerRow]))
                                    grids.Add(mapGrids[c, centerRow]);
                            }
                        }
                    }
                    else
                    {
                        int temp = centerRow - i;
                        if (temp >= 0)
                        {
                            //设置了内圆半径
                            if (innerRadius == 0
                                || (innerRadius > 0 && mapGrids[c, temp].Distance(mapGrids[centerColumn, centerRow]) > innerRadius))
                            {
                                if (filter == null || filter(mapGrids[c, temp]))
                                    grids.Add(mapGrids[c, temp]);
                            }
                        }

                        temp = centerRow + i;
                        if (temp < mapHeight)
                        {
                            //设置了内圆半径
                            if (innerRadius == 0
                                || (innerRadius > 0 && mapGrids[c, temp].Distance(mapGrids[centerColumn, centerRow]) > innerRadius))
                            {
                                if (filter == null || filter(mapGrids[c, temp]))
                                    grids.Add(mapGrids[c, temp]);
                            }
                        }
                    }
                }
            }
        }

        //获取出生点 0:上方 1:下方
        public GridUnit[] GetBornGrid(int side, int bornCount, bool rand)
        {
            if (rand)
                return GetRandomBornGrid(bornCount);

            side = side > 0 ? 1 : 0;
            bornCount = Mathf.Min(bornCount, BornCount);

            int start = side * bornCount;
            GridUnit[] grids = new GridUnit[bornCount];
            bornGrids.CopyTo(start, grids, 0, bornCount);
            return grids;
        }

        //获得全图的随机位置
        private GridUnit[] GetRandomBornGrid(int count)
        {
            List<GridUnit> randomBornGrids = new List<GridUnit>(normalGrids);
            //移除已经包含了玩家的格子
            for (int i = randomBornGrids.Count - 1; i >= 0; --i)
            {
                if (randomBornGrids[i].battleUnit != null)
                    randomBornGrids.RemoveAt(i);
            }
            GridUnit[] grids = new GridUnit[count];
            for (int i = 0; i < count; i++)
            {
                int randomIdx = Random.Range(0, randomBornGrids.Count);
                grids[i] = randomBornGrids[randomIdx];
                randomBornGrids.RemoveAt(randomIdx);
            }
            return grids;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is BattleMap)
            {
                return mapID == ((BattleMap)obj).mapID;
            }
            return false;
        }

        public void ResetMap()
        {
            for (int r = 0; r < mapHeight; ++r)
            {
                for (int c = 0; c < mapWidth; ++c)
                {
                    //初始设置为普通格子
                    mapGrids[c, r].GridType = GridType.Normal;
                    //清除buff
                    mapGrids[c, r].gridUnitBuff = null;
                    //清楚道具
                    mapGrids[c, r].gridItem = null;
                    //清空上面的战斗单位
                    mapGrids[c, r].battleUnit = null;
                    mapGrids[c, r].tempRef = null;
                }
            }

            //随机一些出生格子
            GenerateBorn();

            //随机一些障碍格子
            GenerateObstacle(obstacleCount, obstacleGap);

            //整理格子列表
            TidyGridList();

            //从普通格子里随机一些带Buff的格子~
            GenerateBuff(buffCount);

            GenerateItems(itemCount);

            RefreshGridsRuntimePasses();
        }
    }
}