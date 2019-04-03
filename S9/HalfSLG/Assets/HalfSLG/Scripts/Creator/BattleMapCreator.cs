using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{

    public class BattleMapCreator
        : CounterMap<BattleMapCreator, BattleMap>, IGameBase
    {
        public string Desc()
        {
            return string.Empty;
        }

        public void Init(params object[] args)
        {
            BattleManager.Instance.MgrLog("Battle map creator inited.");
        }

        public BattleMap Create(int width, int height, int obstacleCount, int obstacleGap)
        {
            BattleMap battleMap = Create();
            battleMap.Init(width, height, obstacleCount, obstacleGap);
            return battleMap;
        }
    }
}