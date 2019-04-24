using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame.BattleBehaviourSystem
{
    [CreateAssetMenu(menuName = "BBSystem/KillerAdjustor", order = 200)]
    public class BattleUnitKillerChipAdjustor
        : ScriptableObject, IBattleBehaviourChipAdjustor
    {
        public void AdjustBehaviourItem(List<BattleBehaviourItem> behaviourList)
        {
            if (behaviourList == null)
                return;

            //根据血量进行调整
            float originMax = 0f;
            float newMax = 0f;
            //计算两个和
            for (int i = 0; i < behaviourList.Count; i++)
            {
                originMax += behaviourList[i].point;
                float hpElapsed = ((float)behaviourList[i].targetBattleUnit.battleUnitAttribute.maxHp - behaviourList[i].targetBattleUnit.battleUnitAttribute.hp) / behaviourList[i].targetBattleUnit.battleUnitAttribute.maxHp;
                float killerWeight = Mathf.Lerp(0.1f, 1f, hpElapsed / 0.95f);
                behaviourList[i].point *= killerWeight;
                newMax += behaviourList[i].point;
            }
            //调整
            for (int i = 0; i < behaviourList.Count; i++)
            {
                if (newMax < Mathf.Epsilon)
                    behaviourList[i].point = 0f;
                else
                    behaviourList[i].point = originMax * behaviourList[i].point / newMax;
            }
        }
    }
}