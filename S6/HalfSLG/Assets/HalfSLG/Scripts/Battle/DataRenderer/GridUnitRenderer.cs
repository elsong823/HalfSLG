using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ELGame
{
    //格子绘制时的类型
    [Flags]
    public enum GridRenderType
    {
        Normal   = 0,       //普通
        Selected = 1,       //只是被选中
        Start    = 2,       //寻路的起点
        End      = 4,       //寻路的重点
        Path     = 8,       //寻路结果经过
        Searched = 16,      //被搜索过的
        Range    = 32,      //范围
    }

    public class GridUnitRenderer
        : BaseBehaviour, IVisualRenderer<GridUnit, GridUnitRenderer>
    {
        //格子渲染类型
        [SerializeField] private GridRenderType gridRenderType = GridRenderType.Normal;
        //瓦片渲染器
        [SerializeField] private SpriteRenderer tileRenderer = null;
        //显示格子的名字
        [SerializeField] private TextMeshPro gridInfo = null;
        //特效节点
        [SerializeField] private Transform effectNode = null;
        
        //关联的格子信息
        public GridUnit gridUnit;

        public override void Init(params object[] args)
        {
            tileRenderer.sortingLayerID = EGameConstL.SortingLayer_Battle_Map;
            gridInfo.sortingLayerID = EGameConstL.SortingLayer_Battle_Map;
        }

        private GridRenderType GridRenderType
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

        public void AppendGridRenderType(GridRenderType renderType)
        {
            gridRenderType |= renderType;
            RefreshColor();
        }

        public void RemoveGridRenderType(GridRenderType renderType)
        {
            gridRenderType &= (~renderType);
            RefreshColor();
        }

        public void ResetGridRenderType()
        {
            gridRenderType = GridRenderType.Normal;
            RefreshColor();
        }

        public void RefreshColor()
        {
            if (gridRenderType == GridRenderType.Normal || gridUnit.GridType == GridType.Obstacle)
            {
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
                return;
            }

            if ((gridRenderType & GridRenderType.Start) != GridRenderType.Normal)
            {
                tileRenderer.color = Color.red;
                return;
            }
            else if ((gridRenderType & GridRenderType.Selected) != GridRenderType.Normal)
            {
                tileRenderer.color = Color.red;
                return;
            }
            else if ((gridRenderType & GridRenderType.End) != GridRenderType.Normal)
            {
                tileRenderer.color = Color.blue;
                return;
            }
            else if ((gridRenderType & GridRenderType.Path) != GridRenderType.Normal)
            {
                tileRenderer.color = Color.yellow;
                return;
            }
            else if ((gridRenderType & GridRenderType.Searched) != GridRenderType.Normal)
            {
                tileRenderer.color = Color.cyan;
                return;
            }
            else if ((gridRenderType & GridRenderType.Range) != GridRenderType.Normal)
            {
                tileRenderer.color = Color.cyan;
                return;
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
            if (other != null && other is GridUnitRenderer)
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
            transform.SetUnused(false, EGameConstL.STR_Grid);
        }
    }
}