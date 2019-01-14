using System.Collections;
using System.Collections.Generic;

namespace ELGame
{
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
}