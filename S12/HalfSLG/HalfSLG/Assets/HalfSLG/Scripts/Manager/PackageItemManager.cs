using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class PackageItemManager
        : BaseManager<PackageItemManager>
    {
        public override string MgrName => "PackageItemManager";

        private Dictionary<int, SO_PackageItem> dicPackageItems = new Dictionary<int, SO_PackageItem>();

        public override void InitManager()
        {
            base.InitManager();
            InitItems();
        }

        //初始化道具
        private void InitItems()
        {
            Object[] items = GetAssetsFromBundle("scriptableobjects/packageitem.unity3d", typeof(SO_PackageItem));
            if (items != null)
            {
                for (int i = 0; i < items.Length; ++i)
                {
                    SO_PackageItem item = items[i] as SO_PackageItem;
                    if (item == null)
                        continue;

                    dicPackageItems.Add(item.itemID, item);
                }
            }
        }

        //获取技能
        public SO_PackageItem GetItem(int itemID)
        {
            if (!dicPackageItems.ContainsKey(itemID))
            {
                UtilityHelper.LogError(string.Format("Get item by id failed -> {0}", itemID));
                return null;
            }
            return dicPackageItems[itemID];
        }
    }
}