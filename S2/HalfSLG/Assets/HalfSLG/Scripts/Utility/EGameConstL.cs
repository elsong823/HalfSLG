using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class EGameConstL
    {
        public const string GameName = "HalfSLG";

        public const bool Map_FirstRowOffset = false;    //true:地图首行向右偏移半个单位
        public const float Map_GridWidth = 2.56f;
        public const float Map_GridOffsetY = 1.92f;
        public const float Map_HexRadius = 1.478f;

        public const int WorldMapMaxTryTimes = 99;
        public const int Infinity = 999999;

        public const string Tag_BattleCamera = "BattleCamera";
    }
}