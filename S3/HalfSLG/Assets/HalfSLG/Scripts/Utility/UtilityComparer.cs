using System.Collections;
using System.Collections.Generic;

namespace ELGame
{
    //距离比较
    public class BattleUnitDistanceComparer : IComparer<BattleUnit>
    {
        private static BattleUnitDistanceComparer instance;
        public static BattleUnitDistanceComparer Instance
        {
            get
            {
                if (instance == null)
                    instance = new BattleUnitDistanceComparer();

                return instance;
            }
        }

        public int Compare(BattleUnit x, BattleUnit y)
        {
            return -1;
        }
    }
}