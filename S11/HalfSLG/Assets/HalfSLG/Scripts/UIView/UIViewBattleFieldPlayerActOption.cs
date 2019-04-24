using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ELGame
{
    public class UIViewBattleFieldPlayerActOption
        : UIViewBase
    {
        //控制显示位置
        [SerializeField] private RectTransform rtOptionLayout;   //操作按钮组

        [SerializeField] private CanvasGroup cgOptionLayout;    //操作子面板的group

        //按钮
        [SerializeField] private Button btnMove;    //移动
        [SerializeField] private Button btnAttack;  //攻击
        [SerializeField] private Button btnStay;    //待命
        [SerializeField] private Button btnItem;    //使用道具  
        [SerializeField] private Button btnOptionLayoutTrigger; //操作按钮组的触发器

        [Header("Skill"), Space]
        [SerializeField] private RectTransform rtSkillLayout;    //技能按钮组
        [SerializeField] private List<Button> skillBtns;

        [Header("Item"), Space]
        [SerializeField] private RectTransform rtItemLayout;    //道具按钮组
        [SerializeField] private List<Button> itemBtns;

        private BattleUnit battleUnit;

        protected override void UpdateArguments(params object[] args)
        {
            if (args.Length >= 0)
                battleUnit = args[0] as BattleUnit;
        }

        protected override void InitUIObjects()
        {
            base.InitUIObjects();
            
            //设置按钮文字
            SetObjectText(btnMove.gameObject, "移动");
            SetObjectText(btnAttack.gameObject, "攻击");
            SetObjectText(btnStay.gameObject, "待命");
            SetObjectText(btnItem.gameObject, "道具");

            //点击回调
            btnMove.onClick.AddListener(OnClickedMove);
            btnAttack.onClick.AddListener(OnClickedAttack);
            btnStay.onClick.AddListener(OnClickedStay);
            btnItem.onClick.AddListener(OnClickedUseItem);
            btnOptionLayoutTrigger.onClick.AddListener(OnClickedOptionLayoutTrigger);

            //获取技能按钮
            rtSkillLayout.GetComponentsInChildren<Button>(true, skillBtns);
            //动态获取，保证顺序
            if (skillBtns == null || skillBtns.Count == 0)
            {
                UtilityHelper.LogError("Init BattleFieldPlayerActOption failed.Not found skill btn item.");
                return;
            }
            //绑定技能按钮回调
            for (int i = 0; i < skillBtns.Count; ++i)
            {
                skillBtns[i].name = string.Format("{0}{1}", EGameConstL.STR_SkillBtn, i);
                skillBtns[i].onClick.AddListener(OnClickedSkillItem);
            }
            //绑定道具按钮回调
            for (int i = 0; i < itemBtns.Count; ++i)
            {
                itemBtns[i].name = string.Format("{0}{1}", EGameConstL.STR_ItemBtn, i);
                itemBtns[i].onClick.AddListener(OnClickedItem);
            }
        }

        public override void OnShow()
        {
            base.OnShow();

            if (battleUnit == null)
            {
                UtilityHelper.LogError("Show view error: UIViewBattleFieldPlayerActOption");
                Close();
                return;
            }
            //设置位置
            var anchoredPosition = UIViewManager.Instance.ConvertWorldPositionToRootCanvasPosition(battleUnit.mapGrid.localPosition);
            var relativePos = UIViewManager.Instance.GetRelativePosition(anchoredPosition);
            rtOptionLayout.ResetPivot(relativePos, 0f, 0f);
            rtOptionLayout.anchoredPosition = anchoredPosition;

            //设置按钮
            btnMove.gameObject.SetActive(battleUnit.CheckManualState(ManualActionState.Move));

            //初始时隐藏技能节点
            HideSkillNode();
            HideItemNode();

            //设置道具按钮的状态
            UpdateItemBtnState();
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        public override void OnExit()
        {
            base.OnExit();
            battleUnit = null;
        }
        
        //点击了移动
        private void OnClickedMove()
        {
            if (battleUnit == null)
            {
                Close();
                return;
            }

            if (BattleFieldRenderer.Instance)
                BattleFieldRenderer.Instance.BattleUnitMove(battleUnit);
        }

        //点击了攻击
        private void OnClickedAttack()
        {
            if (battleUnit == null)
            {
                Close();
                return;
            }

            ShowSkillPanel();
        }

        //点击了道具
        private void OnClickedUseItem()
        {
            if (battleUnit == null)
            {
                Close();
                return;
            }

            ShowItemPanel();
        }

        //点击了待命
        private void OnClickedStay()
        {
            if (battleUnit == null)
            {
                Close();
                return;
            }

            if (BattleFieldRenderer.Instance)
                BattleFieldRenderer.Instance.BattleUnitStay(battleUnit);
        }

        //点击了按钮组的触发器
        private void OnClickedOptionLayoutTrigger()
        {
            HideSkillNode();
            HideItemNode();
        }

        //点击了技能按钮
        private void OnClickedSkillItem()
        {
            //获取当前点击对象
            string btnName = EventSystem.current.currentSelectedGameObject.name;
            int skillIdx = -1;
            if (int.TryParse(btnName.Replace(EGameConstL.STR_SkillBtn, string.Empty), out skillIdx))
            {
                SO_BattleSkill skill = battleUnit.battleUnitAttribute.battleSkills[skillIdx];
                if (skill != null)
                {
                    if (battleUnit.battleUnitAttribute.energy >= skill.energyCost && BattleFieldRenderer.Instance != null)
                    {
                        BattleFieldRenderer.Instance.BattleUnitUseSkill(battleUnit, skill);
                    }
                    else
                    {
                        UtilityHelper.LogWarning(string.Format("能量不足:{0}/{1}", battleUnit.battleUnitAttribute.energy, skill.energyCost));
                    }
                }
                else
                    UtilityHelper.LogError("Skill item error ->" + btnName);
            }
            else
            {
                UtilityHelper.LogError("Skill item name error ->" + btnName);
            }
        }

        private void OnClickedItem()
        {
            //获取当前点击对象
            string btnName = EventSystem.current.currentSelectedGameObject.name;
            int itemIdx = -1;
            if (int.TryParse(btnName.Replace(EGameConstL.STR_ItemBtn, string.Empty), out itemIdx))
            {
                PackageItem item = battleUnit.package.GetItemByIdx(itemIdx);
                if (item != null && item.count > 0)
                {
                    if (BattleFieldRenderer.Instance != null)
                        BattleFieldRenderer.Instance.BattleUnitUseItem(battleUnit, item.item, 1);
                }
            }
        }

        //显示技能节点
        private void ShowSkillPanel()
        {
            cgOptionLayout.alpha = 0.5f;
            //设置节点位置
            var anchoredPosition = rtOptionLayout.anchoredPosition;
            var relativePos = UIViewManager.Instance.GetRelativePosition(anchoredPosition);
            rtSkillLayout.ResetPivot(relativePos, 0f, 0f);
            rtSkillLayout.anchoredPosition = anchoredPosition + new Vector2(relativePos.x <= 0f ? 165f : -165f, 0);
            rtSkillLayout.gameObject.SetActive(true);

            //操作节点遮挡开启，用于关闭道具按钮节点
            btnOptionLayoutTrigger.gameObject.SetActive(true);

            //获取技能
            for (int i = 0; i < battleUnit.battleUnitAttribute.battleSkills.Length; ++i)
            {
                if (i >= skillBtns.Count && skillBtns.Count > 0)
                {
                    //创建新按钮
                    Button btn = Instantiate<Button>(skillBtns[0], rtSkillLayout);
                    //设置新的按钮
                    btn.name = string.Format("{0}{1}", EGameConstL.STR_SkillBtn, i);
                    btn.onClick.AddListener(OnClickedSkillItem);
                    skillBtns.Add(btn);
                }
                //设置技能名字
                var label = skillBtns[i].transform.Find("Label").GetComponent<TextMeshProUGUI>();
                int battleUnitEnergy = battleUnit.battleUnitAttribute.energy;
                int energyCost = battleUnit.battleUnitAttribute.battleSkills[i].energyCost;
                label.text = string.Format("{0}({1}/{2})", battleUnit.battleUnitAttribute.battleSkills[i].skillName, battleUnitEnergy, energyCost);
                //判断能量是否足够
                label.color = battleUnitEnergy >= energyCost ? EGameConstL.Color_labelWhite : EGameConstL.Color_labelRed;
            }

            //设置按钮状态
            for (int i = 0; i < skillBtns.Count; ++i)
            {
                skillBtns[i].gameObject.SetActive(i < battleUnit.battleUnitAttribute.battleSkills.Length);
            }
        }

        private void UpdateItemBtnState()
        {
            if (battleUnit.package == null || battleUnit.package.itemCount == 0)
            {
                btnItem.GetComponent<Image>().color = EGameConstL.Color_Translucence;
                btnItem.interactable = false;
            }
            else
            {
                btnItem.GetComponent<Image>().color = Color.white;
                btnItem.interactable = true;
            }
        }

        //显示道具
        private void ShowItemPanel()
        {
            cgOptionLayout.alpha = 0.5f;
            //设置节点位置
            var anchoredPosition = rtOptionLayout.anchoredPosition;
            var relativePos = UIViewManager.Instance.GetRelativePosition(anchoredPosition);
            rtItemLayout.ResetPivot(relativePos, 0f, 0f);
            rtItemLayout.anchoredPosition = anchoredPosition + new Vector2(relativePos.x <= 0f ? 165f : -165f, 0);
            rtItemLayout.gameObject.SetActive(true);

            //操作节点遮挡开启，用于关闭技能按钮节点
            btnOptionLayoutTrigger.gameObject.SetActive(true);

            //获取技能
            int validCount = 0;
            for (int i = 0; i < battleUnit.package.itemCount; ++i)
            {
                var item = battleUnit.package.GetItemByIdx(i);
                //只考虑有数量的
                if (item != null && item.count > 0)
                    ++validCount;
                else
                    continue;

                //需要新增
                if (i >= itemBtns.Count && itemBtns.Count > 0)
                {
                    //创建新按钮
                    Button btn = Instantiate<Button>(itemBtns[0], rtItemLayout);
                    //设置新的按钮
                    btn.name = string.Format("{0}{1}", EGameConstL.STR_ItemBtn, i);
                    btn.onClick.AddListener(OnClickedItem);
                    itemBtns.Add(btn);
                }
                //设置道具图标和数量
                var label = itemBtns[i].transform.Find("ItemIcon/ItemCount").GetComponent<TextMeshProUGUI>();
                label.text = string.Format("x{0}", item.count);
                var icon = itemBtns[i].transform.Find("ItemIcon").GetComponent<Image>();
                icon.sprite = item.item.icon;
            }

            //设置按钮状态
            for (int i = 0; i < itemBtns.Count; ++i)
                itemBtns[i].gameObject.SetActive(i < validCount);
        }

        //隐藏技能节点
        private void HideSkillNode()
        {
            cgOptionLayout.alpha = 1f;
            rtSkillLayout.gameObject.SetActive(false);
            btnOptionLayoutTrigger.gameObject.SetActive(false);
        }

        private void HideItemNode()
        {
            cgOptionLayout.alpha = 1f;
            rtItemLayout.gameObject.SetActive(false);
            btnOptionLayoutTrigger.gameObject.SetActive(false);
        }
    }
}