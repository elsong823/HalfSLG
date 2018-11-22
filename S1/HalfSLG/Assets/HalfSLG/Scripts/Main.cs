using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class Main
        : ELBehaviour
    {
        private List<BattleData> battleDataList = new List<BattleData>();

        private void Awake()
        {
            Random.InitState((int)System.DateTime.Now.Ticks);
        }

        private void Start()
        {
            //Test
            //创建10个
            for (int i = 0; i < 10; ++i)
            {
                BattleData bd = BattleCreator.Instance.CreateBattle();
                battleDataList.Add(bd);
            }
            BattleField.Instance.LoadBattleData(battleDataList[idx]);
        }

        public int idx = 0;
        private void OnGUI()
        {
            if (GUI.Button(new Rect(0, 0, 100, 100), "Next"))
            {
                ++idx;
                if (idx >= 10)
                    idx = 0;
                BattleField.Instance.LoadBattleData(battleDataList[idx]);
            }
        }
    }
}