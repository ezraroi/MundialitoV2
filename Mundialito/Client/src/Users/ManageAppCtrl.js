'use strict';
angular.module('mundialitoApp').controller('ManageAppCtrl', ['$scope', '$log', 'Alert', 'users','teams', 'generalBets','UsersManager', 'players', function ($scope, $log, Alert, users, teams, generalBets, UsersManager, players) {
    $scope.users = users;
    $scope.generalBets = generalBets;
    $scope.deleteUser = (user) => {
        var scope = user;
        $scope.deleteUserPromise = user.delete().then(() => {
            Alert.success('User was deleted successfully');
            $scope.users.splice($scope.users.indexOf(scope), 1);
        });
    };

    $scope.resolveBet = (bet) => {
        $scope.resolveGeneralBetPromise = bet.resolve().then(() => {
            Alert.success('General bet was resolved successfully');
        });
    };

    $scope.makeAdmin = (user) => {
        user.makeAdmin().then(() => {
            Alert.success('User was is now admin');
            user.IsAdmin = true;
        });
    };

    $scope.activate = (user) => {
        user.activate().then(() => {
            Alert.success('User was activated successfully');
        });
    };

    $scope.deactivate = (user) => {
        user.deactivate().then(() => {
            Alert.success('User was deactivated successfully');
        });
    };
}]);