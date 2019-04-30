using System.Collections.Generic;
using UnityEngine;

namespace ELGame.BattleBehaviourSystem
{
    [CreateAssetMenu(menuName = "BBSystem/RageChip")]
    public class BattleRageChip
        :ScriptableObject, IBattleBehaviourChip
    {
        public virtual string ChipName => "RageChip";
        public virtual BattleBehaviourType BehaviourType => BattleBehaviourType.Rage;
        public bool NeedRecordSkillResult => true;

        protected BattleBaseData baseData;
        protected BattleBehaviourSystem behaviourSystem;
        protected float rageLevel = 0f; //愤怒值

        protected BattleUnitPointRecorder hateRecorder = new BattleUnitPointRecorder();

        public virtual void Init(BattleBehaviourSystem behaviourSystem, BattleBaseData baseData)
        {
            this.baseData = baseData;
            this.behaviourSystem = behaviourSystem;

            //建立仇恨记录器
            hateRecorder.Init(baseData.enemyBattleTeam.battleUnits);
        }

        public void RecordDamage(BattleUnit maker, float hateAddition)
        {
            if (maker == null || hateAddition <= 0)
                return;

            hateRecorder.RecordAddition(maker, hateAddition);
        }

        public virtual void CalculateBehaviourItem(List<BattleBehaviourItem> behaviourItems, float weight)
        {
            hateRecorder.RefreshPoint(true);

            //为damage加成
            for (int i = 0; i < behaviourItems.Count; i++)
            {
                if (behaviourItems[i].behaviourType != BattleBehaviourType.Damage)
                    continue;

                if (!behaviourItems[i].targetBattleUnit.CanAction)
                    continue;

                int distance = baseData.hostBattleUnit.mapGrid.Distance(behaviourItems[i].targetBattleUnit.mapGrid);
                float distanceWeight = behaviourSystem.GetDistanceWeight(distance);

                //狂暴系数
                float rageRatio = rageLevel / EGameConstL.MaxRageLevel;
                float hateAddition = hateRecorder.GetPoint(behaviourItems[i].targetBattleUnit) * weight * distanceWeight * rageRatio;
                float last = behaviourItems[i].point;
                behaviourItems[i].point = last + hateAddition;
            }
        }

        public virtual void RecordSkillResult(BattleUnit from, BattleUnitSkillResult battleUnitSkillResult)
        {
            //只记录自己
            if (!battleUnitSkillResult.battleUnit.Equals(baseData.hostBattleUnit))
                return;

            if (battleUnitSkillResult.syncAttribute.hpChanged >= 0)
                return;

            float hate = -battleUnitSkillResult.syncAttribute.hpChanged * battleUnitSkillResult.battleSkill.hatredMultiple;
            hateRecorder.RecordAddition(from, hate);

            //愤怒值增加
            rageLevel += battleUnitSkillResult.battleSkill.rageLevel;
            rageLevel = rageLevel > EGameConstL.MaxRageLevel ? EGameConstL.MaxRageLevel : rageLevel;
        }
        
        public void ResetChip()
        {
            hateRecorder.Clear();
        }

        public void RageLevelCooldown()
        {
            rageLevel -= 10f;
            rageLevel = rageLevel < 0f ? 0 : rageLevel;
        }
    }
}
