using Mundialito.DAL.GeneralBets;

namespace Mundialito.Logic;

public class GeneralBetsService {

    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IGeneralBetsRepository _generalBetsRepository;
    private readonly TournamentTimesUtils _tournamentTimesUtils;

    public GeneralBetsService(IDateTimeProvider dateTimeProvider, IGeneralBetsRepository generalBetsRepository, TournamentTimesUtils tournamentTimesUtils) {
        _dateTimeProvider = dateTimeProvider;
        _generalBetsRepository = generalBetsRepository;
        _tournamentTimesUtils = tournamentTimesUtils;
    }

    public IEnumerable<GeneralBet> GetGeneralBets() 
    {
        if (_dateTimeProvider.UTCNow >= _tournamentTimesUtils.GetGeneralBetsCloseTime())
            return _generalBetsRepository.GetGeneralBets();
        return new List<GeneralBet>();
    }

    public GeneralBet? GetUserGeneralBet(string username)
    {
        if (_dateTimeProvider.UTCNow >= _tournamentTimesUtils.GetGeneralBetsCloseTime())
            return _generalBetsRepository.GetUserGeneralBet(username);
        return null;
    }
}