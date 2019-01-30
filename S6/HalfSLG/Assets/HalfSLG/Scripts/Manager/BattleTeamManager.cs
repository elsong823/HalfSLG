
namespace ELGame
{
    public class BattleTeamManager
        : ELSingletonDic<BattleTeamManager, BattleTeam>, IGameBase
    {
        public string Desc()
        {
            return string.Empty;
        }

        public void Init(params object[] args)
        {
            UtilityHelper.Log("Battle team manager inited.");
        }

        public BattleTeam CreateBattleTeam()
        {
            BattleTeam data = null;
            int id = 0;
            base.Create(out data, out id);
            if (data != null)
            {
                data.teamID = id;
            }

            return data;
        }
    }
}