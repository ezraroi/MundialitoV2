
using Microsoft.Extensions.Options;
using Mundialito.Configuration;

namespace Mundialito.Logic;

public class TournamentTimesUtils
{
     private readonly Config config;

    public TournamentTimesUtils(IOptions<Config> config)
    {
        this.config = config.Value;
    }

    public DateTime GetGeneralBetsCloseTime()
    {
        if (string.IsNullOrEmpty(config.TournamentStartDate))
        {
            return new DateTime(2024, 6, 14).ToUniversalTime();
        }
        else
        {
            return DateTime.ParseExact(config.TournamentStartDate, "dd/MM/yyyy H:mm", null).ToUniversalTime();
        }
    }

    public DateTime GetGeneralBetsResolveTime()
    {
        if (string.IsNullOrEmpty(config.TournamentEndDate))
        {
            return new DateTime(2024, 7, 14).ToUniversalTime();
        }
        else
        {
            return DateTime.ParseExact(config.TournamentEndDate, "dd/MM/yyyy H:mm", null).ToUniversalTime();
        }
    }

}


