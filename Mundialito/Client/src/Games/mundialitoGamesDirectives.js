'use strict';
angular.module('mundialitoApp').directive('mundialitoGames', ['Alert', function (Alert) {
    return {
        restrict: 'E',
        scope: {
            games: '=info',
            gamesType: '=filter',
            showOnly: '=',
            onAdd: '&'
        },
        templateUrl: 'App/Games/gamesTemplate.html',
        link: ($scope) => {
            $scope.allGames = $scope.games;
            $scope.$watch('gamesType', function(newValue) {
                if ((newValue) && (newValue !== "All")) {
                    $scope.games = $scope.games.filter((game) => {
                        return game.IsOpen;
                    });
                } else {
                    $scope.games = $scope.allGames;
                }
            });
            $scope.deleteGame = (game) => {
                var scope = game;
                game.delete().then(() => {
                    Alert.success('Game was deleted successfully');
                    $scope.games.splice($scope.games.indexOf(scope), 1);
                });
            };
        }
    };
}]);
