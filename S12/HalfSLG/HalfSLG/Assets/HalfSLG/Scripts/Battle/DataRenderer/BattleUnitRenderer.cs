using System.Collections;
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

        [SerializeField] private BattleUnitHealthBar battleUnitHealthBar;

        public void Init()
        {
            unitRenderer.sortingLayerID = EGameConstL.SortingLayer_Battle_Map;
            frameRenderer.sortingLayerID = EGameConstL.SortingLayer_Battle_Map;
            battleUnitInfo.sortingLayerID = EGameConstL.SortingLayer_Battle_Map;
            battleUnitHealthBar.Init();
        }

        //刷新值显示
        private void RefreshAttribute(BattleUnitSyncAttribute attribute)
        {
            if (battleUnit == null || attribute == null)
                return;
            
            battleUnitInfo.text = battleUnit.battleUnitAttribute.battleUnitName;

            //刷新生命值
            battleUnitHealthBar.UpdateHealth(attribute.currentHP, battleUnit.battleUnitAttribute.maxHp);
            //刷新能量
            battleUnitHealthBar.UpdateEnergy(attribute.currentEnergy, battleUnit.battleUnitAttribute.maxEnergy);

            //刷新下显示
            if (linkedUnitInfoView != null)
                linkedUnitInfoView.SetArguments(battleUnit.mapGrid, battleUnit.battleUnitAttribute.hp > 0 ? battleUnit : null);
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

        public void RefreshRenderer()
        {
            UpdateRenderState(BattleUnitRenderState.Normal);
        }

        //运行英雄动作
        public IEnumerator RunHeroAction(BattleUnitActionEvent heroAction)
        {
            if (heroAction == null)
                yield break;

            switch (heroAction.battleUnitActionType)
            {
                case BattleUnitActionType.EnterBattleField:
                    yield return PlayEnterBattleFieldAction(heroAction as BattleUnitEnterBattleFieldAction);
                    break;
                case BattleUnitActionType.ChangeTarget:
                    yield return PlayChangeTargetAction(heroAction as BattleUnitChangeTargetAction);
                    break;
                case BattleUnitActionType.MoveToTarget:
                    yield return PlayMotionAction(heroAction as BattleUnitMotionAction);
                    break;
                case BattleUnitActionType.UseSkill:
                    yield return PlaySkillAction(heroAction as BattleUnitSkillAction);
                    break;
                case BattleUnitActionType.AttributeUpdate:
                    yield return PlayAttributeUpdateAction(heroAction as BattleUnitAttributeUpdate);
                    break;
                case BattleUnitActionType.ManualOperate:
                    yield return PlayManualAction(heroAction as BattleUnitManualAction);
                    break;
                case BattleUnitActionType.PickupItem:
                    yield return PlayPickupItemAction(heroAction as BattleUnitPickupItemAction);
                    break;
                case BattleUnitActionType.UseItem:
                    yield return PlayUseItemAction(heroAction as BattleUnitUseItemAction);
                    break;
                case BattleUnitActionType.Warning:
                    UtilityHelper.LogWarning(heroAction.ToString());
                    yield return EGameConstL.WaitForTouchScreen;
                    break;
                default:
                    break;
            }
            yield return null;
        }

        //进入战场
        private IEnumerator PlayEnterBattleFieldAction(BattleUnitEnterBattleFieldAction action)
        {
            if (action == null)
            {
                UtilityHelper.LogError("Error BattleHeroEnterBattleFieldAction");
                yield break;
            }

            //设置位置
            UpdatePositionByGrid(action.bornGrid);
            RefreshAttribute(action.attribute);
        }

        //更新数值
        private IEnumerator PlayAttributeUpdateAction(BattleUnitAttributeUpdate action)
        {
            RefreshAttribute(action.attribute);
            yield return EGameConstL.WaitForDotOneSecond;
        }

        //切换目标
        private IEnumerator PlayChangeTargetAction(BattleUnitChangeTargetAction action)
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
        private IEnumerator PlayMotionAction(BattleUnitMotionAction action)
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
            if (!battleUnit.battleUnitAttribute.manualOperation)
                UpdateRenderState(BattleUnitRenderState.Normal);
        }

        //使用技能
        private IEnumerator PlaySkillAction(BattleUnitSkillAction action)
        {
            if (action == null)
            {
                UtilityHelper.LogError("Error BattleHeroSkillAction");
                yield break;
            }


            if (action.skillResult == null)
            {
                UtilityHelper.LogError("Error BattleHeroSkillAction: No skill result.");
                yield break;
            }

            //释放前
            yield return BeforeReleaseSkill(action);

            //释放
            yield return ReleaseSkill(action);

            //释放后
            yield return AfterReleaseSkill(action);
        }

        //被打服了
        private IEnumerator PlayDefeatedAction()
        {
            transform.ShiftOut();
            yield return null;
        }

        //等待玩家的操作
        private IEnumerator PlayManualAction(BattleUnitManualAction action)
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

        //拾取道具
        private IEnumerator PlayPickupItemAction(BattleUnitPickupItemAction action)
        {
            NormalMessage normalMessage = new NormalMessage(EGameConstL.EVENT_BATTLE_UNIT_PACKAGE_CHANGED);
            normalMessage.Body = battleUnit.package;

            EventManager.Instance.Run(EGameConstL.EVENT_BATTLE_UNIT_PACKAGE_CHANGED, normalMessage);

            yield break;
        }

        //使用道具
        private IEnumerator PlayUseItemAction(BattleUnitUseItemAction action)
        {
            if (action.attributeUpdate != null)
            {
                if (action.attributeUpdate.hpChanged != 0)
                {
                    PlayDamageLabel(action.attributeUpdate.hpChanged, BattleSkillDamageType.Heal);
                }
                RefreshAttribute(action.attributeUpdate);
            }
            yield break;
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

        //战斗表现生命周期
        //释放技能前
        private IEnumerator BeforeReleaseSkill(BattleUnitSkillAction action)
        {
            //将自己显示为行动状态~变身！
            UpdateRenderState(BattleUnitRenderState.Action);

            //仅有范围的技能显示范围呢
            if (action.battleSkill.effectRadius > 0)
            {
                GridUnit centerGrid = null;
                switch (action.battleSkill.targetType)
                {
                    case BattleSkillTargetType.BattleUnit:
                    case BattleSkillTargetType.Self:
                        break;
                        centerGrid = action.targetBattleUnit.mapGrid;
                        break;
                    case BattleSkillTargetType.GridUnit:
                        centerGrid = action.targetGrid;
                        break;
                    default:
                        break;
                }
                if (centerGrid != null)
                {
                    BattleFieldRenderer.Instance.SetCircularRangeRenderStateActive(
                        true, 
                        GridRenderType.SkillEffectRange, 
                        centerGrid.row, centerGrid.column, 
                        action.battleSkill.effectRadius);
                }
            }
            //受到技能影响的单位高亮(暂时)
            for (int i = 0; i < action.skillResult.Length; ++i)
            {
                action.skillResult[i].battleUnit.battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Selected);
            }

            yield return EGameConstL.WaitForHalfSecond;
        }

        //释放技能时
        private IEnumerator ReleaseSkill(BattleUnitSkillAction action)
        {
            //取消范围显示
            if (action.battleSkill.effectRadius > 0)
            {
                BattleFieldRenderer.Instance.SetCircularRangeRenderStateActive(false, GridRenderType.SkillEffectRange);
            }

            //受到技能影响的单位还原
            for (int i = 0; i < action.skillResult.Length; ++i)
            {
                action.skillResult[i].battleUnit.battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);
            }

            yield return EGameConstL.WaitForHalfSecond;

            //属性刷新
            RefreshAttribute(action.selfAttribute);

            //展示
            for (int i = 0; i < action.skillResult.Length; ++i)
            {
                //同时开启掉血特效
                StartCoroutine(action.skillResult[i].battleUnit.battleUnitRenderer.OnSkillDamage(action.skillResult[i]));
            }
        }

        //释放技能后
        private IEnumerator AfterReleaseSkill(BattleUnitSkillAction action)
        {
            UpdateRenderState(BattleUnitRenderState.Normal);
            yield return null;
        }

        private void PlayDamageLabel(int value, BattleSkillDamageType damageType)
        {
            //播放掉血特效
            EffectDamageLabel damageEffect = EffectManager.Instance.CreateEffectByName<EffectDamageLabel>(EGameConstL.Effect_DamageLabel, EffectPlayType.WorldPosition);
            if (damageEffect != null)
            {
                damageEffect.SortingLayer = EGameConstL.SortingLayer_Battle_Effect;
                damageEffect.gameObject.SetActive(true);
                damageEffect.transform.position = unitRenderer.transform.position;
                damageEffect.SetDamage(value, damageType);
            }

        }

        //技能命中时
        private IEnumerator OnSkillDamage(BattleUnitSkillResult skillResult)
        {
            if (skillResult == null)
                yield return null;

            PlayDamageLabel(skillResult.syncAttribute.hpChanged, skillResult.battleSkill.damageType);

            //更新血条
            skillResult.battleUnit.battleUnitRenderer.RefreshAttribute(skillResult.syncAttribute);

            //判断是否跪了
            if (skillResult.syncAttribute.currentHP <= 0)
            {
                yield return EGameConstL.WaitForHalfSecond;
                //离场
                yield return skillResult.battleUnit.battleUnitRenderer.PlayDefeatedAction();
            }
        }

    }
}