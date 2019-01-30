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
        [SerializeField] private Button btnStart;
        [SerializeField] private TextMeshProUGUI tmpMainNotice;
        [SerializeField] private GameObject objNoticeBG;

        protected override void InitUIObjects()
        {
            base.InitUIObjects();

            btnStart.onClick.AddListener(ClickStart);
            btnStart.gameObject.SetActive(false);
            objNoticeBG.gameObject.SetActive(false);
        }

        public void BattleFieldReady()
        {
            btnStart.gameObject.SetActive(true);
        }
        
        private void ClickStart()
        {
            BattleManager.Instance.RunManualTest();
            btnStart.gameObject.SetActive(false);
        }

        public void ShowBattleEnd()
        {
            UIViewManager.Instance.HideViews(UIViewLayer.Popup);
            objNoticeBG.gameObject.SetActive(true);
            tmpMainNotice.text = "战斗结束";
        }
    }
}