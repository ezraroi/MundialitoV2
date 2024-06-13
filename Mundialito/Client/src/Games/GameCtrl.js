'use strict';
angular.module('mundialitoApp').controller('GameCtrl', ['$scope', '$log', 'Constants', 'UsersManager', 'GamesManager', 'BetsManager', 'game', 'userBet', 'Alert', '$location', 'PluginsProvider', 'keyValueEditorUtils', function ($scope, $log, Constants, UsersManager, GamesManager, BetsManager, game, userBet, Alert, $location, PluginsProvider, keyValueEditorUtils) {
    $scope.game = game;
    $scope.simulatedGame = {};
    $scope.plugins = {};
    $scope.userBet = userBet;
    $scope.userBet.GameId = game.GameId;
    $scope.showEditForm = false;
    $scope.toKeyValue = (object) => {
        return _.keys(object).map((key) => { return { 'name': key, 'value': object[key] } });
    };
    $scope.integrationsData = $scope.toKeyValue($scope.game.IntegrationsData);

    PluginsProvider.getGameDetailsFromAll($scope.game).then((results) => {
        results.forEach((result) => {
            $scope.plugins[result.property] = { data: result.data, template: result.template };
        });
    });

    if (!$scope.game.IsOpen) {
        BetsManager.getGameBets($scope.game.GameId).then((data) => {
            $log.debug("GameCtrl: get game bets" + angular.toJson(data));
            $scope.gameBets = data;

            var chart1 = {};
            chart1.type = "PieChart";
            chart1.options = {
                displayExactValues: true,
                is3D: true,
                backgroundColor: { fill: 'transparent' },
                chartArea: { left: 10, top: 20, bottom: 0, height: "100%" },
                title: 'Bets Distribution'
            };
            var mark1 = _.filter(data, function (bet) { return bet.HomeScore > bet.AwayScore; }).length;
            var markX = _.filter(data, function (bet) { return bet.HomeScore === bet.AwayScore; }).length;
            var mark2 = _.filter(data, function (bet) { return bet.HomeScore < bet.AwayScore; }).length;
            chart1.data = [
                ['Game Mark', 'Number Of Users'],
                ['1', mark1],
                ['X', markX],
                ['2', mark2]
            ];
            $scope.chart = chart1;
        });
    }

    $scope.updateGame = () => {
        if ((angular.isDefined(game.Stadium.Games)) && (game.Stadium.Games != null)) {
            delete game.Stadium.Games;
        }
        $scope.game.IntegrationsData = keyValueEditorUtils.mapEntries(keyValueEditorUtils.compactEntries($scope.integrationsData));
        $scope.game.update().then((res) => {
            Alert.success('Game was updated successfully');
            GamesManager.setGame(res.data);
        }).catch((err) => {
            Alert.error('Failed to update game, please try again');
            $log.error('Error updating game', err);
        });
    };

    $scope.updateBet = () => {
        if ($scope.userBet.BetId !== -1) {
            $scope.userBet.update().then((data) => {
                Alert.success('Bet was updated successfully');
                BetsManager.setBet(data);
            }).catch((err) => {
                Alert.error('Failed to update bet, please try again');
                $log.error('Error updating bet', err);
            });
        }
        else {
            BetsManager.addBet($scope.userBet).then((data) => {
                $log.log('GameCtrl: Bet ' + data.BetId + ' was added');
                $scope.userBet = data;
                Alert.success('Bet was added successfully');
            }, (err) => {
                Alert.error('Failed to add bet, please try again');
                $log.error('Error adding bet', err);
            });
        }
    };

    $scope.simulateGame = () => {
        $log.debug('GameCtrl: simulating game');
        GamesManager.simulateGame($scope.game.GameId, $scope.simulatedGame).then((data) => {
            $scope.users = data;
            Alert.success('Table updated with simulation result');
        }).catch((err) => {
            Alert.error('Failed to simulate game, please try again');
            $log.error('Error simulating game', err);
        });
    }

    $scope.sort = (column) => {
        $log.debug('GameCtrl: sorting by ' + column);
        $scope.gameBets = _.sortBy($scope.gameBets, (item) => {
            switch (column) {
                case 'points': return item.Points;
                case 'cards': return item.CardsMark;
                case 'corners': return item.CornersMark;
                case 'user': return item.User.FirstName + item.User.LastName;
                case 'result': return item.HomeScore + '-' + item.AwayScore;
            }
        });
    };

    $scope.gridOptions = {
        ...Constants.TABLE_GRID_OPTIONS, ...{
            data: 'users',
            onRegisterApi: (gridApi) => {
                $scope.gridApi = gridApi;
                $scope.gridApi.colResizable.on.columnSizeChanged($scope, saveState);
                $scope.gridApi.core.on.columnVisibilityChanged($scope, saveState);
                $scope.gridApi.core.on.sortChanged($scope, saveState);
            }
        }
    };
    $scope.getTableHeight = () => {
        var rowHeight = 30;
        var headerHeight = 30;
        var total = (($scope.users ? $scope.users.length : 0) * rowHeight + headerHeight);
        $log.debug('Total Height: ' + total);
        return {
            height: total + "px"
        };
    };
    $scope.goToUser = (rowItem) => {
        $location.path(rowItem.entity.getUrl());
    };
    function saveState() {
        var state = $scope.gridApi.saveState.save();
        localStorage.setItem('gridState', state);
    };
    $scope.getUserPlace = (user) => {
        return $scope.usersMap.get(user.Username).Place;
    }
    $scope.$watch('simulatedGame', () => { $scope.users = undefined }, true);
    UsersManager.getTable().then((users) => {
        $scope.usersMap = new Map();
        users.forEach((obj) => {
            $scope.usersMap.set(obj.Username, obj);
        });
        let followeesUsers = _.chain(users).filter((user) => $scope.security.user.Followees.includes(user.Username)).pluck('Username').value();
        $scope.followeesBets = _.filter($scope.gameBets, (bet) => followeesUsers.includes(bet.User.Username));
        let topUsers = _.chain(users).first(3).pluck('Username').value();
        $scope.top3UsersBets = _.filter($scope.gameBets, (bet) => topUsers.includes(bet.User.Username));
        let myPlace = 0;
        users.forEach((user, place) => {
            if (user.Username === $scope.security.user.Username) {
                myPlace = place;
            }
        });
        let lowIndex = Math.max(myPlace - 3, 0);
        let upperIndex = Math.min(myPlace + 3, users.length);
        let neighbors = _.chain(users.slice(lowIndex, upperIndex + 1))
            .pluck('Username').filter((user) => user !== $scope.security.user.Username).value();
        $scope.neighborsBets = _.filter($scope.gameBets, (bet) => neighbors.includes(bet.User.Username));
    });
}]);