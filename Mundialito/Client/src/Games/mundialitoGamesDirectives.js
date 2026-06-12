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
            $scope.$watch('gamesType', function(newValue) {
                if ((newValue) && (newValue !== "All")) {
                    $scope.displayedGames = $scope.games.filter((game) => {
                        return game.IsOpen;
                    });
                } else {
                    $scope.displayedGames = $scope.games;
                }
            });
            $scope.deleteGame = (game) => {
                var scope = game;
                game.delete().then(() => {
                    Alert.success('Game was deleted successfully');
                    $scope.games.splice($scope.games.indexOf(scope), 1);
                    var displayedIdx = $scope.displayedGames.indexOf(scope);
                    if (displayedIdx !== -1) $scope.displayedGames.splice(displayedIdx, 1);
                });
            };
        }
    };
}]);
