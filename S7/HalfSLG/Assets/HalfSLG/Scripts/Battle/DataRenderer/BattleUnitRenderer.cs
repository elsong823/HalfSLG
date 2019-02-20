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

    public enum BattleUnitRenderState
    {
        Normal,         //正常状态
        Action,         //行动中的
        Selectable,     //可选的
        NotSelectable,  //不可选的
        Selected,       //已选中的
    }

    public class BattleUnitRenderer
            : BaseBehaviour, 
              IVisualRenderer<BattleUnit, BattleUnitRenderer>
    {
        //对应的图片渲染器
        [SerializeField] private SpriteRenderer unitRenderer;
        [SerializeField] private SpriteRenderer frameRenderer;
        //用于显示名字、生命值等
        [SerializeField] private TextMeshPro battleUnitInfo;
        
        //关联的战斗单位数据
        public BattleUnit battleUnit;

        //关联的信息显示面板(TODO:Event)
        [HideInInspector] public UIViewBattleFieldUnitInfo linkedUnitInfoView;

        //用于区分敌我双方的颜色
        public TeamColor teamColor = TeamColor.None;
        private BattleUnitRenderState targetRendererState = BattleUnitRenderState.Normal;

        //血条
        [SerializeField] Transform healthBarNode;
        [SerializeField] BattleUnitHealthBar battleUnitHealthPrefab;
        private BattleUnitHealthBar battleUnitHealthBar;

        //技能(暂时放到这里，方便调试)
        [Space]
        public List<SO_BattleSkill> battleSkills; //手动操作可使用的技能
        public SO_BattleSkill AISkill;            //ai可使用的技能（目前没有AI，因此只放近战攻击）

        public override void Init(params object[] args)
        {
            unitRenderer.sortingLayerID = EGameConstL.SortingLayer_Battle_Map;
            frameRenderer.sortingLayerID = EGameConstL.SortingLayer_Battle_Map;
            battleUnitInfo.sortingLayerID = EGameConstL.SortingLayer_Battle_Map;
        }

        private BattleUnitHealthBar HealthBar
        {
            get
            {
                if (battleUnitHealthBar == null && battleUnitHealthPrefab != null)
                {
                    battleUnitHealthBar = Instantiate<BattleUnitHealthBar>(battleUnitHealthPrefab);
                    battleUnitHealthBar.transform.SetParent(healthBarNode);
                    battleUnitHealthBar.transform.Normalize();
                    battleUnitHealthBar.Init();
                }
                return battleUnitHealthBar;
            }
        }

        //刷新生命值显示
        private void RefreshAttribute(BattleHeroSyncAttribute attribute)
        {
            if (attribute == null)
                return;

            battleUnitInfo.text = string.Format("BU_{0}_{1}",
                                                battleUnit.battleTeam.teamID,
                                                battleUnit.battleUnitID);

            //刷新生命值
            HealthBar.UpdateHealth(attribute.currentHP, battleUnit.maxHp);

            //刷新下显示
            if (linkedUnitInfoView != null)
                linkedUnitInfoView.SetArguments(battleUnit.mapGrid, battleUnit.hp > 0 ? battleUnit : null);
        }

        //受到技能伤害
        private void OnSkillDamage(BattleHeroSkillResult skillResult)
        {
            if (skillResult == null)
                return;

            //播放掉血特效
            EffectDamageLabel damageEffect = EffectManager.Instance.CreateEffectByName<EffectDamageLabel>(EGameConstL.Effect_DamageLabel, EffectPlayType.WorldPosition);
            if (damageEffect != null)
            {
                damageEffect.SortingLayer = EGameConstL.SortingLayer_Battle_Effect;
                damageEffect.gameObject.SetActive(true);
                damageEffect.transform.position = unitRenderer.transform.position;
                damageEffect.SetDamage(skillResult.syncAttribute.hpChanged, skillResult.battleSkill.damageType);
            }
        }

        public void UpdatePositionByGrid(GridUnit gridUnit)
        {
            if (battleUnit != null)
            {
                transform.localPosition = gridUnit.localPosition;
                unitRenderer.sortingOrder = gridUnit.row * EGameConstL.OrderGapPerRow + EGameConstL.OrderIncrease_BattleUnit;
                frameRenderer.sortingOrder = gridUnit.row * EGameConstL.OrderGapPerRow + EGameConstL.OrderIncrease_BattleUnit + 1;
                battleUnitInfo.sortingOrder = gridUnit.row * EGameConstL.OrderGapPerRow + EGameConstL.OrderIncrease_BattleUnit + 2;
            }
        }

        public void OnConnect(BattleUnit unit)
        {
            battleUnit = unit;
            if (battleUnit != null)
            {
                transform.name = battleUnit.ToString();
                //刷新形象
                UpdateRenderState(BattleUnitRenderState.Normal);
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
            transform.SetUnused(false, EGameConstL.STR_BattleUnit);
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

            UpdateRenderState(BattleUnitRenderState.Action);
            action.newTargetUnit.battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Selected);

            yield return EGameConstL.WaitForHalfSecond;

            UpdateRenderState(BattleUnitRenderState.Normal);
            action.newTargetUnit.battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);
        }

        //移动
        private IEnumerator PlayMotionAction(BattleHeroMotionAction action)
        {
            if (action == null)
            {
                UtilityHelper.LogError("Error BattleHeroMotionAction");
                yield break;
            }

            UpdateRenderState(BattleUnitRenderState.Action);
            //手动操作的移动可能没有攻击目标
            if (action.targetUnit != null)
                action.targetUnit.battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Selected);

            //先显示区域
            BattleFieldRenderer.Instance.SetCircularRangeRenderStateActive(
                true, 
                GridRenderType.MoveRange, 
                action.fromGrid.row, 
                action.fromGrid.column, 
                action.moveRange);

            //显示路径
            BattleFieldRenderer.Instance.SetGridsRenderStateActive(true, action.gridPath);

            //yield return EGameConstL.WaitForTouchedScreen;

            //移动
            for (int i = 0; i < action.gridPath.Length; ++i)
            {
                UpdatePositionByGrid(action.gridPath[i]);
                yield return EGameConstL.WaitForDotOneSecond;
            }

            //清空范围和路径
            BattleFieldRenderer.Instance.SetCircularRangeRenderStateActive(false, GridRenderType.MoveRange);

            //显示路径
            BattleFieldRenderer.Instance.SetGridsRenderStateActive(false, null);

            //手动操作的移动可能没有攻击目标
            if (action.targetUnit != null)
                action.targetUnit.battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);
            
            //TEMP
            if(!battleUnit.manual)
                UpdateRenderState(BattleUnitRenderState.Normal);
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

            UpdateRenderState(BattleUnitRenderState.Action);

            for (int i = 0; i < action.skillResult.Length; ++i)
            {
                if (action.skillResult[i].battleUnit != null && action.skillResult[i].battleUnit.battleUnitRenderer != null)
                {
                    action.skillResult[i].battleUnit.battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Selected);

                    yield return EGameConstL.WaitForHalfSecond;

                    action.skillResult[i].battleUnit.battleUnitRenderer.OnSkillDamage(action.skillResult[i]);
                    action.skillResult[i].battleUnit.battleUnitRenderer.RefreshAttribute(action.skillResult[i].syncAttribute);
                }
            }


            UpdateRenderState(BattleUnitRenderState.Normal);

            for (int i = 0; i < action.skillResult.Length; ++i)
            {
                if (action.skillResult[i].battleUnit != null && action.skillResult[i].battleUnit.battleUnitRenderer != null)
                    action.skillResult[i].battleUnit.battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);
            }
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

            UpdateRenderState(BattleUnitRenderState.Action);
            
            //通知战场管理器
            BattleFieldRenderer.Instance.SetManualBattleUnit(this);
        }

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
        
        //改变显示状态
        public void UpdateRenderState(BattleUnitRenderState renderState)
        {
            this.targetRendererState = renderState;

            frameRenderer.enabled = this.targetRendererState != BattleUnitRenderState.Normal;

            switch (this.targetRendererState)
            {
                case BattleUnitRenderState.Normal:
                    RefreshFigure();
                    break;

                    //行动的
                case BattleUnitRenderState.Action:
                    frameRenderer.color = EGameConstL.Color_battleUnitAction;
                    break;

                    //可选的
                case BattleUnitRenderState.Selectable:
                    frameRenderer.color = EGameConstL.Color_battleUnitSelectable;
                    break;

                    //不可选的
                case BattleUnitRenderState.NotSelectable:
                    frameRenderer.color = Color.gray;
                    break;

                    //已选的
                case BattleUnitRenderState.Selected:
                    frameRenderer.color = Color.yellow;
                    break;

                default:
                    UtilityHelper.LogError("Update Target RendererState failed, unknown state:" + renderState.ToString());
                    break;
            }
        }
    }
}