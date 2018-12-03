using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BattleData
    {
        public BattleMapData mapData;

        public void Generate(int width, int height, int obstacleCount, int gap, int battleUnitCount)
        {
            GenerateMap(width, height, obstacleCount, gap);
        }

        //生成地图
        private void GenerateMap(int width, int height, int obstacleCount, int gap)
        {
            //创建地图
            mapData = BattleMapManager.Instance.CreateMap(width, height, obstacleCount, gap);
        }
    }
}