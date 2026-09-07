'use strict';
angular.module('mundialitoApp').controller('BetsCenterCtrl', ['$scope', '$log', '$timeout', '$interval', 'Alert', 'BetsManager', 'games', function ($scope, $log, $timeout, $interval, Alert, BetsManager, games) {
    $scope.games = games;
    $scope.bets = {};
    /* Keyed per game: a single flag would disable every row on one save. */
    $scope.savingBets = {};
    /* Same for the deadline. These rows are the games that were open when the page loaded,
       and nothing re-evaluates that, so a row whose kickoff passes keeps a live Save button
       - the Game page had the same problem. Rows close themselves; the server stays the
       authority, and a row that closes here is refused there too. */
    $scope.closedGames = {};

    var deadlineTick = $interval(() => {
        var now = Date.now();
        var changed = false;
        games.forEach((game) => {
            var closeTime = new Date(game.CloseTime).getTime();
            var closed = !isNaN(closeTime) && closeTime <= now;
            if (closed !== !!$scope.closedGames[game.GameId]) {
                $scope.closedGames[game.GameId] = closed;
                changed = true;
            }
        });
        /* invokeApply is false below, so ask for a digest only when something actually moved
           - otherwise thirty-odd rows get re-evaluated every second for nothing. */
        if (changed) {
            $scope.$evalAsync(angular.noop);
        }
    }, 1000, 0, false);
    $scope.$on('$destroy', () => $interval.cancel(deadlineTick));


    var loadUserBets = function() {
        if (!angular.isDefined($scope.security.user) || ($scope.security.user == null))
        {
            $log.debug('BetsCenterCtrl: user info not loaded yet, will retry in 1 second');
            $timeout(loadUserBets,1000);
        }
        else {
            $scope.getUserBetsPromise = BetsManager.getUserBets($scope.security.user.Username).then((bets) => {
                for (var i = 0; i < bets.length; i++) {
                    $scope.bets[bets[i].Game.GameId] = bets[i];
                }

                for (var j = 0; j < games.length; j++) {
                    if (!angular.isDefined($scope.bets[games[j].GameId])) {
                        $log.debug('BetsCenterCtrl: game ' + games[j].GameId + ' has not bet');
                        $scope.bets[games[j].GameId] = { HasBet: false };
                    }
                }
            });
        }
    };

    loadUserBets();

    $scope.updateBet = function(gameId) {
        if ($scope.savingBets[gameId] || $scope.closedGames[gameId]) {
            return;
        }
        $log.debug('BetsCenterCtrl: Will save bet on game ' + gameId);
        $scope.savingBets[gameId] = true;
        BetsManager.saveBet(gameId, $scope.bets[gameId]).then((bet) => {
            $scope.bets[gameId] = bet;
            Alert.success('Bet was saved successfully');
        }).catch((err) => {
            /* The http interceptor owns the user-facing message. */
            $log.error('Error saving bet', err);
        }).finally(() => {
            $scope.savingBets[gameId] = false;
        });
    };
    $scope.shuffleBet = function(gameId) {
        var homeGoals, awayGoals;
	    var toto = ['1', 'X', '2'];
	    var goals = [0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 3, 3, 4, 5];
	    var gameMark = toto[Math.floor((Math.random() * 3))];
	    do {
	        homeGoals = goals[Math.floor((Math.random() * goals.length))];
	        awayGoals = goals[Math.floor((Math.random() * goals.length))];
	    } while (gameMark !== 'X' && homeGoals === awayGoals);
	    $log.debug('Random game mark is ' + gameMark);
	    if (gameMark === 'X') {
	        awayGoals = homeGoals;
	    }
	    $log.debug('Home goals: ' + homeGoals);
	    $log.debug('Away goals: ' + awayGoals);
	    $scope.bets[gameId].HomeScore = homeGoals;
	    $scope.bets[gameId].AwayScore = awayGoals
        $scope.bets[gameId].CardsMark = toto[Math.floor((Math.random() * 3))];
		$scope.bets[gameId].CornersMark = toto[Math.floor((Math.random() * 3))];
    };
}]);
