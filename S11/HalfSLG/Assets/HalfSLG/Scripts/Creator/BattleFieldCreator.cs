using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BattleFieldCreator
        : CounterMap<BattleFieldCreator, BattleField>, IGameBase
    {
        public string Desc() { return string.Empty; }

        public void Init(params object[] args)
        {
            BattleManager.Instance.MgrLog("Battle field creator inited.");
        }

        public BattleField Create(
            int mapWidth, int mapHeight, 
            int obstacleCount, int obstacleGap, 
            int buffCount, int itemCount,
            List<SO_BattleUnitAttribute> teamA, List<SO_BattleUnitAttribute> teamB)
        {
            BattleField battleField = Create();
            battleField.Init(mapWidth, mapHeight, obstacleCount, obstacleGap, buffCount, itemCount, teamA, teamB);
            return battleField;
        }
    }
}