'use strict';
angular.module('mundialitoApp').factory('Player', ['$http', '$log', function ($http, $log) {
    function Player(playerData) {
        if (playerData) {
            this.setData(playerData);
        }
        // Some other initializations related to stadium
    };

    Player.prototype = {
        setData: function (playerData) {
            angular.extend(this, playerData);
        },
        delete: function () {
            if (confirm('Are you sure you would like to delete player ' + this.Name + '?')) {
                $log.debug('Player: Will delete player ' + this.PlayerId);
                return $http.delete('api/players/' + this.PlayerId, { tracker: 'deletePlayer' });
            }
        }
    };
    return Player;
}]);
