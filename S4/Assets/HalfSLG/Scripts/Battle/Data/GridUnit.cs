using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    //格子类型
    public enum GridType
    {
        None,       
        Normal,     //平地
        Born,       //出生点
        Obstacle,   //障碍，无法通过
    }
    
    public class GridUnit
        :IVisualData<GridUnit, GridUnitRenderer>
    {
        public GridUnit(int mapID, int row, int column)
        {
            gridType = GridType.None;
            this.mapID = mapID;
            this.row = row;
            this.column = column;
        }

        //所属地图ID
        public int mapID;
        //格子类型
        private GridType gridType;
        //当前停留的战斗单位
        public BattleUnit battleUnit;
        //格子可通行方向
        public int passes;
        //格子所在行列
        public int row;
        public int column;
        //格子所在空间位置
        public Vector3 localPosition;
        //指向obj类型的通用临时使用的指针
        public System.Object tempRef;

        public GridUnitRenderer gridUnitRenderer;

        public GridType GridType
        {
            get
            {
                return gridType;
            }
            set
            {
                gridType = value;
                switch (gridType)
                {
                    case GridType.None:
                        passes = 0;
                        break;
                    case GridType.Normal:
                        passes = 63;
                        break;
                    case GridType.Obstacle:
                        passes = 0;
                        break;
                    case GridType.Born:
                        passes = 63;
                        break;
                    default:
                        passes = 63;
                        break;
                }
                if (gridUnitRenderer != null)
                    gridUnitRenderer.RefreshColor();
            }
        }

        //计算两格子之间的距离
        public int Distance(GridUnit target)
        {
            //行之间的差
            int rowGap = Mathf.Abs(target.row - row);
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
            //在移动范围之外，额外增加
            if (target.column < minColumn)
                return rowGap + minColumn - target.column;
            else if (target.column > maxColumn)
                return rowGap + target.column - maxColumn;
            //在移动范围之内，因此行移动量就是两格子的距离
            else
                return rowGap;
        }

        public void OnEnter(BattleUnit battleUnit)
        {
            this.battleUnit = battleUnit;
        }

        public void OnLeave()
        {
            this.battleUnit = null;
        }

        public override bool Equals(object obj)
        {
            if (obj is GridUnit)
            {
                GridUnit data = (GridUnit)obj;
                return data.mapID == mapID
                    && data.row == row 
                    && data.column == column;
            }

            return false;
        }

        public override string ToString()
        {
            return string.Format("[{0}-({1},{2})]", mapID, row, column);
        }

        public void ConnectRenderer(GridUnitRenderer renderer)
        {
            if(renderer == null)
            {
                UtilityHelper.LogError("Grid unit connect renderer failed. RD is null");
                return;
            }

            if (gridUnitRenderer != null)
                DisconnectRenderer();

            gridUnitRenderer = renderer;
            gridUnitRenderer.OnConnect(this);
        }

        public void DisconnectRenderer()
        {
            if(gridUnitRenderer != null)
            {
                gridUnitRenderer.OnDisconnect();
                gridUnitRenderer = null;
            }
        }
    }
}