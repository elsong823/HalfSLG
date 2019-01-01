using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{

    public class BattleMapManager
        : ELSingletonDic<BattleMapManager, BattleMap>, IGameBase
    {
        public string Desc()
        {
            return string.Empty;
        }

        public void Init(params object[] args)
        {
            UtilityHelper.Log("Battle map manager inited.");
        }

        public BattleMap CreateMap(int width, int height, int obstacleCount, int obstacleGap)
        {
            BattleMap battleMapData = null;
            int mapID = 0;
            base.Create(out battleMapData, out mapID);
            if (battleMapData != null)
            {
                battleMapData.mapID = mapID;
                battleMapData.Generate(width, height, obstacleCount, obstacleGap);
            }
            else
            {
                UtilityHelper.LogError(string.Format("Create map failed->width:{0},height:{1}",
                    width, height));
            }
            return battleMapData;
        }
    }
}