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
        Normal               = 0,       //普通
        Selected             = 1,       //只是被选中
        Start                = 2,       //寻路的起点
        End                  = 4,       //寻路的重点
        Path                 = 8,       //寻路结果经过
        Searched             = 16,      //被搜索过的
        MoveRange            = 32,      //可移动范围
        SkillReleaseRange    = 64,      //技能释放范围
        SkillEffectRange     = 128,     //技能影响范围
    }
    
    public class GridUnitRenderer
        : BaseBehaviour, IVisualRenderer<GridUnit, GridUnitRenderer>
    {
        //格子渲染类型
        [SerializeField] private GridRenderType gridRenderType = GridRenderType.Normal;
        //瓦片渲染器
        [SerializeField] private SpriteRenderer tileRenderer = null;
        //buff节点
        [SerializeField] private SpriteRenderer buffRenderer = null;
        //道具图标
        [SerializeField] private SpriteRenderer itemIconRenderer = null;
        //显示格子的名字
        [SerializeField] private TextMeshPro gridInfo = null;
        
        //关联的格子信息
        public GridUnit gridUnit;

        public void Init()
        {
            tileRenderer.sortingLayerID = EGameConstL.SortingLayer_Battle_Map;
            buffRenderer.sortingLayerID = EGameConstL.SortingLayer_Battle_Map;
            gridInfo.sortingLayerID = EGameConstL.SortingLayer_Battle_Map;
            buffRenderer.sortingLayerID = EGameConstL.SortingLayer_Battle_Map;
            itemIconRenderer.sortingLayerID = EGameConstL.SortingLayer_Battle_Map;
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

        public void RefreshItemIcon(int itemID, int itemCount)
        {
            SO_PackageItem gridItem = null;
            if(itemCount > 0)
                gridItem = PackageItemManager.Instance.GetItem(itemID);

            itemIconRenderer.enabled = gridItem != null;
            if (gridItem == null)
            {
                itemIconRenderer.sprite = null;
                itemIconRenderer.enabled = false;
            }
            else
            {
                itemIconRenderer.sprite = gridItem.icon;
                itemIconRenderer.enabled = true;
            }
        }

        private void RefreshGridBuff()
        {
            //刷新buff
            if (gridUnit.gridUnitBuff == null)
                buffRenderer.enabled = false;
            else
            {
                buffRenderer.enabled = true;
                switch (gridUnit.gridUnitBuff.buffType)
                {
                    case GridUnitBuffType.None:
                    case GridUnitBuffType.Atk:
                        buffRenderer.color = EGameConstL.Color_GridBuff_Atk;
                        break;
                    case GridUnitBuffType.Def:
                        buffRenderer.color = EGameConstL.Color_GridBuff_Def;
                        break;
                    case GridUnitBuffType.Range:
                        buffRenderer.color = EGameConstL.Color_GridBuff_Range;
                        break;
                    default:
                        break;
                }
            }
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
                        tileRenderer.color = Color.white;
                        break;

                    default:
                        tileRenderer.color = Color.white;
                        break;
                }
                return;
            }

            //起点
            if ((gridRenderType & GridRenderType.Start) != GridRenderType.Normal)
            {
                tileRenderer.color = Color.red;
                return;
            }
            //被选中
            else if ((gridRenderType & GridRenderType.Selected) != GridRenderType.Normal)
            {
                tileRenderer.color = Color.red;
                return;
            }
            //寻路终点
            else if ((gridRenderType & GridRenderType.End) != GridRenderType.Normal)
            {
                tileRenderer.color = Color.blue;
                return;
            }
            //寻路路径
            else if ((gridRenderType & GridRenderType.Path) != GridRenderType.Normal)
            {
                tileRenderer.color = Color.yellow;
                return;
            }
            //寻找过的道路（测试用）
            else if ((gridRenderType & GridRenderType.Searched) != GridRenderType.Normal)
            {
                tileRenderer.color = Color.cyan;
                return;
            }
            //技能影响范围
            else if ((gridRenderType & GridRenderType.SkillEffectRange) != GridRenderType.Normal)
            {
                tileRenderer.color = EGameConstL.Color_skillEffectRange;
                return;
            }
            //技能释放范围
            else if ((gridRenderType & GridRenderType.SkillReleaseRange) != GridRenderType.Normal)
            {
                tileRenderer.color = EGameConstL.Color_skillReleaseRange;
                return;
            }
            //移动范围
            else if ((gridRenderType & GridRenderType.MoveRange) != GridRenderType.Normal)
            {
                tileRenderer.color = EGameConstL.Color_moveRange;
                return;
            }
        }

        private void UpdateLocalPosition()
        {
            if(gridUnit != null)
            {
                //刷新order
                transform.localPosition = gridUnit.localPosition;
                int rowOrder = gridUnit.row * EGameConstL.OrderGapPerRow;
                tileRenderer.sortingOrder = rowOrder;
                buffRenderer.sortingOrder = rowOrder + 1;
                itemIconRenderer.sortingOrder = rowOrder + 2;
                gridInfo.sortingOrder = rowOrder + 3;
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
            if(this.gridUnit != null)
            {
                transform.name = this.gridUnit.ToString();
                RefreshColor();
                UpdateLocalPosition();
                UpdateGridPassesState();
                gameObject.SetActive(true);

                RefreshGridBuff();

                int itemID = gridUnit.gridItem == null ? 0 : gridUnit.gridItem.item.itemID;
                int itemCount = gridUnit.gridItem == null ? 0 : gridUnit.gridItem.count;
                RefreshItemIcon(itemID, itemCount);
            }
        }

        public void OnDisconnect()
        {
            gridUnit = null;
            gridRenderType = GridRenderType.Normal;
            transform.SetUnused(false, EGameConstL.STR_Grid);
        }

        public void RefreshRenderer()
        {
            if (gridUnit != null)
            {
                RefreshColor();
                UpdateGridPassesState();
                RefreshGridBuff();

                int itemID = gridUnit.gridItem == null ? 0 : gridUnit.gridItem.item.itemID;
                int itemCount = gridUnit.gridItem == null ? 0 : gridUnit.gridItem.count;
                RefreshItemIcon(itemID, itemCount);
            }
        }

        public void UpdateGridPassesState()
        {
            if (gridUnit != null)
            {
                gridInfo.text = string.Format("{0}    {1}\n\n\n{2}  ({3:00},{4:00})  {5}\n\n{6}    {7}",
                    (gridUnit.runtimePasses & 32) == 0 ? 0 : 1,
                    (gridUnit.runtimePasses & 16) == 0 ? 0 : 1,
                    (gridUnit.runtimePasses & 1) == 0 ? 0 : 1,
                    gridUnit.row, gridUnit.column,
                    (gridUnit.runtimePasses & 8) == 0 ? 0 : 1,
                    (gridUnit.runtimePasses & 2) == 0 ? 0 : 1,
                    (gridUnit.runtimePasses & 4) == 0 ? 0 : 1);
            }
        }

        //运行英雄动作
        public IEnumerator RunGridEvent(GridUnitEvent gridUnitEvent)
        {
            if (gridUnitEvent == null)
                yield break;

            switch (gridUnitEvent.gridUnitEventType)
            {
                case GridUnitEventType.RefreshItems:
                    RunRefreshItemEvent(gridUnitEvent as GridUnitRefreshItemsEvent);
                    break;
                default:
                    break;
            }

            yield return null;
        }

        private void RunRefreshItemEvent(GridUnitRefreshItemsEvent e)
        {
            if (e == null)
                return;

            RefreshItemIcon(e.itemID, e.itemCount);
        }

    }
}