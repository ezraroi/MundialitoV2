'use strict';
angular.module('mundialitoApp').factory('BetsManager', ['$http', '$q', 'Bet', '$log', 'MundialitoUtils', 'GamesManager', function ($http, $q, Bet, $log, MundialitoUtils, GamesManager) {
    var betsManager = {
        _pool: {},
        _retrieveInstance: function(betId, betData) {
            var instance = this._pool[betId];

            if (instance) {
                $log.debug('BetsManager: updating existing instance of bet ' + betId);
                instance.setData(betData);
            } else {
                $log.debug('BetsManager: saving new instance of bet ' + betId);
                instance = new Bet(betData);
                this._pool[betId] = instance;
            }
            instance.LoadTime = new Date();
            return instance;
        },
        _search: function(betId) {
            $log.debug('BetsManager: will fetch bet ' + betId + ' from local pool');
            var instance = this._pool[betId];
            if (angular.isDefined(instance) && MundialitoUtils.shouldRefreshInstance(instance)) {
                $log.debug('BetsManager: Instance was loaded at ' + instance.LoadTime + ', will reload it from server');
                return undefined;
            }
            return instance;
        },
        _load: function(betId, deferred) {
            var scope = this;
            $log.debug('BetsManager: will fetch bet ' + betId + ' from server');
            $http.get('api/bets/' + betId, { tracker: 'getBet' })
                .then((betData) => {
                    var bet = scope._retrieveInstance(betData.data.BetId, betData.data);
                    deferred.resolve(bet);
                })
                .catch(() => {
                    deferred.reject();
                });
        },

        /* Public Methods */
        /* The only bet write path. One idempotent PUT whether or not a bet exists yet, so
           callers never branch and a double tap is two identical writes. Always resolves a
           pooled Bet instance, never the raw $http envelope. */
        saveBet: function(gameId, betData) {
            var scope = this;
            $log.debug('BetsManager: will save bet on game ' + gameId);
            /* Only the four fields the server accepts: the game comes from the URL and the
               owner from the token. */
            var body = {
                HomeScore: betData.HomeScore,
                AwayScore: betData.AwayScore,
                CardsMark: betData.CardsMark,
                CornersMark: betData.CornersMark
            };
            return $http.put('api/games/' + gameId + '/mybet', body, { tracker: 'saveBet' })
                .then((res) => scope._retrieveInstance(res.data.BetId, res.data));
        },

        /* Use this function in order to get a bet instance by it's id */
        getBet: function(betId,fresh) {
            var deferred = $q.defer();
            var bet = undefined;
            if ((!angular.isDefined(fresh) || (!fresh))) {
                bet = this._search(betId);
            }
            if (bet) {
                deferred.resolve(bet);
            } else {
                this._load(betId, deferred);
            }
            return deferred.promise;
        },

        /* Use this function in order to get instances of all the game bets */
        getGameBets: function(gameId) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('BetsManager: will fetch all bets of game ' + gameId + ' from server');
            $http.get('api/games/' + gameId + '/bets', { tracker: 'getGameBets' })
                .then((betsArray) => {
                    var bets = [];
                    betsArray.data.forEach((betData) => {
                        var bet = scope._retrieveInstance(betData.BetId, betData);
                        bets.push(bet);
                    });

                    deferred.resolve(bets);
                })
                .catch(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        getUserBets : function(username) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('BetsManager: will fetch user ' + username +' bets from server');
            $http.get('api/bets/user/' + username, { tracker: 'getUserBets' })
                .then((betsArray) => {
                    var bets = [];
                    betsArray.data.forEach((betData) => {
                        var bet = scope._retrieveInstance(betData.BetId, betData);
                        bets.push(bet);
                    });

                    deferred.resolve(bets);
                })
                .catch(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        getUserBetOnGame : function(gameId) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('BetsManager: will fetch user bet of game ' + gameId + ' from server');
            $http.get('api/games/' + gameId + '/mybet', { tracker: 'getUserBetOnGame' })
                .then((betData) => {
                    /* The no-bet placeholder is deliberately not pooled: it has no BetId,
                       so every un-bet game would share one slot. */
                    if (betData.data.HasBet) {
                        deferred.resolve(scope._retrieveInstance(betData.data.BetId, betData.data));
                    } else {
                        deferred.resolve(betData.data);
                    }
                })
                .catch(() => {
                    deferred.reject();
                });
            return deferred.promise;
        }

    };
    return betsManager;
}]);
