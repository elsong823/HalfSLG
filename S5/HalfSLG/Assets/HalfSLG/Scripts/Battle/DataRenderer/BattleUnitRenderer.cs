using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ELGame
{
    public enum TeamColor
    {
        None,
        Red,
        Blue,
    }

    public class BattleUnitRenderer
            : BaseBehaviour, IVisualRenderer<BattleUnit, BattleUnitRenderer>
    {
        //对应的图片渲染器
        [SerializeField] private SpriteRenderer unitRenderer;
        //用于显示名字、生命值等
        [SerializeField] private TextMeshPro battleUnitInfo;
        //挂特效的节点
        [SerializeField] private Transform effectNode;  
        
        //关联的战斗单位数据
        public BattleUnit battleUnit;

        //关联的信息显示面板(TODO:Event)
        [HideInInspector] public UIViewBattleFieldUnitInfo linkedUnitInfoView;

        private EffectController selectedEffect;
        private List<BattleUnitRenderer> addedEffectBattleUnitRenderers;
        
        //用于区分敌我双方的颜色
        public TeamColor teamColor = TeamColor.None;

        //刷新形象
        private void RefreshFigure()
        {
            switch (teamColor)
            {
                case TeamColor.None:
                    unitRenderer.color = Color.black;
                    break;
                case TeamColor.Red:
                    unitRenderer.color = Color.red;
                    break;
                case TeamColor.Blue:
                    unitRenderer.color = Color.blue;
                    break;
                default:
                    break;
            }
        }
        
        //刷新生命值显示
        private void RefreshAttribute(BattleHeroSyncAttribute attribute)
        {
            if (attribute == null)
                return;

            battleUnitInfo.text = string.Format("BU_{0}_{1}\n{2}/{3}",
                                                battleUnit.battleTeam.teamID,
                                                battleUnit.battleUnitID,
                                                attribute.currentHP,
                                                battleUnit.maxHp);

            //刷新下显示
            if (linkedUnitInfoView != null)
                linkedUnitInfoView.SetArguments(battleUnit.mapGrid, battleUnit.hp > 0 ? battleUnit : null);
        }

        public void UpdatePositionByGrid(GridUnit gridUnit)
        {
            if (battleUnit != null)
            {
                transform.localPosition = gridUnit.localPosition;
                unitRenderer.sortingOrder = gridUnit.row * EGameConstL.OrderGapPerRow + EGameConstL.OrderIncrease_BattleUnit;
                battleUnitInfo.sortingOrder = gridUnit.row * EGameConstL.OrderGapPerRow + EGameConstL.OrderIncrease_BattleUnit;
            }
        }

        public void OnConnect(BattleUnit unit)
        {
            battleUnit = unit;
            if (battleUnit != null)
            {
                transform.name = battleUnit.ToString();
                //刷新形象
                RefreshFigure();
                //设置名字
                battleUnitInfo.text = string.Format("BU_{0}_{1}", battleUnit.battleTeam.teamID, battleUnit.battleUnitID);
                //先移出
                transform.ShiftOut();
                gameObject.SetActive(true);
            }
        }

        public void OnDisconnect()
        {
            battleUnit = null;
            teamColor = TeamColor.None;
            transform.SetUnused(false);
        }

        //运行英雄动作
        public IEnumerator RunHeroAction(BattleHeroAction heroAction)
        {
            if (heroAction == null)
                yield break;

            switch (heroAction.actionType)
            {
                //进入战场
                case MsgBattleHeroActionType.EnterBattleField:
                    yield return PlayEnterBattleFieldAction(heroAction as BattleHeroEnterBattleFieldAction);
                    break;

                //切换目标
                case MsgBattleHeroActionType.ChangeTarget:
                    yield return PlayChangeTargetAction(heroAction as BattleHeroChangeTargetAction);
                    break;

                //移动
                case MsgBattleHeroActionType.MotionAction:
                    yield return PlayMotionAction(heroAction as BattleHeroMotionAction);
                    break;

                //使用技能
                case MsgBattleHeroActionType.SkillAction:
                    yield return PlaySkillAction(heroAction as BattleHeroSkillAction);
                    break;

                //角色被击败
                case MsgBattleHeroActionType.Defeated:
                    yield return PlayDefeatedAction(heroAction as BattleHerodDefeatedAction);
                    break;

                //一个警告，用于调试，不应该出现这种问题
                case MsgBattleHeroActionType.Warning:
                    Debug.LogWarning(heroAction.ToString());
                    yield return EGameConstL.WaitForTouchScreen;
                    break;
                
                //等待玩家手动操作
                case MsgBattleHeroActionType.Manual:
                    yield return PlayManualAction(heroAction as BattleHeroManualAction);
                    break;

                default:
                    UtilityHelper.LogError("Error type of hero action:" + heroAction.actionType);
                    break;
            }
            yield return null;
        }

        //进入战场
        private IEnumerator PlayEnterBattleFieldAction(BattleHeroEnterBattleFieldAction action)
        {
            if (action == null)
            {
                UtilityHelper.LogError("Error BattleHeroEnterBattleFieldAction");
                yield break;
            }

            //设置位置
            UpdatePositionByGrid(action.gridUnit);
            RefreshAttribute(action.attribute);
        }

        //切换目标
        private IEnumerator PlayChangeTargetAction(BattleHeroChangeTargetAction action)
        {
            if (action == null)
            {
                UtilityHelper.LogError("Error BattleHeroChangeTargetAction");
                yield break;
            }

            AddBattleBeSelected(this, EGameConstL.Color_Cyan);

            AddBattleBeSelected(action.newTargetUnit.battleUnitRenderer, EGameConstL.Color_Yellow);

            //yield return EGameConstL.WaitForTouchedScreen;

            ClearAllSelectedBattleUnit();
        }

        //移动
        private IEnumerator PlayMotionAction(BattleHeroMotionAction action)
        {
            if (action == null)
            {
                UtilityHelper.LogError("Error BattleHeroMotionAction");
                yield break;
            }

            AddBattleBeSelected(this, EGameConstL.Color_Cyan);

            //手动操作的移动可能没有攻击目标
            if(action.targetUnit != null)
                AddBattleBeSelected(action.targetUnit.battleUnitRenderer, EGameConstL.Color_Yellow);

            //先显示区域
            BattleFieldRenderer.Instance.SetRangeHighlightActive(true, action.fromGrid.row, action.fromGrid.column, action.moveRange);

            //显示路径
            BattleFieldRenderer.Instance.SetPathHighlightActive(true, action.gridPath);

            //yield return EGameConstL.WaitForTouchedScreen;

            //移动
            for (int i = 0; i < action.gridPath.Length; ++i)
            {
                UpdatePositionByGrid(action.gridPath[i]);
                yield return EGameConstL.WaitForDotOneSecond;
            }

            //清空范围和路径
            BattleFieldRenderer.Instance.SetRangeHighlightActive(false, 0, 0, 0);

            //显示路径
            BattleFieldRenderer.Instance.SetPathHighlightActive(false, null);

            ClearAllSelectedBattleUnit();
        }

        //使用技能
        private IEnumerator PlaySkillAction(BattleHeroSkillAction action)
        {
            if (action == null)
            {
                UtilityHelper.LogError("Error BattleHeroSkillAction");
                yield break;
            }


            if(action.skillResult == null)
            {
                UtilityHelper.LogError("Error BattleHeroSkillAction: No skill result.");
                yield break;
            }

            AddBattleBeSelected(this, EGameConstL.Color_Cyan);

            for (int i = 0; i < action.skillResult.Length; ++i)
            {
                if (action.skillResult[i].battleUnit != null && action.skillResult[i].battleUnit.battleUnitRenderer != null)
                {
                    AddBattleBeSelected(action.skillResult[i].battleUnit.battleUnitRenderer, EGameConstL.Color_Yellow);
                    yield return EGameConstL.WaitForHalfSecond;
                    action.skillResult[i].battleUnit.battleUnitRenderer.RefreshAttribute(action.skillResult[i].syncAttribute);
                }
            }

           // yield return EGameConstL.WaitForTouchedScreen;

            ClearAllSelectedBattleUnit();
        }

        //被打服了
        private IEnumerator PlayDefeatedAction(BattleHerodDefeatedAction action)
        {
            if (action == null)
            {
                UtilityHelper.LogError("Error BattleHerodDefeatedAction");
                yield break;
            }
            
            transform.ShiftOut();
        }

        //等待玩家的操作
        private IEnumerator PlayManualAction(BattleHeroManualAction action)
        {
            if (action == null)
            {
                UtilityHelper.LogError("Error BattleHeroManualAction");
                yield break;
            }

            AddBattleBeSelected(this, EGameConstL.Color_GreenApple);
            
            //通知战场管理器
            BattleFieldRenderer.Instance.SetManualBattleUnit(this);

        }

        //添加特效
        private void AddEffect(EffectController effect)
        {
            if (selectedEffect == null)
            {
                effect.transform.SetParent(effectNode);
                effect.transform.Normalize();
                effect.gameObject.SetActive(true);
                effect.Color = Color.cyan;
                effect.SortingLayer = unitRenderer.sortingLayerID;
                effect.SortingOrder = unitRenderer.sortingOrder + EGameConstL.OrderIncrease_BattleUnit;
                selectedEffect = effect;
            }
        }

        //移除特效
        private void RemoveEffect()
        {
            if (selectedEffect != null)
            {
                EffectManager.Instance.ReturnEffect(selectedEffect);
                selectedEffect = null;
            }
        }

        //是否显示被选中的状态
        private void AddBattleBeSelected(BattleUnitRenderer beSelected, Color color)
        {
            if (beSelected == null)
                return;

            if (addedEffectBattleUnitRenderers == null)
                addedEffectBattleUnitRenderers = new List<BattleUnitRenderer>();

            var selectedEffect = EffectManager.Instance.GetEffectObject(string.Empty);
            if (selectedEffect != null)
            {
                beSelected.AddEffect(selectedEffect);
                addedEffectBattleUnitRenderers.Add(beSelected);
                selectedEffect.Color = color;
            }
        }

        //清空所有被选中
        private void ClearAllSelectedBattleUnit()
        {
            if (addedEffectBattleUnitRenderers != null)
            {
                for (int i = 0; i < addedEffectBattleUnitRenderers.Count; ++i)
                {
                    addedEffectBattleUnitRenderers[i].RemoveEffect();
                }
                addedEffectBattleUnitRenderers.Clear();
            }
        }
    }
}