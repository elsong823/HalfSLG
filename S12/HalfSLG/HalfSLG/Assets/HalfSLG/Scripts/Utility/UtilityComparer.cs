using System.Collections;
using System.Collections.Generic;
using ELGame.BattleBehaviourSystem;

namespace ELGame
{
    public class BattleBehaviourItemComparer
        : IComparer<ELGame.BattleBehaviourSystem.BattleBehaviourItem>
    {
        public int Compare(BattleBehaviourItem x, BattleBehaviourItem y)
        {
            return (int)(y.point * 1000) - (int)(x.point * 1000);
        }
    }

    public class BattleUnitRecordItemComparer
        : IComparer<ELGame.BattleBehaviourSystem.BattleUnitRecordItem>
    {
        public int Compare(BattleUnitRecordItem x, BattleUnitRecordItem y)
        {
            int value_X = x.maker.CanAction ? (int)x.point * 1000 : -1;
            int value_Y = y.maker.CanAction ? (int)y.point * 1000 : -1;

            return value_Y - value_X;
        }
    }
    
    //距离比较
    public class BattleUnitDistanceComparer 
        : IComparer<BattleUnit>
    {
        public int Compare(BattleUnit x, BattleUnit y)
        {
            return -1;
        }
    }

    //UIView名字的比较器
    public class EnumUIViewNameComparer
        : IEqualityComparer<UIViewName>
    {
        public bool Equals(UIViewName x, UIViewName y)
        {
            return x == y;
        }

        public int GetHashCode(UIViewName obj)
        {
            return (int)obj;
        }
    }
    
    //UIView层级
    public class EnumUIViewLayerComparer
        : IEqualityComparer<UIViewLayer>
    {
        public bool Equals(UIViewLayer x, UIViewLayer y)
        {
            return x == y;
        }

        public int GetHashCode(UIViewLayer obj)
        {
            return (int)obj;
        }
    }

    //战斗单位事件的类型
    public class EnumBattleFieldEventTypeComparer
        : IEqualityComparer<BattleFieldEventType>
    {
        public bool Equals(BattleFieldEventType x, BattleFieldEventType y)
        {
            return x == y;
        }

        public int GetHashCode(BattleFieldEventType obj)
        {
            return (int)obj;
        }
    }
}