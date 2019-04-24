using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ELGame.AI
{
    public class StrategyDataCenter
    {

        private static StrategyDataCenter instance;
        public static StrategyDataCenter Instance
        {
            get
            {
                if(null == instance)
                {
                    instance = new StrategyDataCenter();
                }
                return instance;
            }
        }
        private StrategyDataCenter()
        {
            // 当任何 BattleAction 发生时，抛出事件
            // xxEvent += OnBattleActionDidHappen;
        }



        public event Action<BattleFieldEvent> onBattleAction;

        public readonly DataPackBattleField dataPackBattleField = new DataPackBattleField();
        public readonly Dictionary<int,DataPackBattleTeam> dataPackBattleTeam = new Dictionary<int, DataPackBattleTeam>();

        void OnBattleActionDidHappen(BattleFieldEvent battleAction)
        {
            // 1. 更新 battleFieldData
            dataPackBattleField.UpdateValues(battleAction);

            // 2. 更新 战队的信息
            foreach(var d in dataPackBattleTeam)
            {
                d.Value.UpdateValues(battleAction);
            }

            // 3. 分发事件给大脑
            if (null!= onBattleAction)
            {
                onBattleAction.Invoke(battleAction);
            }
        }


    }
}
