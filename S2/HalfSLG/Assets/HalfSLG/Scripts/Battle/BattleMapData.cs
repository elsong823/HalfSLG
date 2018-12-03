using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    /// <summary>
    /// 战斗地图信息
    /// </summary>
    public class BattleMapData
    {
        //地图信息
        public int mapID;
        //地图宽高
        public int mapWidth = 0;
        public int mapHeight = 0;

        //地图格子信息
        public GridUnitData[,] mapGrids;
        //出生格子
        List<GridUnitData> bornGrids = new List<GridUnitData>();
        //普通格子
        List<GridUnitData> normalGrids = new List<GridUnitData>();
        //障碍格子
        List<GridUnitData> obstacleGrids = new List<GridUnitData>();

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
        public void Generate(int width, int height, int obstacleCount, int gap)
        {
            EUtilityHelperL.TimerStart();

            if (width <= 0 || height <= 0)
                return;
            
            //记录地图宽高
            mapWidth = width;
            mapHeight = height;
            //生成格子数组
            mapGrids = new GridUnitData[mapWidth, mapHeight];

            //全部生成为普通格子
            for (int r = 0; r < mapHeight; ++r)
            {
                for (int c = 0; c < mapWidth; ++c)
                {
                    GridUnitData gridUnitData = new GridUnitData(mapID, r, c);
                    gridUnitData.localPosition = new Vector3(
                        c * EGameConstL.Map_GridWidth + ((r & 1) == (EGameConstL.Map_FirstRowOffset ? 0 : 1) ? (EGameConstL.Map_GridWidth * 0.5f) : 0f),
                        -r * EGameConstL.Map_GridOffsetY,
                        0
                        );

                    //初始设置为普通格子
                    gridUnitData.GridType = GridType.Normal;
                    //保存
                    mapGrids[c, r] = gridUnitData;
                }
            }
            //随机一些障碍格子
            GenerateObstacle(obstacleCount, gap);
            //整理格子列表
            TidyGridList();

            EUtilityHelperL.Log(string.Format("Generate map {0}, time cost:{1}", mapID, EUtilityHelperL.TimerEnd()));
        }

        //整理格子
        private void TidyGridList()
        {
            //将各种格子放入存入对应的容器
            normalGrids.Clear();
            bornGrids.Clear();
            obstacleGrids.Clear();

            //整理格子
            foreach (var grid in mapGrids)
            {
                switch (grid.GridType)
                {
                    case GridType.Normal:
                        normalGrids.Add(grid);
                        break;
                    case GridType.Born:
                        bornGrids.Add(grid);
                        break;
                    case GridType.Obstacle:
                        obstacleGrids.Add(grid);
                        break;
                    default:
                        break;
                }
            }
        }

        public GridUnitData GetGridData(int row, int column)
        {
            if (row < 0 || row >= mapHeight || column < 0 || column >= mapWidth)
                return null;

            return mapGrids[column, row];
        }

        //根据格子行列获取某个方向的格子
        public GridUnitData GetGridDataByDir(int row, int column, int dir)
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
            List<GridUnitData> randomRange = new List<GridUnitData>();
            //减掉不能随机的格子
            List<GridUnitData> reduction = new List<GridUnitData>();

            //将普通格子放入
            foreach (var grid in mapGrids)
            {
                if (grid.GridType == GridType.Normal)
                    randomRange.Add(grid);
            }

            int count = obstacleCount;
            while(count > 0 && randomRange.Count > 0)
            {
                int randIdx = Random.Range(0, randomRange.Count);
                GridUnitData randomGrid = randomRange[randIdx];
                randomGrid.GridType = GridType.Obstacle;
                //排除格子周围的格子
                GetRangeGrids(randomGrid.row, randomGrid.column, gap, reduction);
                if(reduction.Count > 0)
                {
                    foreach (var item in reduction)
                    {
                        randomRange.Remove(item);
                    }
                }
                --count;
            }
        }

        //根据圆心获取范围格子
        public void GetRangeGrids(int row, int column, int range, List<GridUnitData> grids)
        {
            if (grids == null || range <= 0)
                return;

            grids.Clear();

            //按照行移动量来算
            for (int i = 0; i <= range; ++i)
            {
                //计算每一行的范围
                //行之间的差
                int rowGap = i;
                //移动行后所覆盖的最小、最大横坐标
                int minColumn = 0;
                int maxColumn = 0;

                //奇数行开始时
                if ((row & 1) == (EGameConstL.Map_FirstRowOffset ? 0 : 1))
                {
                    minColumn = Mathf.Max(column - (rowGap / 2), 0);
                    maxColumn = column + ((rowGap + 1) / 2);
                }
                //偶数行开始时
                else
                {
                    minColumn = Mathf.Max(column - ((rowGap + 1) / 2), 0);
                    maxColumn = column + (rowGap / 2);
                }

                //列范围
                minColumn = Mathf.Max(0, minColumn - (range - i));
                maxColumn = Mathf.Min(mapWidth - 1, maxColumn + (range - i));
                //装入所有
                for (int c = minColumn; c <= maxColumn; ++c)
                {
                    if (i == 0)
                        grids.Add(mapGrids[c, row]);
                    else
                    {
                        int temp = row - i;
                        if(temp >= 0)
                            grids.Add(mapGrids[c, temp]);
                        temp = row + i;
                        if(temp < mapHeight)
                            grids.Add(mapGrids[c, temp]);
                    }
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is BattleMapData)
            {
                return mapID == ((BattleMapData)obj).mapID;
            }
            return false;
        }
    }
}