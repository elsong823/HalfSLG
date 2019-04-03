using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame.BattleBehaviourSystem
{
    public abstract class BattleStrategyChip 
        :ScriptableObject, IBattleBehaviourChip
    {
        public abstract string ChipName { get; }
        public abstract bool NeedRecordSkillResult { get; }
        public abstract BattleBehaviourType BehaviourType { get; }

        public List<ScriptableObject> adjustorTransfer;
        protected List<IBattleBehaviourChipAdjustor> adjustors;
        protected BattleBaseData baseData;
        protected BattleBehaviourSystem behaviourSystem;
        protected List<BattleBehaviourItem> battleBehaviourItems;

        public virtual void Init(BattleBehaviourSystem behaviourSystem, BattleBaseData baseData)
        {
            this.baseData = baseData;
            this.behaviourSystem = behaviourSystem;
            battleBehaviourItems = new List<BattleBehaviourItem>(baseData.enemyBattleTeam.battleUnits.Count);
            if (adjustorTransfer != null && adjustorTransfer.Count > 0)
            {
                adjustors = new List<IBattleBehaviourChipAdjustor>();
                for (int i = 0; i < adjustorTransfer.Count; i++)
                {
                    if (adjustorTransfer[i] is IBattleBehaviourChipAdjustor)
                        adjustors.Add(adjustorTransfer[i] as IBattleBehaviourChipAdjustor);
                }
            }
        }

        public abstract void CalculateBehaviourItem(List<BattleBehaviourItem> behaviourItems, float weight);

        public abstract void RecordSkillResult(BattleUnit from, BattleUnitSkillResult battleUnitSkillResult);

        public abstract void ResetChip();

        protected void AddToTargetList(List<BattleBehaviourItem> targetList)
        {
            if (adjustors != null && adjustors.Count > 0)
            {
                for (int i = 0; i < adjustors.Count; ++i)
                {
                    adjustors[i].AdjustBehaviourItem(battleBehaviourItems);
                }
            }

            targetList.AddRange(battleBehaviourItems);

            battleBehaviourItems.Clear();
        }
    }
}