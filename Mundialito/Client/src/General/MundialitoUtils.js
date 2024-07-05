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
            let temp = name.split(' ')
            return temp[0].substring(0, 1) + '.' + temp[1].substring(0, 1);
        }
    };

    return Utils;
}]);