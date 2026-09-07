'use strict';
angular.module('mundialitoApp').controller('ManageAppCtrl', ['$scope', '$log', 'Alert', 'managePage', 'UsersManager', 'PlayersManager', function ($scope, $log, Alert, managePage, UsersManager, PlayersManager) {
    $scope.users = managePage.users;
    $scope.generalBets = managePage.generalBets;
    $scope.players = managePage.players;
    $scope.newPlayerName = '';
    $scope.deleteUser = (user) => {
        var scope = user;
        $scope.deleteUserPromise = user.delete().then(() => {
            Alert.success('User was deleted successfully');
            $scope.users.splice($scope.users.indexOf(scope), 1);
        });
    };

    /* Both of these mutate the array the route resolve handed over. PlayersManager memoizes
       its promise and $http caches api/players on top of that, so the array itself is what
       every other view will keep seeing - swapping it out would leave them stale. */
    $scope.addPlayer = () => {
        var name = ($scope.newPlayerName || '').trim();
        if (!name) {
            return;
        }
        $scope.playersPromise = PlayersManager.addPlayer({ Name: name }).then((player) => {
            Alert.success('Player was added successfully');
            $scope.newPlayerName = '';
            $scope.players.push(player);
        });
    };

    $scope.deletePlayer = (player) => {
        var promise = player.delete();
        /* delete() returns nothing when the admin dismisses the confirm. */
        if (!promise) {
            return;
        }
        $scope.playersPromise = promise.then(() => {
            Alert.success('Player was deleted successfully');
            $scope.players.splice($scope.players.indexOf(player), 1);
        });
    };

    $scope.resolveBet = (bet) => {
        $scope.resolveGeneralBetPromise = bet.resolve().then(() => {
            Alert.success('General bet was resolved successfully');
        });
    };

    $scope.makeAdmin = (user) => {
        user.makeAdmin().then(() => {
            Alert.success('User is now admin');
            user.Roles = "Admin";
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
