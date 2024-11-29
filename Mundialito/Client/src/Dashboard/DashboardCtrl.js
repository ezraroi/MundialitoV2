'use strict';
angular.module('mundialitoApp').controller('DashboardCtrl', ['$scope', '$log', 'Constants', '$location', '$timeout', 'GamesManager', 'UsersManager', 'GeneralBetsManager', 'teams', 'players', 'BetsManager', 'MundialitoUtils',
    function ($scope, $log, Constants, $location, $timeout, GamesManager, UsersManager, GeneralBetsManager, teams, players, BetsManager, MundialitoUtils) {
        $scope.generalBetsAreOpen = false;
        $scope.submittedGeneralBet = true;
        $scope.pendingUpdateGames = false;
        $scope.oneAtATime = true;
        $scope.status = {};
        $scope.toggleValue = {};
        $scope.players = players;

        // Function to group teams by ShortName
        function groupTeamsByName(teams) {
            return teams.reduce((acc, team) => {
                const name = team.Name;
                if (!acc[name]) {
                    acc[name] = [];
                }
                acc[name].push(team);
                return acc;
            }, {});
        }
        $scope.groupedTeams = groupTeamsByName(teams);

        $scope.changed = (game) => {
            if ($scope.toggleValue[game.GameId]) {
                $scope.selectedDic[game.GameId] = $scope.marksDic[game.GameId];
                $scope.selectedPercentage[game.GameId] = $scope.marksPercentage[game.GameId];
            } else {
                $scope.selectedDic[game.GameId] = $scope.resultsDic[game.GameId];
                $scope.selectedPercentage[game.GameId] = $scope.resultsPercentage[game.GameId];
            }
        }

        $scope.getGamesPromise = GamesManager.loadAllGames().then((games) => {
            $scope.games = games;
            $scope.resultsDic = {};
            $scope.marksDic = {};
            $scope.selectedDic = {};
            $scope.resultsPercentage = {};
            $scope.marksPercentage = {};
            $scope.selectedPercentage = {};
            $scope.pendingUpdateGames = _.findWhere($scope.games, { IsPendingUpdate: true }) !== undefined;
            $scope.pendingUpdateGamesFolloweesBets = {};
            $log.info('DashboardCtrl: followees:' + $scope.security.user.Followees);
            _.filter($scope.games, (game) => game.IsPendingUpdate).forEach(game => {
                BetsManager.getGameBets(game.GameId).then((data) => {
                    let resulsGrouped = _.groupBy(data, (bet) => { return bet.HomeScore + "-" + bet.AwayScore });
                    let marksGrouped = _.groupBy(data, (bet) => {
                        if (bet.HomeScore === bet.AwayScore) {
                            return 'X';
                        }
                        if (bet.HomeScore > bet.AwayScore) {
                            return bet.Game.HomeTeam.Name;
                        }
                        return bet.Game.AwayTeam.Name;
                    });
                    $scope.resultsDic[game.GameId] = Object.entries(resulsGrouped).sort((a, b) => b[1].length - a[1].length);
                    $scope.marksDic[game.GameId] = Object.entries(marksGrouped).sort((a, b) => b[1].length - a[1].length);
                    $scope.resultsPercentage[game.GameId] = {}
                    $scope.resultsDic[game.GameId].forEach(resItem => {
                        $scope.resultsPercentage[game.GameId][resItem[0]] = Math.round((resItem[1].length / data.length) * 100);
                    });
                    $scope.marksPercentage[game.GameId] = {}
                    $scope.marksDic[game.GameId].forEach(markItem => {
                        $scope.marksPercentage[game.GameId][markItem[0]] = Math.round((markItem[1].length / data.length) * 100);
                    });
                    let followeesBets = _.filter(data, (bet) => {
                        return $scope.security.user.Followees.includes(bet.User.Username) || $scope.security.user.Username === bet.User.Username;
                    });
                    $scope.pendingUpdateGamesFolloweesBets[game.GameId] = followeesBets;
                    $scope.changed(game);
                });
            });
        });

        var userHasGeneralBet = () => {
            if (!angular.isDefined($scope.security.user) || ($scope.security.user == null)) {
                $log.debug('DashboardCtrl: user info not loaded yet, will retry in 1 second');
                $timeout(userHasGeneralBet, 1000);
            }
            else {
                GeneralBetsManager.hasGeneralBet($scope.security.user.Username).then((data) => {
                    $scope.submittedGeneralBet = data === true;
                });
            }
        };
        userHasGeneralBet();
        GeneralBetsManager.canSubmtiGeneralBet().then((data) => {
            $scope.generalBetsAreOpen = (data === true);
            if (!$scope.generalBetsAreOpen) {
                GeneralBetsManager.loadAllGeneralBets().then(function (data) {
                    $scope.generalBets = data;
                    $scope.winningTeams = {};
                    $scope.winningPlayers = {};
                    for (var i = 0; i < $scope.generalBets.length; i++) {
                        if (!angular.isDefined($scope.winningTeams[$scope.generalBets[i].WinningTeam.Name])) {
                            $scope.winningTeams[$scope.generalBets[i].WinningTeam.Name] = 0;
                        }
                        $scope.winningTeams[$scope.generalBets[i].WinningTeam.Name] += 1;
                        if (!angular.isDefined($scope.winningPlayers[$scope.generalBets[i].GoldenBootPlayer.Name])) {
                            $scope.winningPlayers[$scope.generalBets[i].GoldenBootPlayer.Name] = 0;
                        }
                        $scope.winningPlayers[$scope.generalBets[i].GoldenBootPlayer.Name] += 1;
                    }

                    var chart1 = {};
                    chart1.type = "PieChart";
                    chart1.options = {
                        displayExactValues: true,
                        is3D: true,
                        backgroundColor: { fill: 'transparent' },
                        chartArea: { left: 10, top: 20, bottom: 0, height: "100%" },
                        title: 'Winning Team Bets Distribution'
                    };
                    chart1.data = [
                        ['Team', 'Number Of Users']
                    ];
                    for (var team in $scope.winningTeams) {
                        chart1.data.push([team, $scope.winningTeams[team]]);
                    }
                    $scope.chart = chart1;
                    chart1 = {};
                    chart1.type = "PieChart";
                    chart1.options = {
                        displayExactValues: true,
                        is3D: true,
                        backgroundColor: { fill: 'transparent' },
                        chartArea: { left: 10, top: 20, bottom: 0, height: "100%" },
                        title: 'Winning Golden Boot Player Bets Distribution'
                    };
                    chart1.data = [
                        ['Player', 'Number Of Users']
                    ];
                    for (var player in $scope.winningPlayers) {
                        chart1.data.push([player, $scope.winningPlayers[player]]);
                    }
                    $scope.playersChart = chart1;
                });
            }
        });
        $scope.getUsersPromise = UsersManager.loadAllUsers().then((users) => {
            $scope.users = users;
            $scope.usersDic = users.reduce((acc, item) => {
                acc[item.Id] = item;
                return acc;
            }, {});
        });
        $scope.isOpenForBetting = (item) => item.IsOpen;
        $scope.isPendingUpdate = (item) => item.IsPendingUpdate;
        $scope.isDecided = function (item) {
            return !item.IsOpen && !item.IsPendingUpdate;
        };
        $scope.isGameBet = (game) => (item) => item.Game.GameId === game.GameId;
        $scope.hasBets = (game) => $scope.pendingUpdateGamesFolloweesBets[game.GameId] !== undefined && $scope.pendingUpdateGamesFolloweesBets[game.GameId].length > 0
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

        function saveState() {
            var state = $scope.gridApi.saveState.save();
            localStorage.setItem('gridState', state);
        };

        function restoreState() {
            $timeout(() => {
                var state = localStorage.getItem('gridState');
                if (state) $scope.gridApi.saveState.restore($scope, state);
            });
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
    }]);
