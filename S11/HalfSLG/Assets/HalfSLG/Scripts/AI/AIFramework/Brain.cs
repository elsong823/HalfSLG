using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame.AI
{

    public class Brain : IBattleActionCreator
    {

        public BattleState currentState {get; set;}

        BattleStrategy battleStrategy { get; set; }

        public readonly Queue<Transition> pendingTransitions = new Queue<Transition>();
        public readonly Queue<BattleFieldEvent> pendingBattleActions = new Queue<BattleFieldEvent>();

        BattleUnit owner;

        public bool Init(BattleUnit battleUnit)
        {
            owner = battleUnit;

            StrategyDataCenter.Instance.onBattleAction -= HandleBattleAction;
            StrategyDataCenter.Instance.onBattleAction += HandleBattleAction;

            battleStrategy = ScriptableObject.CreateInstance<BattleStrategy>();
            battleStrategy.InitWithBrain(this);
            
            currentState = BattleState.Default;

            return true;
        }

        public readonly DataPackSelf dataPackSelf = new DataPackSelf();
        public readonly Dictionary<int,DataPackRelation> dataPackRelation = new Dictionary<int,DataPackRelation>();

        public void CleanUp()
        {
            this.currentState = null;
            this.battleStrategy = null;
            StrategyDataCenter.Instance.onBattleAction += HandleBattleAction;
        }


        // 【大脑的输入】
        void HandleBattleAction(BattleFieldEvent battleAction)
        {
            // 1. 刷新数据 self
            // 可能会产生transition
            dataPackSelf.UpdateValues(battleAction);

            // 2. 刷新数据 relation
            // 可能会产生transition
            foreach(var d in dataPackRelation)
            {
                d.Value.UpdateValues(battleAction);
            }

            // 3. 判断 是否转变state
            while(pendingTransitions.Count > 0)
            {
                Transition transition = pendingTransitions.Dequeue();
                if (Transition.IsValid(transition))
                {
                    pendingTransitions.Clear(); // 直接丢弃后边的transition？
                    transition.DoTransition(this);
                }
            }
        }

        // 【大脑的输出】
        public BattleFieldEvent CreateBattleAction()
        {
            currentState.OnUpdate(this);

            if(pendingBattleActions.Count > 0)
            {
                return pendingBattleActions.Dequeue();
            }
            return null;
        }
    
    
        // 获取对应对BattleUnit自己
        public BattleUnit GetSelf()
        {
            return owner;
        }


    }
}
