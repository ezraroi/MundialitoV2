angular.module('mundialitoApp').factory('MundialitoUtils', [ 'Constants', function (Constants) {

    var Utils = {
        shouldRefreshInstance : (instance) => {
            if (!angular.isDefined(instance.LoadTime) || !angular.isDate(instance.LoadTime)) {
                return false;
            }
            var now = new Date().getTime();
            return ((now - instance.LoadTime.getTime()) > Constants.REFRESH_TIME);
        },
        shortName : (name) => {
            if (name.indexOf(' ') !== -1) {
                let temp = name.split(' ')
                return temp[0].substring(0, 1) + '.' + temp[1].substring(0, 1);
            }
            return name.substring(0, 1);
        },
        getGameMark: (res, teamId) => {
            if (res.HomeTeam.TeamId === teamId) {
                if (res.HomeScore > res.AwayScore) {
                    return { game : res.GameId, mark : "W" };
                } else if (res.HomeScore < res.AwayScore) {
                    return { game : res.GameId, mark : "L" };
                }
                return { game : res.GameId, mark : "D" };
            } else {
                if (res.HomeScore > res.AwayScore) {
                    return { game : res.GameId, mark : "L" };
                } else if (res.HomeScore < res.AwayScore) {
                    return { game : res.GameId, mark : "W" };
                }
                return { game : res.GameId, mark : "D" };
            } 
        }
    };

    return Utils;
}]);