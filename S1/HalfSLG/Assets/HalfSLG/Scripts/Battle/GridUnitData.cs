using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    //格子类型
    public enum GridType
    {
        Normal,     //平地
        Obstacle,   //障碍，无法通过
    }

    public class GridUnitData 
    {
        //格子类型
        public GridType gridType;
        //格子所在行列
        public Vector2Int gridPosition;
        //格子所在空间位置
        public Vector3 localPosition;

        //计算两格子之间的距离
        public int Distance(GridUnitData target)
        {
            //计算行移动量
            int rowGap = Mathf.Abs(target.gridPosition.x - gridPosition.x);
            //列范围 - x
            int offset = (((rowGap & 1) == 0) ? 0 : 1) + rowGap / 2;

            //如果在范围内，移动量就是行移动量
            if (target.gridPosition.y >= (gridPosition.y - offset) && (target.gridPosition.y <= (gridPosition.y + offset)))
            {
                //Debug.Log(string.Format("({0},{1})->({2},{3})->{4}", target.row, target.column, row, column, rowGap));
                return rowGap;
            }
            else if (target.gridPosition.y > (gridPosition.y + offset))
            {
                //Debug.Log(string.Format("({0},{1})->({2},{3})->{4}", target.row, target.column, row, column, rowGap + (target.column - column - offset)));
                return rowGap + (target.gridPosition.y - gridPosition.y - offset);
            }
            else
            {
                //Debug.Log(string.Format("({0},{1})->({2},{3})->{4}", target.row, target.column, row, column, rowGap + column - offset - target.column));
                return rowGap + gridPosition.y - offset - target.gridPosition.y;
            }
        }
    }
}