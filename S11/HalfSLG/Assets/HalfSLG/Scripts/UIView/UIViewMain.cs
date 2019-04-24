using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ELGame
{
    public class UIViewMain 
        : UIViewBase
    {
        [SerializeField] private Button btnAutoStart;
        [SerializeField] private Button btnManualStart;
        [SerializeField] private Button btnReset;
        [SerializeField] private TextMeshProUGUI tmpMainNotice;
        [SerializeField] private GameObject objNoticeBG;

        protected override void InitUIObjects()
        {
            base.InitUIObjects();

            btnAutoStart.onClick.AddListener(ClickAutoStart);
            btnManualStart.onClick.AddListener(ClickManualStart);
            btnReset.onClick.AddListener(ClickReset);
            btnAutoStart.gameObject.SetActive(false);
            objNoticeBG.gameObject.SetActive(false);
            btnReset.gameObject.SetActive(false);

        }
        
        public void BattleFieldReady(IGameEvent msg)
        {
            btnAutoStart.gameObject.SetActive(false);
        }
        
        private void ClickAutoStart()
        {
            BattleManager.Instance.RunAutoTest();
            btnAutoStart.gameObject.SetActive(false);
            btnManualStart.gameObject.SetActive(false);
            btnReset.gameObject.SetActive(false);
        }

        private void ClickManualStart()
        {
            BattleManager.Instance.RunManualTest();
            btnAutoStart.gameObject.SetActive(false);
            btnManualStart.gameObject.SetActive(false);
            btnReset.gameObject.SetActive(true);
        }

        private void ClickReset()
        {
            BattleFieldRenderer.Instance.ResetBattleField();
        }

        public void ShowBattleEnd()
        {
            UIViewManager.Instance.HideViews(UIViewLayer.Popup);
            objNoticeBG.gameObject.SetActive(true);
            tmpMainNotice.text = "战斗结束";
        }
        
        public override void OnPush()
        {
            base.OnPush();

            RegisterEventListener(EGameConstL.EVENT_BATTLE_FIELD_RENDERER_READY, BattleFieldReady, 1);
        }

        public override void OnPopup()
        {
            base.OnPopup();

            RemoveAllEventListeners();
        }
    }
}