using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ELGame
{
    public class ViewElementPackageItem 
        : UIViewElement
    {
        public Image imgIcon;
        public Image imgIconBg;
        public TextMeshProUGUI textCount;
        public Button btnIcon;

        private System.Action<PackageItem> onItemIconClicked;
        private PackageItem packageItem;

        private bool inited = false;

        public void Init()
        {
            if (inited)
                return;

            btnIcon.onClick.AddListener(OnIconClicked);

            inited = true;
        }

        public void Reset()
        {
            SetData(null, null);
            UpdateElement();
        }

        public void SetData(PackageItem packageItem, System.Action<PackageItem> onItemIconClicked)
        {
            Init();
            this.onItemIconClicked = onItemIconClicked;
            this.packageItem = packageItem;
            UpdateElement();

            bool trigger = onItemIconClicked != null && this.packageItem != null;
            btnIcon.interactable = trigger;
            imgIconBg.raycastTarget = trigger;
        }

        private void SetItemActive(bool active)
        {
            if (active)
            {
                textCount.enabled = true;
                imgIcon.color = Color.white;
            }
            else
            {
                textCount.enabled = false;
                imgIcon.overrideSprite = null;
                imgIcon.color = EGameConstL.Color_Transparent;
            }
        }

        protected override void UpdateElement()
        {
            if (packageItem == null || packageItem.count <= 0)
            {
                packageItem = null;
                SetItemActive(false);
                return;
            }

            //设置图标
            textCount.text = string.Format("x{0}", packageItem.count);
            imgIcon.overrideSprite = packageItem.item.icon;

            SetItemActive(true);
        }

        private void OnIconClicked()
        {
            if (packageItem != null && onItemIconClicked != null)
                onItemIconClicked(packageItem);
        }
    }
}