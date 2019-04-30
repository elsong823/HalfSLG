namespace ELGame
{
    public enum GridUnitEventType
    {
        RefreshItems,   //刷新所持有的道具
    }

    public class GridUnitEvent 
        : BattleFieldEvent
    {
        public GridUnitEventType gridUnitEventType;
        public GridUnit grid;

        protected GridUnitEvent() : base(BattleFieldEventType.GridUnit) { }

        //创建事件
        public static T CreateEvent<T>(GridUnitEventType gridUnitEventType, GridUnit grid)
            where T : GridUnitEvent, new()
        {
            var e = new T();
            e.grid = grid;
            e.gridUnitEventType = gridUnitEventType;
            return e;
        }
    }

    //刷新道具事件
    public class GridUnitRefreshItemsEvent
        : GridUnitEvent
    {
        public int itemID;      //道具ID
        public int itemCount;   //道具数量
    }
}