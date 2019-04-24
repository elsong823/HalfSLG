using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public enum GridUnitBuffType
    {
        None,
        Atk,    //攻击 增加
        Def,    //防御 增加
        Range,  //射程 增加
    }

    public class GridUnitBuff
    {
        public GridUnitBuffType buffType;
        public int addition;

        private GridUnitBuff() { }

        public static GridUnitBuff CreateInstance(GridUnitBuffType gridBuffType, int addition)
        {
            GridUnitBuff buff = new GridUnitBuff();
            buff.buffType = gridBuffType;
            buff.addition = addition;

            return buff;
        }
        
    }
}