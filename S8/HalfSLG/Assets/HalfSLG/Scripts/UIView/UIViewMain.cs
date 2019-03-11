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
        [SerializeField] private TextMeshProUGUI tmpMainNotice;
        [SerializeField] private GameObject objNoticeBG;

        protected override void InitUIObjects()
        {
            base.InitUIObjects();

            btnAutoStart.onClick.AddListener(ClickAutoStart);
            btnManualStart.onClick.AddListener(ClickManualStart);
            btnAutoStart.gameObject.SetActive(false);
            objNoticeBG.gameObject.SetActive(false);
        }

        public void BattleFieldReady()
        {
            btnAutoStart.gameObject.SetActive(true);
        }
        
        private void ClickAutoStart()
        {
            BattleManager.Instance.RunAutoTest();
            btnAutoStart.gameObject.SetActive(false);
            btnManualStart.gameObject.SetActive(false);
        }

        private void ClickManualStart()
        {
            BattleManager.Instance.RunManualTest();
            btnAutoStart.gameObject.SetActive(false);
            btnManualStart.gameObject.SetActive(false);
        }

        public void ShowBattleEnd()
        {
            UIViewManager.Instance.HideViews(UIViewLayer.Popup);
            objNoticeBG.gameObject.SetActive(true);
            tmpMainNotice.text = "战斗结束";
        }
    }
}