//仇恨记录器
//记录一个战斗单位对其他敌对单位的仇恨值

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace ELGame
{
    public class HatredItem
    {
        public BattleUnit battleUnit;
        public int hatred;
    }

    public class HatredRecorder 
        :IComparer<HatredItem>
    {
        private BattleUnit host;
        //仇恨列表
        private List<HatredItem> hatredList = new List<HatredItem>(5);

        //重置
        public void Reset(BattleUnit hostUnit, BattleTeam enemyTeam)
        {
            Clean(false);

            if (hostUnit == null || enemyTeam == null)
            {
                UtilityHelper.LogError("Reset hatred recoreder failed.");
                return;
            }

            host = hostUnit;

            for (int i = 0; i < enemyTeam.battleUnits.Count; ++i)
            {
                if(i >= hatredList.Count)
                    hatredList.Add(new HatredItem());

                hatredList[i].battleUnit = enemyTeam.battleUnits[i];
                hatredList[i].hatred = 0;
            }

            if (hatredList.Count > enemyTeam.battleUnits.Count)
                hatredList.RemoveRange(enemyTeam.battleUnits.Count, hatredList.Count - enemyTeam.battleUnits.Count);
        }

        //清空
        public void Clean(bool resetLength)
        {
            host = null;

            for (int i = 0; i < hatredList.Count; i++)
            {
                hatredList[i].battleUnit = null;
                hatredList[i].hatred = 0;
            }
        }

        //排序仇恨列表
        private void SortHatred()
        {
            //简单的排列
            hatredList.Sort(this);
        }

        //记录仇恨(做加法)
        public void RecoredHatred(int battleUnitID, int hatredIncrease)
        {
            for (int i = 0; i < hatredList.Count; ++i)
            {
                if (hatredList[i].battleUnit.battleUnitID == battleUnitID)
                {
                    //原始仇恨值
                    int originHatred = hatredList[i].hatred;
                    originHatred += hatredIncrease;
                    //仇恨不能小于0
                    originHatred = originHatred < 0 ? 0 : originHatred;
                    //记录新的仇恨
                    hatredList[i].hatred = originHatred;
                    return;
                }
            }
            UtilityHelper.LogError(string.Format("Record hatred failed .. can not find UID in hatred list -> {0}", battleUnitID));
            Debug.Log(Desc());
        }

        //记录仇恨的数量
        public int HatredCount
        {
            get
            {
                return hatredList.Count;
            }
        }

        //根据索引获得仇恨列表中的战斗单位id
        public BattleUnit GetHatredByIdx(int idx, bool sort)
        {
            if (hatredList.Count == 0)
                return null;

            if (sort)
                SortHatred();

            return hatredList[idx > hatredList.Count - 1 ? hatredList.Count : idx].battleUnit;
        }

        public string Desc()
        {
            SortHatred();
            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < hatredList.Count; ++i)
            {
                strBuilder.AppendFormat("UID (Hatred) : {0} ({1})\n", hatredList[i].battleUnit.battleUnitID, hatredList[i].hatred);
            }
            return strBuilder.ToString();
        }

        public int Compare(HatredItem x, HatredItem y)
        {
            int weightX = x.battleUnit.CanAction ? x.hatred : -EGameConstL.Infinity;
            int weightY = y.battleUnit.CanAction ? y.hatred : -EGameConstL.Infinity;

            //权重相同优先距离
            //已经不能行动的单位无法找到mapGrid
            if (weightX == weightY && weightX >= 0 && weightY >= 0)
                return x.battleUnit.mapGrid.Distance(host.mapGrid) - y.battleUnit.mapGrid.Distance(host.mapGrid);
            else
                return weightY - weightX;
        }
    }
}