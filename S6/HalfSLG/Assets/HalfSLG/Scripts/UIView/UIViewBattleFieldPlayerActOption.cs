using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ELGame
{
    public class UIViewBattleFieldPlayerActOption
        : UIViewBase
    {
        //控制显示位置
        [SerializeField] RectTransform rtBase;
        //按钮
        [SerializeField] Button btnMove;    //移动
        [SerializeField] Button btnAttack;  //攻击
        [SerializeField] Button btnStay;    //待命

        private BattleUnit battleUnit;
        private UnityAction moveCallback;
        private UnityAction attackCallback;
        private UnityAction stayCallback;

        protected override void UpdateArguments(params object[] args)
        {
            if (args.Length >= 3)
            {
                battleUnit = args[0] as BattleUnit;
                moveCallback = args[1] as UnityAction;
                attackCallback = args[2] as UnityAction;
                stayCallback = args[3] as UnityAction;
            }
        }

        protected override void InitUIObjects()
        {
            base.InitUIObjects();

            if (rtBase == null 
                || btnMove == null
                || btnAttack == null
                || btnStay == null)
            {
                UtilityHelper.LogError("Init BattleFieldPlayerActOption failed.");
                return;
            }

            //设置按钮文字
            SetObjectText(btnMove.gameObject, "移动");
            SetObjectText(btnAttack.gameObject, "攻击");
            SetObjectText(btnStay.gameObject, "待命");
        }

        private void RemoveAllListener()
        {
            btnMove.onClick.RemoveAllListeners();
            btnAttack.onClick.RemoveAllListeners();
            btnStay.onClick.RemoveAllListeners();
            moveCallback = null;
            attackCallback = null;
            stayCallback = null;
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
            rtBase.anchoredPosition = UIViewManager.Instance.ConvertWorldPositionToRootCanvasPosition(battleUnit.mapGrid.localPosition);

            //设置按钮
            btnMove.gameObject.SetActive(battleUnit.CheckManualState(ManualActionState.Move));

            //绑定回调
            btnMove.onClick.AddListener(moveCallback);
            btnAttack.onClick.AddListener(attackCallback);
            btnStay.onClick.AddListener(stayCallback);
        }

        public override void OnHide()
        {
            base.OnHide();
            RemoveAllListener();
        }

        public override void OnExit()
        {
            base.OnExit();
            battleUnit = null;
        }
    }
}