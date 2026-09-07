'use strict';
angular.module('mundialitoApp').factory('PlayersManager', ['$http', '$q', 'Player', '$log', function ($http, $q, Player, $log) {
    var playersPromise = undefined;
    var playersManager = {
        _pool: {},
        _retrieveInstance: function (playerId, playerData) {
            var instance = this._pool[playerId];

            if (instance) {
                $log.debug('playersPromise: updating existing instance of player ' + playerId);
                instance.setData(playerData);
            } else {
                $log.debug('playersPromise: saving new instance of player ' + playerId);
                instance = new Player(playerData);
                this._pool[playerId] = instance;
            }
            instance.LoadTime = new Date();
            return instance;
        },

        /* Public Methods */

        getPlayerSchema: function () {
            return [
                { property: 'Name', label: 'Name', type: 'text', attr: { required: true } }
            ];
        },

        /* Use this function in order to add a new player. The caller is expected to push the
           result onto the array it already holds - both this factory's playersPromise and
           $http's own cache keep serving that same array, so replacing it is not an option. */
        addPlayer: function (playerData) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('PlayersManager: will add new player - ' + angular.toJson(playerData));
            $http.post('api/players', playerData, { tracker: 'addPlayer' }).then((res) => {
                var player = scope._retrieveInstance(res.data.PlayerId, res.data);
                deferred.resolve(player);
            }).catch((e) => {
                deferred.reject(e);
            });
            return deferred.promise;
        },

        /* Use this function in order to get instances of all the players */
        loadAllPlayers: function () {
            if (playersPromise) {
                return playersPromise;
            }
            var deferred = $q.defer();
            var scope = this;
            $log.debug('PlayersManager: will fetch all players from server');
            $http.get("api/players", { tracker: 'getPlayers', cache: true })
                .then((playersArray) => {
                    var players = [];
                    playersArray.data.forEach((playerData) => {
                        var player = scope._retrieveInstance(playerData.PlayerId, playerData);
                        players.push(player);
                    });
                    deferred.resolve(players);
                })
                .catch((e) => {
                    deferred.reject(e);
                });
            playersPromise = deferred.promise;
            return deferred.promise;
        },

    };
    return playersManager;
}]);
