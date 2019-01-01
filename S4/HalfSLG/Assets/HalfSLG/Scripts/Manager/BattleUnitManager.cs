
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

        public BattleUnit CreateUnit()
        {
            BattleUnit data = null;
            int id = 0;
            base.Create(out data, out id);
            if (data != null)
            {
                data.battleUnitID = id;
                //如果id为0，则设置为手动(暂定为玩家)
                if (BattleManager.Instance.manual)
                    data.manual = id == 0;
                else
                    data.manual = false;
                //初始设定100hp
                data.maxHp = 100;
                data.hp = data.maxHp;
                //随机攻击力
                data.atk = Random.Range(10, 15);
                //随机机动性
                data.mobility = 3;
            }

            return data;
        }
    }
}