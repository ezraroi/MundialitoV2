
namespace Mundialito;

public class TournamentTimesUtils
{
    private static DateTime generalBetsCloseTime = DateTime.MinValue;
    private static DateTime generalBetsResolveTime = DateTime.MinValue;


    public static DateTime GeneralBetsCloseTime 
    {
        get
        {
            if (generalBetsCloseTime == DateTime.MinValue)
            {
                // if (String.IsNullOrEmpty(WebConfigurationManager.AppSettings["TournamentStartDate"]))
                if (String.IsNullOrEmpty("1/06/2024"))
                {
                    generalBetsCloseTime = new DateTime(2014, 6, 12).ToUniversalTime();
                }
                else
                {
                    // generalBetsCloseTime = DateTime.ParseExact(WebConfigurationManager.AppSettings["TournamentStartDate"], "dd/MM/yyyy", null).Subtract(TimeSpan.FromDays(1)).ToUniversalTime();
                    generalBetsCloseTime = DateTime.ParseExact("1/06/2024", "dd/MM/yyyy", null).Subtract(TimeSpan.FromDays(1)).ToUniversalTime();
                }
                
            }
            return generalBetsCloseTime;
        }   
    }

    public static DateTime GeneralBetsResolveTime
    {
        get
        {
            if (generalBetsResolveTime == DateTime.MinValue)
            {
                if (String.IsNullOrEmpty("10/07/2024"))
                {
                    generalBetsResolveTime = new DateTime(2014, 7, 13).ToUniversalTime();
                }
                else
                {
                    generalBetsResolveTime = DateTime.ParseExact("10/07/2024", "dd/MM/yyyy", null).ToUniversalTime();
                }
            }
            return generalBetsResolveTime;
        }
    }
}


