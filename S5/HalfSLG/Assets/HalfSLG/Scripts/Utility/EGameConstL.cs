using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class EGameConstL
    {
        public const string GameName = "HalfSLG";

        public const bool Map_FirstRowOffset = true;    //true:地图首行向右偏移半个单位
        public const float Map_GridWidth = 2.56f;
        public const float Map_GridOffsetY = 1.92f;
        public const float Map_HexRadius = 1.478f;

        public static WaitForSeconds WaitForOneSecond = new WaitForSeconds(1f);
        public static WaitForSeconds WaitForHalfSecond = new WaitForSeconds(0.5f);
        public static WaitForSeconds WaitForDotOneSecond = new WaitForSeconds(0.1f);
        public static WaitForTouchScreen WaitForTouchScreen = new WaitForTouchScreen();

        //一场战斗允许最多的行动次数
        public const int BattleFieldMaxActions = 999;

        public const int WorldMapMaxTryTimes = 99;
        public const int Infinity = 999999;

        //颜色
        public static Color Color_Transparent = new Color(0,0,0,0);
        public static Color Color_Yellow = new Color(255f/255f, 255f/255f, 0f/255f, 255f/255f);
        public static Color Color_Cyan = new Color(0f/255f, 255f/255f, 213f/255f, 255f/255f);
        public static Color Color_GreenApple = new Color(144f/255f, 255f/255f, 1f/255f, 255f/255f);

        //每一行层级的间隔
        public const int OrderGapPerRow = 10;
        public const int OrderIncrease_BattleUnit = 2;

        public const string Tag_BattleCamera = "BattleCamera";
        public const string Tag_UIViewRoot = "UIViewRoot";

        public const string STR_Unused = "UNUSED";
        public const string STR_BG = "BG";

        public const string SortingLayer_Background = "UI_Background";
        public const string SortingLayer_Base = "UI_Base";
        public const string SortingLayer_Popup = "UI_Popup";
        public const string SortingLayer_Top = "UI_Top";
        public const string SortingLayer_Alert = "UI_Alert";
        public const string SortingLayer_Debug = "UI_Debug";

        public const string Other_Clone_prefix = "(Clone)";
    }
}