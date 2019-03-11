
using UnityEngine;

namespace ELGame
{
    public class BattleUnitManager
        : ELSingletonDic<BattleUnitManager, BattleUnit>, IGameBase
    {
        public string Desc()
        {
            return string.Empty;
        }

        public void Init(params object[] args)
        {
            UtilityHelper.Log("Battle unit data manager inited.");
        }

        public BattleUnit CreateUnit(SO_BattleUnitAttribute battleUnitAttribute)
        {
            BattleUnit data = null;
            int id = 0;
            base.Create(out data, out id);
            if (data != null)
            {
                data.battleUnitID = id;
                //设置属性
                data.battleUnitAttribute = GameObject.Instantiate<SO_BattleUnitAttribute>(battleUnitAttribute);;
            }

            return data;
        }
    }
}