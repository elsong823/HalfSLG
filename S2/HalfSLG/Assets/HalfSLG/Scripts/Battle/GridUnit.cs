using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    //格子绘制时的类型
    public enum GridRenderType
    {
        Normal,     //普通
        Selected,   //只是被选中
        Start,      //寻路的起点
        End,        //寻路的重点
        Path,       //寻路结果经过
        Searched,   //被搜索过的
        Range,      //范围
    }

    public class GridUnit
        : ELBehaviour
    {
        public GridUnitData gridData;
        [SerializeField] private GridRenderType gridRenderType;
        [SerializeField] private SpriteRenderer tileRenderer;
        
        public GridRenderType GridRenderType
        {
            set
            {
                gridRenderType = value;
                RefreshColor();
            }
            get
            {
                return gridRenderType;
            }
        }

        public void RefreshColor()
        {
            switch (gridRenderType)
            {
                case GridRenderType.Start:
                    tileRenderer.color = Color.red;
                    return;
                case GridRenderType.Selected:
                    tileRenderer.color = Color.red;
                    return;
                case GridRenderType.End:
                    tileRenderer.color = Color.blue;
                    return;
                case GridRenderType.Path:
                    tileRenderer.color = Color.yellow;
                    return;
                case GridRenderType.Searched:
                    tileRenderer.color = Color.cyan;
                    return;
                case GridRenderType.Range:
                    tileRenderer.color = Color.cyan;
                    return;
                default:
                    break;
            }

            //根据格子类型切换颜色
            switch (gridData.GridType)
            {
                case GridType.Normal:
                    tileRenderer.color = Color.white;
                    break;

                case GridType.Obstacle:
                    tileRenderer.color = Color.gray;
                    break;
                case GridType.Born:
                    tileRenderer.color = Color.green;
                    break;
                default:
                    tileRenderer.color = Color.white;
                    break;
            }
        }

        public override bool Equals(object other)
        {
            if (other is GridUnit)
            {
                return GetInstanceID() == ((GridUnit)other).GetInstanceID();
            }
            return false;
        }
    }
}