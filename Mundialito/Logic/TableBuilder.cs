using Mundialito.Models;
using Mundialito.DAL.Bets;
using Mundialito.DAL.GeneralBets;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.Teams;
using Mundialito.DAL.Players;

namespace Mundialito.Logic;

public class TableBuilder
{
    private readonly TournamentTimesUtils _tournamentTimesUtils;
    private readonly IDateTimeProvider _dateTimeProvider;
    
    public TableBuilder(IDateTimeProvider dateTimeProvider, TournamentTimesUtils tournamentTimesUtils)
    {
        _dateTimeProvider = dateTimeProvider;
        _tournamentTimesUtils = tournamentTimesUtils;
    }

    public IEnumerable<UserWithPointsModel> GetTable(IEnumerable<UserWithPointsModel> allUsers, IEnumerable<Bet> bets, IEnumerable<GeneralBet> generalBets)
    {
        var users = allUsers.ToDictionary(user => user.Id, user => user);
        var yesterdayPlaces = new Dictionary<string, int>(users.Count);
        bets.Where(bet => users.ContainsKey(bet.User.Id)).Where(bet => !bet.IsOpenForBetting(_dateTimeProvider.UTCNow)).ToList().ForEach(bet => users[bet.User.Id].AddBet(new BetViewModel(bet, _dateTimeProvider.UTCNow)));
        bets.Where(bet => users.ContainsKey(bet.User.Id)).Where(bet => !bet.IsOpenForBetting(_dateTimeProvider.UTCNow)).Where(bet => bet.Game.Date < _dateTimeProvider.UTCNow.Subtract(TimeSpan.FromDays(1))).ToList().ForEach(bet => users[bet.User.Id].YesterdayPoints += bet.Points.HasValue ? bet.Points.Value : 0);
        if (_dateTimeProvider.UTCNow >= _tournamentTimesUtils.GetGeneralBetsCloseTime())
        {
            generalBets.ToList().ForEach(generalBet =>
                    users[generalBet.User.Id].SetGeneralBet(new
                    GeneralBetViewModel(generalBet, _tournamentTimesUtils.GetGeneralBetsCloseTime())));

        }
        var res = users.Values.ToList().OrderByDescending(user => user.YesterdayPoints).ToList();
        for (int i = 0; i < res.Count; i++)
        {
            yesterdayPlaces.Add(res[i].Id, i + 1);
        }
        res = res.OrderByDescending(user => user.Points).ToList();
        for (int i = 0; i < res.Count; i++)
        {
            res[i].Place = (i + 1).ToString();
            var diff = yesterdayPlaces[res[i].Id] - (i + 1);
            res[i].PlaceDiff = string.Format("{0}{1}", diff > 0 ? "+" : string.Empty, diff);
        }
        return res;
    }

}