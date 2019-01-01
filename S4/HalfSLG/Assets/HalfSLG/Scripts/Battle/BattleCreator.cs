using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BattleCreator 
        :NormalSingleton<BattleCreator>, IGameBase
    {
        private int battleID = 0;

        private bool inited = false;

        public void Init(params object[] args)
        {
            if (inited)
                return;

            
            inited = true;
            UtilityHelper.Log("Battle creator inited.");
        }

        public BattleField CreateBattle(int width, int height, int obstacleCount, int gap, int battleUnitCount)
        {
            BattleField bd = new BattleField();
            bd.Generate(width, height, obstacleCount, gap, battleUnitCount);
            bd.battleID = battleID++;
            return bd;
        }
        
        public string Desc()
        {
            return string.Empty;
        }
    }
}