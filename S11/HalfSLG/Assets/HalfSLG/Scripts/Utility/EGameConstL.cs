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

        public static WaitForSeconds WaitForOneSecond = new WaitForSeconds(1f);
        public static WaitForSeconds WaitForHalfSecond = new WaitForSeconds(0.5f);
        public static WaitForSeconds WaitForDotOneSecond = new WaitForSeconds(0.1f);
        public static WaitForTouchScreen WaitForTouchScreen = new WaitForTouchScreen();

        //一场战斗允许最多的行动次数
        public const int BattleFieldMaxActions = 999;

        public const int WorldMapMaxTryTimes = 99;
        public const int Infinity = 999999;

        //导航可通行遮罩
        public const int NavigationPassableMask = 64;
        

        //颜色
        public static readonly Color Color_Transparent = new Color(0, 0, 0, 0);
        public static readonly Color Color_Translucence = new Color(1, 1, 1, 0.5f);
        public static readonly Color Color_Yellow = new Color(255f / 255f, 255f / 255f, 0f / 255f, 255f / 255f);
        public static readonly Color Color_Cyan = new Color(0f / 255f, 255f / 255f, 213f / 255f, 255f / 255f);
        public static readonly Color Color_GreenApple = new Color(144f / 255f, 255f / 255f, 1f / 255f, 255f / 255f);
        public static readonly Color Color_moveRange = new Color(146f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);            //移动区域
        public static readonly Color Color_skillReleaseRange = new Color(255f / 255f, 201f / 255f, 245f / 255f, 255f / 255f);    //技能释放区域
        public static readonly Color Color_skillEffectRange = new Color(255f / 255f, 92f / 255f, 91f / 255f, 255f / 255f);       //技能影响区域
        public static readonly Color Color_battleUnitSelectable = new Color(0f / 255f, 255f / 255f, 105f / 255f, 255f / 255f);
        public static readonly Color Color_battleUnitAction = new Color(255f / 255f, 155f / 255f, 0f / 255f, 255f / 255f);
        public static readonly Color Color_labelWhite = new Color(1f, 1f, 1f, 1f);
        public static readonly Color Color_labelRed = new Color(255f / 255f, 53f / 255f, 26f / 255f, 255f / 255f);
        public static readonly Color Color_GridBuff_Atk = new Color(255f / 255f, 53f / 255f, 26f / 255f, 255f / 255f);
        public static readonly Color Color_GridBuff_Def = new Color(0f / 255f, 245f / 255f, 10f / 255f, 255f / 255f);
        public static readonly Color Color_GridBuff_Range = new Color(245f / 255f, 245f / 255f, 10f / 255f, 255f / 255f);


        //每一行层级的间隔
        public const int OrderGapPerRow = 10;
        public const int OrderIncrease_BattleUnit = 5;

        public const string Tag_BattleCamera = "BattleCamera";
        public const string Tag_UIViewRoot = "UIViewRoot";
        public const string Tag_WorldRoot = "WorldRoot";

        //动画Key
        public static readonly int HashACKey_NormalDamage = Animator.StringToHash("NormalDamage");
        public static readonly int HashACKey_NormalMagicDamage = Animator.StringToHash("NormalMagicDamage");
        public static readonly int HashACKey_CritDamage = Animator.StringToHash("CritDamage");
        public static readonly int HashACKey_NormalHeal = Animator.StringToHash("NormalHeal");

        public const string STR_Unused = "UNUSED";
        public const string STR_BG = "BG";
        public const string STR_Grid = "Grid";
        public const string STR_BattleUnit = "BattleUnit";
        public const string STR_SkillBtn = "SkillBtn_";
        public const string STR_ItemBtn = "ItemBtn_";

        public static readonly int SortingLayer_Battle_Map = SortingLayer.NameToID("Battle_Map");
        public static readonly int SortingLayer_Battle_Bar = SortingLayer.NameToID("Battle_Bar");
        public static readonly int SortingLayer_Battle_Effect = SortingLayer.NameToID("Battle_Effect");
        public static readonly int SortingLayer_UI_Background = SortingLayer.NameToID("UI_Background");
        public static readonly int SortingLayer_UI_Base = SortingLayer.NameToID("UI_Base");
        public static readonly int SortingLayer_UI_Popup = SortingLayer.NameToID("UI_Popup");
        public static readonly int SortingLayer_UI_Top = SortingLayer.NameToID("UI_Top");
        public static readonly int SortingLayer_UI_Debug = SortingLayer.NameToID("UI_Debug");

        public const string Other_Clone_prefix = "(Clone)";

        public const string Effect_HexFrame = "Effect_HexFrame";
        public const string Effect_DamageLabel = "Effect_DamageLabel";

        //GAME
        public const int EnergyRecoverPerRound = 10;   //每回合恢复的能量值

        public const float BattleBehaviourChipMaxPoint = 100f;
        public const float MaxRageLevel = 100f;


        public const string EVENT_RESOURCE_MANAGER_READY = "EVENT_RESOURCE_MANAGER_READY";              //资源管理器准备完毕
        public const string EVENT_BATTLE_FIELD_RENDERER_READY = "EVENT_BATTLE_FIELD_RENDERER_READY";    //事件：战场渲染器准备完毕
        public const string EVENT_MANUAL_OPERATION_COMPLETE = "EVENT_MANUAL_OPERATION_COMPLETE";        //手动操作完毕
        public const string EVENT_UPDATE_BATTLE_UNIT_PACKAGE = "EVENT_UPDATE_BATTLE_UNIT_PACKAGE";      //更新战斗单位的背包
        public const string EVENT_BATTLE_UNIT_PACKAGE_CHANGED = "EVENT_BATTLE_UNIT_PACKAGE_CHANGED";    //某个战斗单位的背包发生了变化
        public const string EVENT_BATTLE_UNIT_OR_GRID_TOUCHED = "EVENT_BATTLE_UNIT_OR_GRID_TOUCHED";    //点击了地图格子或战斗单位
        public const string EVENT_BATTLE_UNIT_USE_ITEM = "EVENT_BATTLE_UNIT_USE_ITEM";                  //战斗单位使用道具

        public const string EVENT_BATTLE_UNIT_MANUAL_STATE_CHANGED = "EVENT_BATTLE_UNIT_MANUAL_STATE_CHANGED";
    }
}