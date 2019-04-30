//战斗单位的点数记录器
//带有计算功能
//会将各个item的point按照max进行重算

using System.Collections.Generic;
using UnityEngine;

namespace ELGame.BattleBehaviourSystem
{
    public class BattleUnitRecordItem
    {
        public BattleUnit maker;
        public float value;
        public float point;

        private BattleUnitRecordItem() { }

        public static BattleUnitRecordItem CreateInstance(BattleUnit battleUnit)
        {
            if (battleUnit == null)
                return null;

            BattleUnitRecordItem item = new BattleUnitRecordItem();

            item.maker = battleUnit;
            item.value = 1f;

            return item;
        }
    }

    public class BattleUnitPointRecorder
    {
        //记录数值最高的单位
        private BattleUnitRecordItem max = null;
        //所有单位
        private List<BattleUnitRecordItem> recordItems = new List<BattleUnitRecordItem>();
        //单位映射
        private Dictionary<int, BattleUnitRecordItem> recordItemDic = new Dictionary<int, BattleUnitRecordItem>();

        //获取记录总数
        public int Count { get { return recordItems.Count; } }

        public void Init(List<BattleUnit> battleUnits)
        {
            if (battleUnits == null || battleUnits.Count <= 0)
                return;

            for (int i = 0; i < battleUnits.Count; i++)
            {
                BattleUnitRecordItem recordItem = BattleUnitRecordItem.CreateInstance(battleUnits[i]);
                recordItem.value = 1f;
                recordItems.Add(recordItem);
                recordItemDic.Add(battleUnits[i].ID, recordItem);
            }
            max = recordItems[0];
        }

        //记录增加量
        public void RecordAddition(BattleUnit maker, float value)
        {
            if (maker == null)
                return;

            BattleUnitRecordItem recordItem = null;
            if (recordItemDic.ContainsKey(maker.ID))
            {
                recordItem = recordItemDic[maker.ID];
            }
            else
            {
                recordItem = BattleUnitRecordItem.CreateInstance(maker);
                recordItem.value = 1f;
                recordItems.Add(recordItem);
                recordItemDic.Add(maker.ID, recordItem);
            }

            recordItem.value += value;
        }

        //获取点数
        public float GetPoint(BattleUnit maker)
        {
            if (recordItemDic.ContainsKey(maker.ID))
                return recordItemDic[maker.ID].point;

            return 0f;
        }

        //获取点数
        public float GetPoint(int battleUnitID)
        {
            if (recordItemDic.ContainsKey(battleUnitID))
                return recordItemDic[battleUnitID].point;

            return 0f;
        }

        /// <summary>
        /// 刷新点数
        /// </summary>
        /// <param name="sorted">是否排序</param>
        public void RefreshPoint(bool sorted)
        {
            //刷新最大
            max = null;
            for (int i = 0; i < recordItems.Count; ++i)
            {
                if (!recordItems[i].maker.CanAction)
                    continue;

                if (max == null)
                    max = recordItems[i];
                else
                    max = recordItems[i].value > max.value ? recordItems[i] : max;
            }

            for (int i = 0; i < recordItems.Count; ++i)
            {
                //最大的都跪了，或者自己跪了
                if (max == null || !max.maker.CanAction || !recordItems[i].maker.CanAction)
                    recordItems[i].point = 0f;
                else
                    recordItems[i].point = EGameConstL.BattleBehaviourChipMaxPoint * (1f - ((float)max.value - recordItems[i].value) / max.value);
            }

            if (sorted)
                recordItems.Sort(LiteSingleton<BattleUnitRecordItemComparer>.Instance);
        }

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <param name="refresh">获取前是否刷新</param>
        /// <param name="sorted">获取前是否排序</param>
        /// <returns></returns>
        public IEnumerator<BattleUnitRecordItem> Items(bool refresh, bool sorted)
        {
            if (refresh)
                RefreshPoint(sorted);

            return recordItems.GetEnumerator();
        }

        //根据索引获取
        public BattleUnitRecordItem GetByIdx(int idx)
        {
            if (idx < 0 || idx >= recordItems.Count)
                return null;

            return recordItems[idx];
        }

        //重置
        public void Clear()
        {
            recordItems.Clear();
            recordItemDic.Clear();
            max = null;
        }

    }
}