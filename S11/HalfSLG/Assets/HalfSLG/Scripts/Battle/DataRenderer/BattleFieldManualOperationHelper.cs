using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public enum ManualOperationState
    {
        Waiting,              //等待中
        Select,               //准备选择目标
        Move,                 //准备选择移动地点
        Skill,                //准备使用技能
    }

    public enum SkillOperationType
    {
        None,
        SingleBattleUnitTarget,     //指定一个战斗单位
        SurroundBattleUnit,         //指定一个战斗单位，同时造成环绕伤害
        SurroundSelf,               //以自己为中心的范围伤害
        GridUnitTarget,             //指定一个位置，造成范围伤害
    }

    [System.Serializable]
    public class BattleFieldManualOperationHelper
    {
        //所属战场显示器
        private BattleFieldRenderer fieldRenderer = null;

        //手动操作记录的内容
        private BattleUnitRenderer manualOperatingBattleUnitRenderer = null;    //当前手动操作的战斗单位
        private BattleUnitRenderer selectedBattleUnitRenderer = null;           //当前临时选中的战斗单位
        private GridUnitRenderer selectedGridUnitRenderer = null;               //当前临时选中的地块
        private BattleSkillManualReleaseAnalysisor usedManualReleaseAnalysisor = null;         //手动释放技能分析器

        //当前被特殊显示的地图格子
        private List<GridUnit> moveRangeGridUnits = new List<GridUnit>(20);             //移动范围
        private List<GridUnit> skillReleaseRangeGridUnits = new List<GridUnit>(10);     //技能释放范围
        private List<GridUnit> skillEffectRangeGridUnits = new List<GridUnit>(10);      //技能影响范围
        private List<GridUnit> pathHighlightGridUnits = new List<GridUnit>(10);         //某一条路径

        //操作状态及手动操作类型
        private ManualOperationState manualOperationState = ManualOperationState.Waiting;   //当前操作状态
        private SkillOperationType skillOperationType = SkillOperationType.None;            //手动操作状态下的具体操作类型

        //构造：创建时必须传入一个战场显示器
        public BattleFieldManualOperationHelper(BattleFieldRenderer renderer)
        {
            fieldRenderer = renderer;
        }

        //设置了手动操作的战斗单位渲染器
        public BattleUnitRenderer ManualOperatingBattleUnitRenderer
        {
            set
            {
                manualOperatingBattleUnitRenderer = value;
                manualOperationState = manualOperatingBattleUnitRenderer == null ? ManualOperationState.Waiting : ManualOperationState.Select;
            }
        }

        //某个战斗单位点击了移动
        public void BattleUnitMove(BattleUnit battleUnit)
        {
            if (battleUnit == null
                || battleUnit.battleUnitRenderer == null
                || !battleUnit.battleUnitRenderer.Equals(manualOperatingBattleUnitRenderer))
            {
                UtilityHelper.LogError("Battle unit move failed.");
                return;
            }

            //显示移动范围
            SetCircularRangeRenderStateActive(
                true,
                GridRenderType.MoveRange,
                battleUnit.mapGrid.row,
                battleUnit.mapGrid.column,
                battleUnit.battleUnitAttribute.mobility);

            //设定为移动状态
            manualOperationState = ManualOperationState.Move;

            //关闭选择操作界面
            HideManualActionList();
        }

        //某个战斗单位点击了待命
        public void BattleUnitStay(BattleUnit battleUnit)
        {
            if (battleUnit == null
                || battleUnit.battleUnitRenderer == null
                || !battleUnit.battleUnitRenderer.Equals(manualOperatingBattleUnitRenderer))
            {
                UtilityHelper.LogError("Battle unit stay failed.");
                return;
            }
            ManualOperationComplete();
        }

        //某个战斗单位点击了使用技能
        public void BattleUnitUseSkill(BattleUnit battleUnit, SO_BattleSkill skill)
        {
            if (battleUnit == null
                || battleUnit.battleUnitRenderer == null
                || !battleUnit.battleUnitRenderer.Equals(manualOperatingBattleUnitRenderer))
            {
                UtilityHelper.LogError("Battle unit use skill failed.");
                HideManualActionList();
                return;
            }

            //获取推算的技能释放结果
            usedManualReleaseAnalysisor = BattleCalculator.Instance.ManualReleaseAnalysisor;

            //分析结果
            usedManualReleaseAnalysisor.Analysis(manualOperatingBattleUnitRenderer.battleUnit, skill);
            
            //显示技能释放范围
            if (skill.GetReleaseRadius(battleUnit.mapGrid) > 0)
            {
                SetCircularRangeRenderStateActive(
                    true,
                    GridRenderType.SkillReleaseRange,
                    battleUnit.mapGrid.row,
                    battleUnit.mapGrid.column,
                    skill.GetReleaseRadius(battleUnit.mapGrid));
            }

            //根据类型判断技能显示状态
            switch (skill.targetType)
            {
                //对单个目标
                case BattleSkillTargetType.BattleUnit:
                    //可以被选中的
                    for (int i = 0; i < usedManualReleaseAnalysisor.suitableUnits.Count; ++i)
                        usedManualReleaseAnalysisor.suitableUnits[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Selectable);

                    //不可以被选中的
                    for (int i = 0; i < usedManualReleaseAnalysisor.distanceLimit.Count; ++i)
                        usedManualReleaseAnalysisor.distanceLimit[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.NotSelectable);

                    //队伍不合适的
                    for (int i = 0; i < usedManualReleaseAnalysisor.teamLimit.Count; ++i)
                        usedManualReleaseAnalysisor.teamLimit[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);

                    //设定技能操作类型
                    skillOperationType = skill.effectRadius > 0 ? SkillOperationType.SurroundBattleUnit : SkillOperationType.SingleBattleUnitTarget;
                    break;

                //对范围(某一个位置)目标
                case BattleSkillTargetType.GridUnit:
                    skillOperationType = SkillOperationType.GridUnitTarget;
                    //如果没有位置可选，则直接打开范围
                    if (skill.GetReleaseRadius(battleUnit.mapGrid) <= 0)
                    {
                        //可以被选中，标记为已被选中
                        for (int i = 0; i < usedManualReleaseAnalysisor.suitableUnits.Count; ++i)
                            usedManualReleaseAnalysisor.suitableUnits[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Selected);

                        //不可以被选中的
                        for (int i = 0; i < usedManualReleaseAnalysisor.distanceLimit.Count; ++i)
                            usedManualReleaseAnalysisor.distanceLimit[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.NotSelectable);

                        //队伍不合适的
                        for (int i = 0; i < usedManualReleaseAnalysisor.teamLimit.Count; ++i)
                            usedManualReleaseAnalysisor.teamLimit[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);

                        OnBattleUnitAndGridTouched_StateSkill_GridUnitTarget(battleUnit.mapGrid, null);
                    }
                    else
                    {
                        //需要指定范围，需要点击目标地块
                    }
                    break;

                //以自身为中心的
                case BattleSkillTargetType.Self:
                    //可以被选中，标记为已被选中
                    for (int i = 0; i < usedManualReleaseAnalysisor.suitableUnits.Count; ++i)
                        usedManualReleaseAnalysisor.suitableUnits[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Selected);

                    //不可以被选中的
                    for (int i = 0; i < usedManualReleaseAnalysisor.distanceLimit.Count; ++i)
                        usedManualReleaseAnalysisor.distanceLimit[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.NotSelectable);

                    //队伍不合适的
                    for (int i = 0; i < usedManualReleaseAnalysisor.teamLimit.Count; ++i)
                        usedManualReleaseAnalysisor.teamLimit[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);

                    skillOperationType = SkillOperationType.SurroundSelf;

                    //省去一步点击操作，直接展示区域
                    OnBattleUnitAndGridTouched_StateSkill_SurroundSelf(battleUnit.mapGrid, battleUnit);
                    break;

                default:
                    break;
            }

            //切换操作状态
            manualOperationState = ManualOperationState.Skill;
            HideManualActionList();
        }

        public void BattleUnitUseItem(BattleUnit battleUnit, SO_PackageItem item, int count)
        {
            if (battleUnit == null
                || battleUnit.battleUnitRenderer == null
                || !battleUnit.battleUnitRenderer.Equals(manualOperatingBattleUnitRenderer))
            {
                UtilityHelper.LogError("Battle unit use item failed.");
                HideManualActionList();
                return;
            }
            battleUnit.UseItem(item.itemID, count);
            ManualOperationComplete();
        }

        //点击了取消
        public void ClickedCancel()
        {
            switch (manualOperationState)
            {
                //当前还没有进行操作
                case ManualOperationState.Waiting:
                case ManualOperationState.Select:
                    return;
                case ManualOperationState.Move:
                case ManualOperationState.Skill:
                    CleanState(true, true);
                    break;
                default:
                    break;
            }
        }

        //点击了地块、战斗单位
        public void OnBattleUnitAndGridTouched(GridUnit gridTouched, BattleUnit battleUnitTouched)
        {
            //对应当前地图的操作状态
            switch (manualOperationState)
            {
                case ManualOperationState.Waiting:
                    //当前为等待中
                    UtilityHelper.Log("当前为等待中...");
                    break;
                case ManualOperationState.Select:
                    //当前为允许操作待选择
                    OnBattleUnitAndGridTouched_StateSelect(gridTouched, battleUnitTouched);
                    break;
                case ManualOperationState.Move:
                    //当前为选择移动目标
                    OnBattleUnitAndGridTouched_StateMove(gridTouched, battleUnitTouched);
                    break;
                case ManualOperationState.Skill:
                    //当前为技能使用判断
                    OnBattleUnitAndGridTouched_StateSkill(gridTouched, battleUnitTouched);
                    break;
                default:
                    break;
            }
        }

        //使用道具
        public void UseItem(BattleUnit battleUnit, SO_PackageItem item, int count)
        {
        }

        //点击了地块、战斗单位 -- 在没有任何战斗单位被选择的情况下
        private void OnBattleUnitAndGridTouched_StateSelect(GridUnit gridTouched, BattleUnit battleUnitTouched)
        {
            //点中了战斗单位
            if (battleUnitTouched != null)
            {
                //点中了等待手动操作的战斗单位则弹出操作界面
                if (battleUnitTouched.battleUnitRenderer.Equals(manualOperatingBattleUnitRenderer))
                {
                    ShowManualActionList();
                }
                else
                {
                    HideManualActionList();
                    UIViewManager.Instance.ShowView(UIViewName.BattleFieldUnitInfo, gridTouched, battleUnitTouched);
                }
            }
            //点中了地图
            else
            {
                HideManualActionList();
                UIViewManager.Instance.ShowView(UIViewName.BattleFieldUnitInfo, gridTouched, battleUnitTouched);
            }
        }

        //点击了地块、战斗单位 -- 在当前是选择移动目标的情况下
        private void OnBattleUnitAndGridTouched_StateMove(GridUnit gridTouched, BattleUnit battleUnitTouched)
        {
            //点中了战斗单位
            if (battleUnitTouched != null)
            {
                //显示战斗单位的详情
                UIViewManager.Instance.ShowView(UIViewName.BattleFieldUnitInfo, gridTouched, battleUnitTouched);
            }
            //点中了地图
            else
            {
                //障碍物不能作为移动目标(暂时)
                if (gridTouched.GridType == GridType.Obstacle)
                {
                    UIViewManager.Instance.ShowView(UIViewName.BattleFieldUnitInfo, gridTouched, battleUnitTouched);
                }
                else
                {
                    //点击是否超出了范围
                    GridUnit fromGrid = manualOperatingBattleUnitRenderer.battleUnit.mapGrid;
                    if (fromGrid.Distance(gridTouched) > manualOperatingBattleUnitRenderer.battleUnit.battleUnitAttribute.mobility)
                    {
                        UtilityHelper.Log("超出了移动半径！");
                        UIViewManager.Instance.ShowView(UIViewName.BattleFieldUnitInfo, gridTouched, battleUnitTouched);
                    }
                    else
                    {
                        //判断移动是否可以到达
                        bool result = MapNavigator.Instance.Navigate(
                            fieldRenderer.battleField.battleMap,
                            fromGrid,
                            gridTouched,
                            UtilityObjs.gridUnits,
                            null,
                            manualOperatingBattleUnitRenderer.battleUnit.battleUnitAttribute.mobility
                            );

                        //判断是否可以到达(导航成功且可以可以到达)
                        if (result && UtilityObjs.gridUnits[UtilityObjs.gridUnits.Count - 1].Equals(gridTouched))
                        {
                            //可以到达
                            ManualMoveTo(gridTouched, UtilityObjs.gridUnits.ToArray());
                            UtilityObjs.gridUnits.Clear();
                        }
                        else
                        {
                            //不可以到达
                            UtilityHelper.Log("点击位置不可到达！");
                            UIViewManager.Instance.ShowView(UIViewName.BattleFieldUnitInfo, gridTouched, battleUnitTouched);
                        }
                    }
                }
            }
        }

        //点击了地块、战斗单位 -- 在当前是选择技能释放的情况下
        private void OnBattleUnitAndGridTouched_StateSkill(GridUnit gridTouched, BattleUnit battleUnitTouched)
        {
            //技能有问题
            if (usedManualReleaseAnalysisor == null || usedManualReleaseAnalysisor.battleSkill == null)
            {
                UtilityHelper.LogError("OnBattleUnitAndGridTouched_StateSkill failed:None skill...");
                CleanState(true, true);
                return;
            }

            switch (skillOperationType)
            {
                case SkillOperationType.None:
                    UtilityHelper.LogError(string.Format("OnBattleUnitAndGridTouched_StateSkill failed:none skill type. Skill id -> {0}", usedManualReleaseAnalysisor.battleSkill.skillID));
                    CleanState(true, true);
                    return;

                //单个目标
                case SkillOperationType.SingleBattleUnitTarget:
                    OnBattleUnitAndGridTouched_StateSkill_SingleBattleUnitTarget(gridTouched, battleUnitTouched);
                    break;
                
                //单个目标，带范围覆盖
                case SkillOperationType.SurroundBattleUnit:
                    OnBattleUnitAndGridTouched_StateSkill_SurroundBattleUnit(gridTouched, battleUnitTouched);
                    break;
                
                //以自身为圆心的技能
                case SkillOperationType.SurroundSelf:
                    OnBattleUnitAndGridTouched_StateSkill_SurroundSelf(gridTouched, battleUnitTouched);
                    break;
                
                //范围技能，需要选定释放位置
                case SkillOperationType.GridUnitTarget:
                    OnBattleUnitAndGridTouched_StateSkill_GridUnitTarget(gridTouched, battleUnitTouched);
                    break;

                default:
                    break;
            }
        }

        //点击了技能，在单体目标技能情况下
        private void OnBattleUnitAndGridTouched_StateSkill_SingleBattleUnitTarget(GridUnit gridTouched, BattleUnit battleUnitTouched)
        {
            //没有点中战斗单位
            if (battleUnitTouched == null)
                return;

            //点中了可以被使用技能的单位
            if (usedManualReleaseAnalysisor.suitableUnits.Contains(battleUnitTouched))
                ManualSkill(battleUnitTouched);

            //点中了超出距离的
            else if (usedManualReleaseAnalysisor.distanceLimit.Contains(battleUnitTouched))
                UtilityHelper.Log("目标超出攻击范围");

            //同一个队伍
            else if (usedManualReleaseAnalysisor.teamLimit.Contains(battleUnitTouched))
                UtilityHelper.Log("不能对同一个队伍的单位使用这个技能");

            else
                UtilityHelper.Log("无效的目标单位");
        }

        //点击了技能，在单体带环绕的技能情况下
        private void OnBattleUnitAndGridTouched_StateSkill_SurroundBattleUnit(GridUnit gridTouched, BattleUnit battleUnitTouched)
        {
            //没有点中战斗单位
            if (battleUnitTouched == null)
                return;

            //是否是有效单位
            if (usedManualReleaseAnalysisor.suitableUnits.Contains(battleUnitTouched))
            {
                //重复点击同一个有效的战斗单位，则释放技能
                if (battleUnitTouched.battleUnitRenderer.Equals(selectedBattleUnitRenderer))
                {
                    ManualSkill(battleUnitTouched);
                    return;
                }
                else if (selectedBattleUnitRenderer != null)
                {
                    //取消选中
                    selectedBattleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);
                }
                //显示新的范围
                selectedBattleUnitRenderer = battleUnitTouched.battleUnitRenderer;
                //范围内战斗单位设置为选中
                for (int i = 0; i < usedManualReleaseAnalysisor.suitableUnits.Count; ++i)
                {
                    if (usedManualReleaseAnalysisor.suitableUnits[i].mapGrid.Distance(gridTouched) <= usedManualReleaseAnalysisor.battleSkill.effectRadius)
                        usedManualReleaseAnalysisor.suitableUnits[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Selected);
                }
                SetCircularRangeRenderStateActive(
                    true,
                    GridRenderType.SkillEffectRange,
                    gridTouched.row,
                    gridTouched.column,
                    usedManualReleaseAnalysisor.battleSkill.effectRadius);
            }
            //不是有效单位
            else if (usedManualReleaseAnalysisor.distanceLimit.Contains(battleUnitTouched))
                UtilityHelper.Log("目标超出攻击范围");

            //队伍不符合
            else if (usedManualReleaseAnalysisor.teamLimit.Contains(battleUnitTouched))
                UtilityHelper.Log("不能对同一个队伍的单位使用这个技能");

            else
                UtilityHelper.Log("目标单位无效");
        }

        //点击了技能，在以自身为原点的技能情况下
        private void OnBattleUnitAndGridTouched_StateSkill_SurroundSelf(GridUnit gridTouched, BattleUnit battleUnitTouched)
        {
            //当前选定的战斗单位为空，表示还没有显示攻击范围
            if (selectedBattleUnitRenderer == null)
            {
                selectedBattleUnitRenderer = battleUnitTouched.battleUnitRenderer;
                //展示攻击区域
                SetCircularRangeRenderStateActive(
                    true,
                    GridRenderType.SkillEffectRange,
                    selectedBattleUnitRenderer.battleUnit.mapGrid.row,
                    selectedBattleUnitRenderer.battleUnit.mapGrid.column,
                    usedManualReleaseAnalysisor.battleSkill.effectRadius);
            }
            else
            {
                //点击任意位置判断为释放
                ManualSkill(manualOperatingBattleUnitRenderer.battleUnit.mapGrid);
            }
        }

        //点击了技能，在以固定地点为原点的技能情况下
        private void OnBattleUnitAndGridTouched_StateSkill_GridUnitTarget(GridUnit gridTouched, BattleUnit battleUnitTouched)
        {
            //如果当前有点中的格子
            if (selectedGridUnitRenderer != null 
                && (selectedGridUnitRenderer.Equals(gridTouched.gridUnitRenderer) || usedManualReleaseAnalysisor.battleSkill.GetReleaseRadius(usedManualReleaseAnalysisor.releaser.mapGrid) <= 0))
            {
                //如果当前的释放距离为0，表示无需二次点击确认，直接释放
                if (usedManualReleaseAnalysisor.battleSkill.GetReleaseRadius(usedManualReleaseAnalysisor.releaser.mapGrid) <= 0)
                {
                    ManualSkill(manualOperatingBattleUnitRenderer.battleUnit.mapGrid);
                    return;
                }

                //点中了重复的格子，判断是否有目标
                bool hasTarget = false;
                for (int i = 0; i < usedManualReleaseAnalysisor.suitableUnits.Count; ++i)
                {
                    if (usedManualReleaseAnalysisor.suitableUnits[i].mapGrid.Distance(gridTouched) <= usedManualReleaseAnalysisor.battleSkill.effectRadius)
                    {
                        hasTarget = true;
                        break;
                    }
                }
                //有目标，则释放
                if (hasTarget)
                {
                    ManualSkill(gridTouched);
                }
                else
                {
                    UtilityHelper.Log("范围内没有目标");
                }
            }
            else
            {
                //这个格子不在范围内
                if (manualOperatingBattleUnitRenderer.battleUnit.mapGrid.Distance(gridTouched) <= usedManualReleaseAnalysisor.battleSkill.GetReleaseRadius(usedManualReleaseAnalysisor.releaser.mapGrid))
                {
                    if (selectedGridUnitRenderer != null)
                    {
                        //取消上一个范围显示
                        SetCircularRangeRenderStateActive(false, GridRenderType.SkillEffectRange);
                        //取消可被攻击单位的显示
                        for (int i = 0; i < usedManualReleaseAnalysisor.suitableUnits.Count; ++i)
                            usedManualReleaseAnalysisor.suitableUnits[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);
                    }
                    //设置新的范围显示
                    selectedGridUnitRenderer = gridTouched.gridUnitRenderer;
                    SetCircularRangeRenderStateActive(
                        true,
                        GridRenderType.SkillEffectRange,
                        gridTouched.row,
                        gridTouched.column,
                        usedManualReleaseAnalysisor.battleSkill.effectRadius
                        );
                    //设置新的可被攻击单位的显示
                    for (int i = 0; i < usedManualReleaseAnalysisor.suitableUnits.Count; ++i)
                    {
                        if(usedManualReleaseAnalysisor.suitableUnits[i].mapGrid.Distance(gridTouched) <= usedManualReleaseAnalysisor.battleSkill.effectRadius)
                            usedManualReleaseAnalysisor.suitableUnits[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Selected);
                    }
                }
                else
                {
                    UtilityHelper.Log("超出区域");
                    UIViewManager.Instance.ShowView(UIViewName.BattleFieldUnitInfo, gridTouched, battleUnitTouched);
                }
            }
        }

        //显示手动操作面板
        private void ShowManualActionList()
        {
            if (manualOperatingBattleUnitRenderer == null)
                return;

            BattleUnit battleUnit = manualOperatingBattleUnitRenderer.battleUnit;
            if (battleUnit == null)
            {
                UtilityHelper.LogError("Show manual asction list error : Battle unit is none.");
                return;
            }

            //弹出面板
            UIViewManager.Instance.ShowView(UIViewName.BattleFieldPlayerActOption, battleUnit);
        }

        private void HideManualActionList()
        {
            UIViewManager.Instance.HideView(UIViewName.BattleFieldPlayerActOption);
        }

        //手动选择移动目标
        private void ManualMoveTo(GridUnit grid, GridUnit[] path)
        {
            //为行动添加移动数据
            manualOperatingBattleUnitRenderer.battleUnit.MoveToTargetGrid(null, grid, path);
            fieldRenderer.PlayBattle(AfterManualMove);
        }

        //移动后的回调
        private void AfterManualMove()
        {
            //清空高亮显示
            SetCircularRangeRenderStateActive(false, GridRenderType.MoveRange);
            //切换状态
            manualOperationState = ManualOperationState.Select;
            //通知移动完成
            manualOperatingBattleUnitRenderer.battleUnit.CompleteManualState(ManualActionState.Move);
        }

        //手动释放技能(对具体的目标)
        private void ManualSkill(BattleUnit targetBattleUnit)
        {
            if (targetBattleUnit == null)
                return;
            
            //为行动添加技能释放数据
            manualOperatingBattleUnitRenderer.battleUnit.UseSkill(usedManualReleaseAnalysisor.battleSkill, targetBattleUnit);
            
            //行动结束
            ManualOperationComplete();
        }

        //手动释放技能(对具体的地块)
        private void ManualSkill(GridUnit targetGrid)
        {
            if (targetGrid == null) return;

            //添加一个行动
            manualOperatingBattleUnitRenderer.battleUnit.UseSkill(usedManualReleaseAnalysisor.battleSkill, null, targetGrid);

            //取消范围高亮显示
            SetCircularRangeRenderStateActive(false, GridRenderType.SkillReleaseRange);
            SetCircularRangeRenderStateActive(false, GridRenderType.SkillEffectRange);

            //行动结束
            ManualOperationComplete();
        }

        //手动操作完成
        private void ManualOperationComplete()
        {
            //切换状态
            manualOperationState = ManualOperationState.Waiting;
            skillOperationType = SkillOperationType.None;
            
            //清空状态
            CleanState(false, false);

            //清空手动操作单位
            if (manualOperatingBattleUnitRenderer != null)
            {
                manualOperatingBattleUnitRenderer.battleUnit.CompleteManualState(ManualActionState.SkillOrItem);
                manualOperatingBattleUnitRenderer.battleUnit.CompleteManualState(ManualActionState.Move);
                manualOperatingBattleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);
                manualOperatingBattleUnitRenderer = null;
            }

            //关闭操作菜单
            HideManualActionList();

            //使用了手动分析器
            if (usedManualReleaseAnalysisor != null)
                usedManualReleaseAnalysisor.Reset();

            //显示后通知手动操作完成
            EventManager.Instance.Run(EGameConstL.EVENT_MANUAL_OPERATION_COMPLETE, null);
        }

        //设置某个圆形区域的显示状态(每种类型只能显示一个)
        public void SetCircularRangeRenderStateActive(bool active, GridRenderType gridRenderType, int centerRow = -1, int centerColumn = -1, int radius = -1)
        {
            //确定是哪种范围
            List<GridUnit> rangeHighlightGridUnits = null;
            switch (gridRenderType)
            {
                case GridRenderType.MoveRange:
                    rangeHighlightGridUnits = moveRangeGridUnits;
                    break;
                case GridRenderType.SkillReleaseRange:
                    rangeHighlightGridUnits = skillReleaseRangeGridUnits;
                    break;
                case GridRenderType.SkillEffectRange:
                    rangeHighlightGridUnits = skillEffectRangeGridUnits;
                    break;
                default:
                    UtilityHelper.LogError(string.Format("SetRangeHighlightActive error grid render type : {0}", gridRenderType));
                    return;
            }

            //当前是取消激活
            if (!active)
            {
                for (int i = 0; i < rangeHighlightGridUnits.Count; ++i)
                {
                    if (rangeHighlightGridUnits[i].gridUnitRenderer != null)
                    {
                        rangeHighlightGridUnits[i].gridUnitRenderer.RemoveGridRenderType(gridRenderType);
                    }
                }
                rangeHighlightGridUnits.Clear();
            }
            else
            {
                //当前存在上一个激活，先隐藏
                if (rangeHighlightGridUnits.Count > 0)
                    SetCircularRangeRenderStateActive(false, gridRenderType);
                //获取格子
                fieldRenderer.battleField.battleMap.GetCircularGrids(centerRow, centerColumn, radius, 0, true, rangeHighlightGridUnits);
                //设置高亮状态
                for (int i = 0; i < rangeHighlightGridUnits.Count; ++i)
                {
                    if (rangeHighlightGridUnits[i].gridUnitRenderer != null)
                    {
                        rangeHighlightGridUnits[i].gridUnitRenderer.AppendGridRenderType(gridRenderType);
                    }
                }
            }
        }

        //设置路径显示状态
        public void SetGridsRenderStateActive(bool active, GridUnit[] gridPath = null)
        {
            if (!active)
            {
                for (int i = 0; i < pathHighlightGridUnits.Count; ++i)
                {
                    if (pathHighlightGridUnits[i].gridUnitRenderer != null)
                    {
                        pathHighlightGridUnits[i].gridUnitRenderer.RemoveGridRenderType(GridRenderType.Path);
                    }
                }
                pathHighlightGridUnits.Clear();
            }
            else
            {
                //当前存在上一个激活，先隐藏
                if (pathHighlightGridUnits.Count > 0)
                    SetGridsRenderStateActive(false, null);
                
                else if (gridPath == null)
                {
                    UtilityHelper.LogError("Set path highlight active true warning: gridPath is none.");
                    return;
                }
                pathHighlightGridUnits.AddRange(gridPath);
                //设置高亮状态
                for (int i = 0; i < pathHighlightGridUnits.Count; ++i)
                {
                    if (pathHighlightGridUnits[i].gridUnitRenderer != null)
                    {
                        pathHighlightGridUnits[i].gridUnitRenderer.AppendGridRenderType(GridRenderType.Path);
                    }
                }
            }
        }

        /// <summary>
        /// 清空状态
        /// </summary>
        /// <param name="resetOperationState">是否将操作状态设置为select(等待操作状态)</param>
        /// <param name="resetManualBattleUnitState">是否重置带操作战斗单位(行动中状态)</param>
        private void CleanState(bool resetOperationState, bool resetManualBattleUnitState)
        {
            //清除区域显示效果
            SetCircularRangeRenderStateActive(false, GridRenderType.MoveRange);
            SetCircularRangeRenderStateActive(false, GridRenderType.SkillReleaseRange);
            SetCircularRangeRenderStateActive(false, GridRenderType.SkillEffectRange);
            
            //还原所有战斗单位的状态
            if (usedManualReleaseAnalysisor != null)
            {
                //可以被选中的
                for (int i = 0; i < usedManualReleaseAnalysisor.suitableUnits.Count; ++i)
                    usedManualReleaseAnalysisor.suitableUnits[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);

                //不可以被选中的
                for (int i = 0; i < usedManualReleaseAnalysisor.distanceLimit.Count; ++i)
                    usedManualReleaseAnalysisor.distanceLimit[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);

                //队伍不合适的
                for (int i = 0; i < usedManualReleaseAnalysisor.teamLimit.Count; ++i)
                    usedManualReleaseAnalysisor.teamLimit[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);

                //无效的
                for (int i = 0; i < usedManualReleaseAnalysisor.battleUnitInvalid.Count; ++i)
                    usedManualReleaseAnalysisor.battleUnitInvalid[i].battleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Normal);
            }

            //是否重置操作状态
            if (resetOperationState)
                manualOperationState = ManualOperationState.Select;

            //是否重置手动操作的英雄
            if (resetManualBattleUnitState && manualOperatingBattleUnitRenderer != null)
            {
                manualOperatingBattleUnitRenderer.UpdateRenderState(BattleUnitRenderState.Action);
            }

            //重置已选中项
            selectedBattleUnitRenderer = null;
            selectedGridUnitRenderer = null;
        }
    }
}