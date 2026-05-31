'use strict';
angular.module('mundialitoApp').controller('StadiumCtrl', ['$scope', '$log', 'StadiumsManager', 'GamesManager', 'stadium', 'Alert', function ($scope, $log, StadiumsManager, GamesManager, stadium, Alert) {
    $scope.stadium = stadium;
    $scope.showEditForm = false;
    $scope.toggleEditForm = function () {
        $scope.showEditForm = !$scope.showEditForm;
    };

    $scope.getStadiumGamesPromise = GamesManager.getStadiumGames($scope.stadium.StadiumId).then((data) => {
        $log.debug('StadiumCtrl: Got games of stadium');
        $scope.games = data;
    });

    $scope.updateStadium = () => {
        $scope.editStadiumPromise = $scope.stadium.update().then(() => {
            Alert.success('Stadium was updated successfully');
        });
    };

    $scope.schema =  StadiumsManager.getStaidumSchema();
}]);