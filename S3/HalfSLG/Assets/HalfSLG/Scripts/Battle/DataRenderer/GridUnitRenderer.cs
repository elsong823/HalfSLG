using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    public class GridUnitRenderer
        : BaseBehaviour, IVisualRenderer<GridUnit, GridUnitRenderer>
    {
        //格子渲染类型
        [SerializeField] private GridRenderType gridRenderType;
        //瓦片渲染器
        [SerializeField] private SpriteRenderer tileRenderer = null;
        //显示格子的名字
        [SerializeField] private TextMeshPro gridInfo = null;
        //特效节点
        [SerializeField] private Transform effectNode = null;
        
        //关联的格子信息
        public GridUnit gridUnit;

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
            switch (gridUnit.GridType)
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

        private void UpdateLocalPosition()
        {
            if(gridUnit != null)
            {
                //刷新order
                transform.localPosition = gridUnit.localPosition;
                tileRenderer.sortingOrder = gridUnit.row * EGameConstL.OrderGapPerRow;
                gridInfo.sortingOrder = gridUnit.row * EGameConstL.OrderGapPerRow;
            }
        }

        public override bool Equals(object other)
        {
            if (other is GridUnitRenderer)
            {
                return GetInstanceID() == ((GridUnitRenderer)other).GetInstanceID();
            }
            return false;
        }

        public void OnConnect(GridUnit unit)
        {
            gridUnit = unit;
            if(gridUnit != null)
            {
                transform.name = gridUnit.ToString();
                RefreshColor();
                UpdateLocalPosition();
                gridInfo.text = string.Format("({0},{1})", gridUnit.row, gridUnit.column);
                gameObject.SetActive(true);
            }
        }

        public void OnDisconnect()
        {
            gridUnit = null;
            gridRenderType = GridRenderType.Normal;
            transform.SetUnused(false);
        }
    }
}