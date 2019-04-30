using System.Text;
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

    public class GridItem
    {
        public SO_PackageItem item;
        public int count;
    }

    public class GridUnit
        : IVisualData<GridUnit, GridUnitRenderer>
    {
        public GridUnit(BattleMap battleMap, int row, int column)
        {
            gridType = GridType.None;
            this.battleMap = battleMap;
            this.row = row;
            this.column = column;
        }

        //所属地图ID
        public BattleMap battleMap;
        //格子类型
        private GridType gridType;
        //当前停留的战斗单位
        public BattleUnit battleUnit;
        //格子可通行方向
        public int roadPasses;
        //实时的同行情况
        public int runtimePasses;
        //格子所在行列
        public int row;
        public int column;
        //格子所在空间位置
        public Vector3 localPosition;
        //指向obj类型的通用临时使用的指针
        public System.Object tempRef;

        //这个格子的buff
        public GridUnitBuff gridUnitBuff;
        //这个格子上的道具
        public GridItem gridItem;

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
                        roadPasses = 0;
                        break;
                    case GridType.Normal:
                        roadPasses = 63;
                        break;
                    case GridType.Obstacle:
                        roadPasses = 0;
                        break;
                    case GridType.Born:
                        roadPasses = 63;
                        break;
                    default:
                        roadPasses = 63;
                        break;
                }
                if (gridUnitRenderer != null)
                    gridUnitRenderer.RefreshColor();
            }
        }

        /// <summary>
        /// 是否激活导航通行状，如果一个格子激活了导航通行状态，则在导航时总认为这个格子是可通行的，无论其是否是障碍物或拥有战斗单位。
        /// </summary>
        public bool NavigationPassable
        {
            get
            {
                return (runtimePasses & EGameConstL.NavigationPassableMask) > 0;
            }
            set
            {
                runtimePasses = value ? (runtimePasses | EGameConstL.NavigationPassableMask) : (runtimePasses & (~EGameConstL.NavigationPassableMask));
            }
        }

        /// <summary>
        /// 更新实时通路情况
        /// </summary>
        /// <param name="updateSelf">true=更新自己，false=更新周围</param>
        public void UpdateRuntimePasses(bool updateSelf)
        {
            GridUnit sibling = null;
            if (updateSelf)
            {
                runtimePasses = roadPasses;

                if (GridType == GridType.Obstacle)
                    return;

                for (int dir = 0; dir < 6; ++dir)
                {
                    if ((runtimePasses & 1 << dir) == 0)
                        continue;

                    sibling = battleMap.GetGridByDir(row, column, dir);
                    //不能通过
                    if (sibling == null || sibling.gridType == GridType.Obstacle || sibling.battleUnit != null)
                        runtimePasses &= ~(1 << dir);
                }

                if (gridUnitRenderer != null)
                    gridUnitRenderer.UpdateGridPassesState();
            }
            else
            {
                //更新周围的格子
                for (int dir = 0; dir < 6; ++dir)
                {
                    sibling = battleMap.GetGridByDir(row, column, dir);
                    if (sibling != null)
                        sibling.UpdateRuntimePasses(true);
                }
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
            UpdateRuntimePasses(false);
        }

        public void OnLeave()
        {
            this.battleUnit = null;
            UpdateRuntimePasses(false);
        }

        public void OnItemPicked()
        {
            GridUnitRefreshItemsEvent e = GridUnitEvent.CreateEvent<GridUnitRefreshItemsEvent>(GridUnitEventType.RefreshItems, this);
            e.itemID = gridItem.item.itemID;
            e.itemCount = 0;
            battleMap.battleField.AppendBattleAction(e);

            gridItem = null;
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

        public override bool Equals(object obj)
        {
            if (obj != null && obj is GridUnit)
            {
                GridUnit data = (GridUnit)obj;
                return data.battleMap.mapID == battleMap.mapID
                    && data.row == row
                    && data.column == column;
            }

            return false;
        }

        public override string ToString()
        {
            if (gridUnitBuff != null)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendFormat("[{0}-({1},{2})]\n", battleMap.mapID, row, column);
                stringBuilder.AppendFormat("Buff:\n");
                stringBuilder.AppendFormat("Buff type: {0}\n", gridUnitBuff.buffType);
                stringBuilder.AppendFormat("Buff addition: {0}", gridUnitBuff.addition);
                return stringBuilder.ToString();
            }
            else
                return string.Format("[{0}-({1},{2})]\n", battleMap.mapID, row, column);
        }

    }
}