using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ELGame
{
    public class ViewElementPackage
        : UIViewElement
    {
        [SerializeField] private List<ViewElementPackageItem> packageItems;
        [SerializeField] private Transform container;
        private bool activeTrigger;
        private BattleUnitPackage battleUnitPackage;

        public BattleUnitPackage BattleUnitPackage
        {
            get
            {
                return battleUnitPackage;
            }
        }

        private ViewElementPackageItem CreatePackageItem()
        {
            if (packageItems == null || packageItems.Count == 0)
                return null;

            var instance = Instantiate<ViewElementPackageItem>(packageItems[0]);
            instance.Reset();
            instance.transform.SetParent(container);
            instance.transform.Normalize();
            packageItems.Add(instance);

            return instance;
        }

        protected override void UpdateElement()
        {
            container.gameObject.SetActive(battleUnitPackage != null);

            //没有设置
            if (battleUnitPackage == null)
            {
                for (int i = 0; i < packageItems.Count; i++)
                {
                    packageItems[i].SetData(null, null);
                }
                return;
            }

            int count = battleUnitPackage.Capacity > packageItems.Count ? battleUnitPackage.Capacity : packageItems.Count;
            ViewElementPackageItem packageItem = null;
            for (int i = 0; i < count; i++)
            {
                if (i >= packageItems.Count)
                    packageItem = CreatePackageItem();
                else
                    packageItem = packageItems[i];

                if(activeTrigger)
                    packageItem.SetData(battleUnitPackage.GetItemByIdx(i), OnItemClicked);
                else
                    packageItem.SetData(battleUnitPackage.GetItemByIdx(i), null);
            }
        }
        
        public void UpdateBattleUnitPackage(BattleUnitPackage battleUnitPackage, bool activeTrigger)
        {
            if (packageItems == null || packageItems.Count == 0)
                return;

            this.battleUnitPackage = battleUnitPackage;

            this.activeTrigger = activeTrigger;

            UpdateElement();
        }

        private void OnItemClicked(PackageItem item)
        {
            if (item == null)
                return;

            if (battleUnitPackage != null 
                && battleUnitPackage.Owner != null
                && BattleUnitPackage.Owner.CheckManualState(ManualActionState.SkillOrItem))
            {
                Debug.Log(string.Format("{0}使用了道具{1}", battleUnitPackage.Owner.battleUnitAttribute.name, item.item.itemName));
                battleUnitPackage.Owner.UseItem(item.item.itemID, 1);
            }
        }
    }
}