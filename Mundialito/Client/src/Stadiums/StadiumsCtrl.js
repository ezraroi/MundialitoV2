'use strict';
angular.module('mundialitoApp').controller('StadiumsCtrl', ['$scope', '$log', 'StadiumsManager', 'stadiums', 'Alert', function ($scope, $log, StadiumsManager, stadiums, Alert) {
    $scope.stadiums = stadiums;
    $scope.showNewStadium = false;
    $scope.newStadium = null;

    $scope.addNewStadium = () => {
        $scope.newStadium = StadiumsManager.getEmptyStadiumObject();
    };

    $scope.saveNewStadium = () => {
        StadiumsManager.addStadium($scope.newStadium).then((data) => {
            Alert.success('Stadium was added successfully');
            $scope.newStadium = null;
            $scope.stadiums.push(data);
        });
    };

    $scope.deleteStadium = (stadium) => {
        var scope = stadium;
        stadium.delete().then(() => {
            Alert.success('Stadium was deleted successfully');
            $scope.stadiums.splice($scope.stadiums.indexOf(scope), 1);
        });
    };

    $scope.schema = StadiumsManager.getStaidumSchema();
}]);