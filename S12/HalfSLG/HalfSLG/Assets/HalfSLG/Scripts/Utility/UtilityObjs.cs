//共用对象，用时注意clear

using System.Collections;
using System.Collections.Generic;

namespace ELGame
{
    public static class UtilityObjs
    {
        //通用的战斗单位list
        public static List<BattleUnit> battleUnits = new List<BattleUnit>(10);
        //通用的格子单位List
        public static List<GridUnit> gridUnits = new List<GridUnit>(20);
        //通用的战斗消息List
        public static List<BattleFieldEvent> battleActions = new List<BattleFieldEvent>(2);
    }
}