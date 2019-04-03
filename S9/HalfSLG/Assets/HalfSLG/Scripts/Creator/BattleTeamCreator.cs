
using System.Collections.Generic;

namespace ELGame
{
    public class BattleTeamCreator
        : CounterMap<BattleTeamCreator, BattleTeam>, IGameBase
    {
        public string Desc()
        {
            return string.Empty;
        }

        public void Init(params object[] args)
        {
            BattleManager.Instance.MgrLog("Battle team creator inited.");
        }

        public BattleTeam Create(List<SO_BattleUnitAttribute> members)
        {
            BattleTeam battleTeam = Create();
            for (int i = 0; i < members.Count; ++i)
            {
                //创建战斗单位
                BattleUnit battleUnit = BattleUnitCreator.Instance.Create(members[i]);
                //加入队伍
                battleTeam.AddBattleUnit(battleUnit);
            }
            return battleTeam;
        }
    }
}