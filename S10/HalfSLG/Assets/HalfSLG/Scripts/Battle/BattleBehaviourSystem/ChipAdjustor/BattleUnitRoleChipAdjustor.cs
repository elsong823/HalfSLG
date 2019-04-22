using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ELGame.BattleBehaviourSystem
{

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BattleUnitRoleChipAdjustor))]
    public class BattleUnitRoleChipAdjustorEditor
        : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Refresh Pct"))
            {
                BattleUnitRoleChipAdjustor adjustor = (BattleUnitRoleChipAdjustor)target;
                if (adjustor != null)
                    adjustor.RefreshPct();
            }
        }
    }
#endif


    [CreateAssetMenu(menuName = "BBSystem/RoleAdjustor", order = 201)]
    public class BattleUnitRoleChipAdjustor
        : ScriptableObject, IBattleBehaviourChipAdjustor
    {
        [Range(0.1f, 1f)] public float dpsPct = 1f;           //输出占比
        [Range(0.1f, 1f)] public float tankPct = 1f;          //坦克占比
        [Range(0.1f, 1f)] public float supportPct = 1f;     //辅助占比

        public void RefreshPct()
        {
            float sum = dpsPct + tankPct + supportPct;
            dpsPct /= sum;
            tankPct /= sum;
            supportPct /= sum;
        }

        public void AdjustBehaviourItem(List<BattleBehaviourItem> behaviourList)
        {
            //根据职业类型修正行为点数
            float originMax = 0f;
            float newMax = 0f;
            //计算两个和
            for (int i = 0; i < behaviourList.Count; i++)
            {
                originMax += behaviourList[i].point;
                switch (behaviourList[i].targetBattleUnit.battleBehaviourSystem.battleUnitRole)
                {
                    case BattleUnitRole.Tank:
                        behaviourList[i].point *= (1f + tankPct);
                        newMax += behaviourList[i].point;
                        break;
                    case BattleUnitRole.DPS:
                        behaviourList[i].point *= (1f + dpsPct);
                        newMax += behaviourList[i].point;
                        break;
                    case BattleUnitRole.Support:
                        behaviourList[i].point *= (1f + supportPct);
                        newMax += behaviourList[i].point;
                        break;
                    default:
                        break;
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