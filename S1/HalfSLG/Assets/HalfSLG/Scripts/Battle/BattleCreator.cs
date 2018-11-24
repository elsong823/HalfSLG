using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class BattleCreator 
    {
        private static BattleCreator instance;
        public static BattleCreator Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BattleCreator();
                    instance.Init();
                }
                return instance;
            }
        }

        private bool inited = false;

        private void Init()
        {
            if (inited)
                return;





            inited = true;
            EUtilityHelperL.Log("Battle creator inited.");
        }

        //创建一场战斗
        public BattleData CreateBattle()
        {
            BattleData bd = new BattleData();
            bd.Generate(8, 12, 10, 2);
            return bd;
        }
    }
}