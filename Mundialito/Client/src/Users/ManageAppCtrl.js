'use strict';
angular.module('mundialitoApp').controller('ManageAppCtrl', ['$scope', '$log', 'Alert', 'users','teams', 'generalBets','UsersManager', 'players', function ($scope, $log, Alert, users, teams, generalBets, UsersManager, players) {
    $scope.users = users;
    $scope.generalBets = generalBets;
    $scope.privateKey = {};
    $scope.teamsDic = {};
    $scope.playersDic = {};

    for(var i=0; i<teams.length; i++) {
        $scope.teamsDic[teams[i].TeamId] = teams[i];
    }

    for (var i = 0; i < players.length; i++) {
        $scope.playersDic[players[i].PlayerId] = players[i];
    }

    $scope.deleteUser = (user) => {
        var scope = user;
        $scope.deleteUserPromise = user.delete().then(() => {
            Alert.success('User was deleted successfully');
            $scope.users.splice($scope.users.indexOf(scope), 1);
        });
    };

    $scope.resolveBet = function(bet) {
        $scope.resolveGeneralBetPromise = bet.resolve().then(() => {
            Alert.success('General bet was resolved successfully');
        });
    };

    $scope.generateKey = function() {
        $scope.privateKey.key = '';
        $scope.generatePrivateKeyPromisd = UsersManager.generatePrivateKey($scope.privateKey.email).then(function(data) {
            $log.debug('ManageAppCtrl: got private key ' + data);
            $scope.privateKey.key = data;
        });
    };

    $scope.makeAdmin = function(user) {
        user.makeAdmin().then(() => {
            Alert.success('User was is now admin');
            user.IsAdmin = true;
        })
    };

}]);