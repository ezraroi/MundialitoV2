﻿'use strict';
angular.module('mundialitoApp').controller('GamesCtrl', ['$scope','$log','GamesManager','games','teams', 'StadiumsManager' ,'Alert',function ($scope,$log, GamesManager, games, teams, StadiumsManager, Alert) {
    $scope.newGame = null;
    $scope.gamesFilter = "Open";
    $scope.gamesToggle = false;
    $scope.games = games;
    $scope.teams = teams;
    $scope.changed = () => {
        if ($scope.gamesFilter === "Open") {
            $scope.gamesFilter = "All"
        } else {
            $scope.gamesFilter = "Open"
        }
    }

    StadiumsManager.loadAllStadiums().then(function (res) {
        $scope.stadiums = res;
    });

    $scope.addNewGame = function () {
        $('.selectpicker').selectpicker('refresh');
        $scope.newGame = GamesManager.getEmptyGameObject();
    };

    $scope.saveNewGame = function() {
        $scope.addGamePromise = GamesManager.addGame($scope.newGame).then((data) => {
            Alert.success('Game was added successfully');
            $scope.newGame = GamesManager.getEmptyGameObject();
            $scope.games.push(data);
        });
    };

    $scope.isPendingUpdate = function(item) {
        return item.IsPendingUpdate;
    };

    $scope.updateGame = function(game) {
        if  ((angular.isDefined(game.Stadium.Games)) && (game.Stadium.Games != null)) {
            delete game.Stadium.Games;
        }
        $scope.editGamePromise = game.update().then((data) => {
            Alert.success('Game was updated successfully');
            GamesManager.setGame(data);
        });
    };
}]);