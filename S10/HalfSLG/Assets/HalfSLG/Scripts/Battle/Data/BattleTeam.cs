using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ELGame
{
    public class BattleTeam
        :CountableInstance
    {
        public BattleField battleField;
        public List<BattleUnit> battleUnits = new List<BattleUnit>();
        private Dictionary<int, BattleUnit> battleUnitsDic = new Dictionary<int, BattleUnit>();

        //添加战斗单位
        public void AddBattleUnit(BattleUnit battleUnit)
        {
            if (battleUnit == null)
                return;
            
            if (battleUnit.battleTeam != null)
            {
                //重复加入
                if (battleUnit.battleTeam.Equals(this))
                    return;

                UtilityHelper.LogError("Add battle unit failed.Battle unit already joined a team.");
                return;
            }

            //重复添加
            if (battleUnits.Contains(battleUnit))
                return;

            //加入
            battleUnits.Add(battleUnit);
            battleUnitsDic.Add(battleUnit.ID, battleUnit);

            battleUnit.JoinBattleTeam(this);
        }

        //移除战斗单位
        public void RemoveBattleUnit(BattleUnit battleUnit)
        {
            if (battleUnit.battleTeam == null || !battleUnit.battleTeam.Equals(this))
            {
                UtilityHelper.LogError("Remove battle unit failed.");
                return;
            }

            //移出
            battleUnits.Remove(battleUnit);
            battleUnitsDic.Remove(battleUnit.ID);

            battleUnit.QuitBattleTeam();
        }
        
        //根据id获取单位
        public BattleUnit GetBattleUnitByID(int battleUnitID)
        {
            if (battleUnitsDic.ContainsKey(battleUnitID))
                return battleUnitsDic[battleUnitID];

            return null;
        }

        //进入战场
        public void EnterBattleField(BattleField battleField, GridUnit[] bornGrids)
        {
            if (battleField == null)
            {
                UtilityHelper.LogError("Enter battle field failed. None battle field.");
                return;
            }

            if (bornGrids == null || bornGrids.Length == 0)
            {
                UtilityHelper.LogError("Enter battle field failed. None born grids");
                return;
            }

            this.battleField = battleField;

            //打乱
            for (int i = 0; i < battleUnits.Count; i++)
            {
                if (i >= bornGrids.Length)
                {
                    UtilityHelper.LogError("Battle unit enter battle field failed. Born grid not enough.");
                    continue;
                }
                battleUnits[i].EnterBattleField(battleField, bornGrids[i]);
            }
        }

        //离开战场
        public void LeaveBattleField()
        {
            if (battleField != null)
            {
                for (int i = 0; i < battleUnits.Count; i++)
                {
                    battleUnits[i].LeaveBattleField();
                }
                battleField = null;
            }
        }
        
        public override bool Equals(object obj)
        {
            if (obj != null && obj is BattleTeam) 
            {
                return ID == ((BattleTeam)obj).ID;
            }
            return false;
        }

        public string Desc()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Team id = {0}\n", ID);
            for (int i = 0; i < battleUnits.Count; ++i)
            {
                sb.AppendFormat(" {0}\n", battleUnits[i].Desc());
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            return string.Format("Team_{0}", ID);
        }
    }
}