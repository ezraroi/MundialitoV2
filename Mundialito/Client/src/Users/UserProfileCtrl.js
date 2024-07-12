'use strict';
angular.module('mundialitoApp').controller('UserProfileCtrl', ['$scope', '$log', 'UsersManager', 'Alert', 'GeneralBetsManager', 'profileUser', 'userGameBets', 'teams', 'generalBetsAreOpen', 'players', function ($scope, $log, UsersManager, Alert, GeneralBetsManager, profileUser, userGameBets, teams, generalBetsAreOpen, players) {
    $scope.profileUser = profileUser;
    $scope.userGameBets = userGameBets;
    $scope.teams = teams;
    $scope.players = players;
    $scope.noGeneralBetWasSubmitted = false;
    $scope.generalBetsAreOpen = (generalBetsAreOpen === true);
    $log.debug('UserProfileCtrl: generalBetsAreOpen = ' + generalBetsAreOpen);
    $scope.alreadyFollow = $scope.security.user.Followees.includes($scope.profileUser.Username)

    $scope.isLoggedUserProfile = () => {
        var res = ($scope.security.user != null) && ($scope.security.user.Username === $scope.profileUser.Username);
        $log.debug('UserProfileCtrl: isLoggedUserProfile = ' + res);
        return ($scope.security.user != null) && ($scope.security.user.Username === $scope.profileUser.Username);
    };

    $scope.isGeneralBetClosed = () => {
        var res = !$scope.generalBetsAreOpen;
        $log.debug('UserProfileCtrl: isGeneralBetClosed = ' + res);
        return res;
    };

    $scope.isGeneralBetReadOnly = () => {
        var res = (!$scope.isLoggedUserProfile() || ($scope.isGeneralBetClosed()));
        $log.debug('UserProfileCtrl: isGeneralBetReadOnly = ' + res);
        return res;
    }

    $scope.shoudLoadGeneralBet = () => {
        var res = ($scope.isLoggedUserProfile() || ($scope.isGeneralBetClosed()));
        $log.debug('UserProfileCtrl: shoudLoadGeneralBet = ' + res);
        return res;
    }

    if ($scope.shoudLoadGeneralBet()) {
        $scope.generalBetsPromise = GeneralBetsManager.hasGeneralBet($scope.profileUser.Username).then((answer) => {
            $log.debug('UserProfileCtrl: hasGeneralBet = ' + answer);
            if (answer === true) {
                GeneralBetsManager.getUserGeneralBet($scope.profileUser.Username).then((generalBet) => {
                    $log.info('UserProfileCtrl: got user general bet - ' + angular.toJson(generalBet));
                    $scope.generalBet = generalBet;
                });
            }
            else {
                $scope.generalBet = {};
                if ($scope.isGeneralBetClosed()) {
                    $scope.noGeneralBetWasSubmitted = true;
                    return;
                }
                if ($scope.isLoggedUserProfile() && !$scope.isGeneralBetClosed()) {
                    return;
                }
                $scope.noGeneralBetWasSubmitted = true;
            }
        });
    }

    $scope.saveGeneralBet = () => {
        if (angular.isDefined($scope.generalBet.GeneralBetId)) {
            $scope.generalBetsPromise = $scope.generalBet.update().then(() => {
                Alert.success('General Bet was updated successfully');
            }, () => {
                Alert.error('Failed to update General Bet, please try again');
            });
        }

        else {
            $scope.generalBetsPromise = GeneralBetsManager.addGeneralBet($scope.generalBet).then((data) => {
                $log.log('UserProfileCtrl: General Bet ' + data.GeneralBetId + ' was added');
                $scope.generalBet = data;
                Alert.success('General Bet was added successfully');
            }, () => {
                Alert.error('Failed to add General Bet, please try again');
            });
        }
    };

    $scope.social = () => {
        if ($scope.alreadyFollow) {
            UsersManager.unfollow($scope.profileUser.Username).then(() => {
                $scope.alreadyFollow = false;
                const index = $scope.security.user.Followees.indexOf($scope.profileUser.Username);
                $scope.security.user.Followees.splice(index, 1);
                Alert.success('You no longer following ' + $scope.profileUser.Username);
            }).catch((err) => {
                Alert.error('Failed to unfollow ' + $scope.profileUser.Username + ': ' + err);
            });
        } else {
            UsersManager.follow($scope.profileUser.Username).then(() => {
                $scope.alreadyFollow = true;
                $scope.security.user.Followees.push($scope.profileUser.Username);
                Alert.success('You are now following ' + $scope.profileUser.Username);
            }).catch((err) => {
                Alert.error('Failed to follow ' + $scope.profileUser.Username + ': ' + err);
            });
        }
    };

    $scope.getSocialPromise = UsersManager.getSocial($scope.profileUser.Username).then((data) => {
        $log.log('UserProfileCtrl: Got social response');
        $scope.followers = data['followers'];
        $scope.followees = data['followees'];
    });

    if ($scope.isLoggedUserProfile()) {
        $scope.getStatsPromise = UsersManager.getMyStats().then((data) => {
            $scope.performance = data;
        }).catch((err) => {
            $log.error('Failed to get user slef statistics', err);
            Alert.error('Failed to fetch user statistics: ' + err);
        });
    } else {
        $scope.getStatsPromise = UsersManager.getStats($scope.profileUser.Username).then((data) => {
            $scope.performance = data;
        }).catch((err) => {
            $log.error('Failed to get user statistics', err);
            Alert.error('Failed to fetch user statistics: ' + err);
        });
    }
    if ($scope.security.user.Username === $scope.profileUser.Username) {
        $scope.compareUsersPromise = UsersManager.getUserProgess($scope.security.user.Username);
    } else {
        $scope.compareUsersPromise = UsersManager.compareUsers($scope.security.user.Username, $scope.profileUser.Username);
    }
    $scope.compareUsersPromise.then((res) => {
        if (res.length > 0) {
            let users = _.map(res[0].Entries, (entry) => { return entry.Name });
            var cols = _.map(users, (user) => {
                return {
                    id: user,
                    label: user,
                    type: "number"
                }
            });
            cols.unshift({
                id: "date",
                label: "Date",
                type: "date"
            });
            let rows = _.map(res, (entry) => {
                var res = { c: [{
                    v: new Date(entry.Date),
                }]};
                _.each(entry.Entries, (entry) => {
                    res.c.push({
                        v: entry.Place,
                    });
                });
                return res;
            });
            $scope.chart = {
                type: "LineChart",
                data: {
                    "cols": cols,
                    "rows": rows
                },
                options: {
                    colors: ['#0000FF', '#009900', '#CC0000', '#DD9900'],
                    defaultColors: ['#0000FF', '#009900', '#CC0000', '#DD9900'],
                    displayExactValues: true,
                    is3D: true,
                    backgroundColor: { fill: 'transparent' },
                    vAxis: {
                        title: "Place",
                    },
                    hAxis: {
                        title: "Date"
                    }
                }
            };
        }
    }).catch((err) => {
        $log.error('Failed to compare users', err);
        Alert.error('Failed to compare users: ' + err);
    });
}]);
