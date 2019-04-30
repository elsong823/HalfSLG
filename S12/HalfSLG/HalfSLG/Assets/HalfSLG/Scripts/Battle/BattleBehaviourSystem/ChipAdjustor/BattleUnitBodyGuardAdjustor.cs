using System.Collections.Generic;
using UnityEngine;

namespace ELGame.BattleBehaviourSystem
{
    [CreateAssetMenu(menuName = "BBSystem/BodyGuardAdjustor", order = 202)]
    public class BattleUnitBodyGuardAdjustor
        : ScriptableObject, IBattleBehaviourChipAdjustor
    {
        [Range(0f, 2f)] public float tankMultiple = 0f;          //坦克占比
        [Range(0f, 2f)] public float dpsMultiple = 0f;           //输出占比
        [Range(0f, 2f)] public float supportMultiple = 0f;       //辅助占比
        
        public void AdjustBehaviourItem(List<BattleBehaviourItem> behaviourList)
        {
            //根据 目标 的 目标 类型修正行为点数
            float originMax = 0f;
            float newMax = 0f;
            //计算两个和
            for (int i = 0; i < behaviourList.Count; i++)
            {
                originMax += behaviourList[i].point;
                //当前这个家伙没有目标
                if (behaviourList[i].targetBattleUnit.targetBattleUnit == null)
                {
                    behaviourList[i].point = 1f * behaviourList[i].point;
                    newMax += behaviourList[i].point;
                }
                //有目标
                else
                {
                    switch (behaviourList[i].targetBattleUnit.targetBattleUnit.battleBehaviourSystem.battleUnitRole)
                    {
                        case BattleUnitRole.Tank:
                            behaviourList[i].point *= (1f + tankMultiple);
                            newMax += behaviourList[i].point;
                            break;
                        case BattleUnitRole.DPS:
                            behaviourList[i].point *= (1f + dpsMultiple);
                            newMax += behaviourList[i].point;
                            break;
                        case BattleUnitRole.Support:
                            behaviourList[i].point *= (1f + supportMultiple);
                            newMax += behaviourList[i].point;
                            break;
                        default:
                            break;
                    }
                }
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