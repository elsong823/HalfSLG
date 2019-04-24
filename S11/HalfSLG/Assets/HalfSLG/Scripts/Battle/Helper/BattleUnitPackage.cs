//战斗单位的背包

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class PackageItem
    {
        public SO_PackageItem item;
        public int count;

        private PackageItem() { }

        public static PackageItem CreateInstance()
        {
            return new PackageItem();
        }

        public void Reset()
        {
            item = null;
            count = 0;
        }
    }

    public class BattleUnitPackage 
    {
        private BattleUnit owner;
        private int capacity = 1;
        private List<PackageItem> items;

        private BattleUnitPackage() { }

        public static BattleUnitPackage CreateInstance(BattleUnit owner, int capaticy)
        {
            if (owner == null)
                return null;

            BattleUnitPackage package = new BattleUnitPackage();
            package.owner = owner;
            package.capacity = capaticy;
            
            return package;
        }
        
        public BattleUnit Owner
        {
            get
            {
                return owner;
            }
        }

        public int Capacity { get { return capacity; } }

        public int itemCount
        {
            get
            {
                return items == null ? 0 : items.Count;
            }
        }

        public PackageItem GetItemByIdx(int idx)
        {
            if (items == null || items.Count == 0)
                return null;

            if (idx < 0 || idx >= items.Count)
                return null;

            return items[idx];
        }
        
        /// <summary>
        /// 尝试添加道具
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="addition"></param>
        /// <returns>是否成功</returns>
        public bool TryAddItem(int itemID, int addition, ref int finalCount)
        {
            if (items == null)
                items = new List<PackageItem>();

            BattleUnitPickupItemAction action = null;
            PackageItem emptyItem = null;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].item != null && items[i].item.itemID == itemID)
                {
                    items[i].count += addition;
                    finalCount = items[i].count;
                    return true;
                }
                else if (items[i].item == null)
                    emptyItem = items[i];
            }

            //找到了一个空位
            if (emptyItem != null)
            {
                emptyItem.item = PackageItemManager.Instance.GetItem(itemID);
                if (emptyItem.item != null)
                {
                    emptyItem.count += addition;
                    finalCount = emptyItem.count;
                    return true;
                }
            }

            //背包已满
            if (items.Count >= capacity)
            {
                UtilityHelper.LogWarning(string.Format("Add item failed. Package if full : {0} -> {1}/{2}", itemID, items.Count, capacity));
                return false;
            }

            //添加一个道具
            emptyItem = PackageItem.CreateInstance();
            emptyItem.item = PackageItemManager.Instance.GetItem(itemID);
            if (emptyItem.item != null)
                items.Add(emptyItem);

            emptyItem.count += addition;
            finalCount = emptyItem.count;
            return true;
        }
        
        /// <summary>
        /// 尝试使用道具
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="useCount"></param>
        /// <returns>使用数量</returns>
        public int TryUseItem(int itemID, int useCount, ref int finalCount)
        {
            if (items == null || items.Count == 0)
            {
                UtilityHelper.LogError(string.Format("Use item failed. Do not have this item -> {0}", itemID));
                return 0;
            }
            for (int i = items.Count - 1; i >= 0; --i)
            {
                if (items[i].item.itemID == itemID)
                {
                    //使用道具
                    if (items[i].count < useCount)
                    {
                        UtilityHelper.LogWarning(string.Format("Use item warning. {0}/{1}", useCount, items[i].count));
                        useCount = items[i].count;
                    }

                    //使用道具
                    items[i].count -= useCount;

                    //判断是否还有剩余
                    if (items[i].count <= 0)
                        items[i].Reset();

                    finalCount = items[i].count;

                    return useCount;
                }
            }
            UtilityHelper.LogError(string.Format("Use item failed. Do not have this item -> {0}", itemID));
            return 0;
        }
        
        //清空背包
        public void Clear()
        {
            if (items != null)
            {
                items.Clear();
                items = null;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is BattleUnitPackage)
            {
                var package = obj as BattleUnitPackage;
                //主人相同则为相同
                if (package.owner != null && owner != null)
                    return package.owner.Equals(owner);

                return false;
            }
            return false;
        }
    }
}