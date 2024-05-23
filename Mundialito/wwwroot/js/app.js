angular.module('mundialitoApp', ['security', 'ngSanitize', 'ngRoute', 'ngAnimate', 'ui.bootstrap', 'autofields.bootstrap', 'cgBusy', 'ajoslin.promise-tracker', 'ui.select2',
    'ui.bootstrap.datetimepicker', 'FacebookPluginDirectives', 'ui.grid', 'ui.grid.autoResize', 'googlechart', 'angular-data.DSCacheFactory', 'toaster', 'ui.grid.saveState', 'ui.grid.resizeColumns'])
    .value('cgBusyTemplateName','App/Partials/angular-busy.html')
    .config(['$routeProvider', '$httpProvider', '$locationProvider', '$parseProvider', 'securityProvider','Constants', function ( $routeProvider, $httpProvider, $locationProvider, $parseProvider, securityProvider, Constants) {
        $locationProvider.html5Mode(true);
        $httpProvider.defaults.headers.common['X-Requested-With'] = 'XMLHttpRequest';
        $httpProvider.interceptors.push('myHttpInterceptor');
        securityProvider.urls.login = Constants.LOGIN_PATH;
        securityProvider.usePopups = false;

        $routeProvider.
            when('/', {
                templateUrl: 'App/Dashboard/Dashboard.html',
                controller: 'DashboardCtrl',
                resolve : {
                    teams : ['TeamsManager', function ( TeamsManager) {
                        return TeamsManager.loadAllTeams();
                    }],
                    players: ['PlayersManager', function (PlayersManager) {
                        return PlayersManager.loadAllPlayers();
                    }]
                }
            }).
            when('/bets_center', {
                templateUrl: 'App/Bets/BetsCenter.html',
                controller: 'BetsCenterCtrl',
                resolve : {
                    games : ['GamesManager', function (GamesManager) {
                        return GamesManager.loadOpenGames();
                    }]
                }
            }).
            when('/users/:username', {
                templateUrl: 'App/Users/UserProfile.html',
                controller: 'UserProfileCtrl',
                resolve : {
                    profileUser: ['$route', 'UsersManager', function ($route, UsersManager) {
                        var username = $route.current.params.username;
                        return UsersManager.getUser(username, true);
                    }],
                    userGameBets : ['$route', 'BetsManager', function ($route, BetsManager) {
                        var username = $route.current.params.username;
                        return BetsManager.getUserBets(username);
                    }],
                    teams : ['TeamsManager', function ( TeamsManager) {
                        return TeamsManager.loadAllTeams();
                    }],
                    generalBetsAreOpen : ['GeneralBetsManager', function (GeneralBetsManager) {
                        return GeneralBetsManager.canSubmtiGeneralBet();
                    }],
                    players: ['PlayersManager', function (PlayersManager) {
                        return PlayersManager.loadAllPlayers();
                    }]
                }
            }).
            when('/manage_users', {
                templateUrl: 'App/Users/ManageApp.html',
                controller: 'ManageAppCtrl',
                resolve: {
                    users : ['UsersManager', function (UsersManager) {
                        return UsersManager.loadAllUsers();
                    }],
                    teams : ['TeamsManager', function (TeamsManager) {
                        return TeamsManager.loadAllTeams();
                    }],
                    generalBets: ['GeneralBetsManager' , function (GeneralBetsManager) {
                        return GeneralBetsManager.loadAllGeneralBets();
                    }],
                    players: ['PlayersManager', function (PlayersManager) {
                        return PlayersManager.loadAllPlayers();
                    }]
                }
            }).
            when('/teams', {
                templateUrl: 'App/Teams/Teams.html',
                controller: 'TeamsCtrl',
                resolve: {
                    teams: ['TeamsManager', function (TeamsManager) {
                        return TeamsManager.loadAllTeams();
                    }]
                }
            }).
            when('/teams/:teamId', {
                templateUrl: 'App/Teams/Team.html',
                controller: 'TeamCtrl',
                resolve: {
                    team: ['$route','TeamsManager',  function ($route, TeamsManager) {
                        var teamId = $route.current.params.teamId;
                        return TeamsManager.getTeam(teamId);
                    }],
                    games : ['$route','GamesManager', function ($route, GamesManager) {
                        var teamId = $route.current.params.teamId;
                        return GamesManager.getTeamGames(teamId);
                    }]
                }
            }).
            when('/games/:gameId', {
                templateUrl: 'App/Games/Game.html',
                controller: 'GameCtrl',
                resolve: {
                    game: ['$route','GamesManager', function ($route, GamesManager) {
                        var gameId = $route.current.params.gameId;
                        return  GamesManager.getGame(gameId);
                    }],
                    userBet: ['$route','BetsManager', function ($route, BetsManager) {
                        var gameId = $route.current.params.gameId;
                        return BetsManager.getUserBetOnGame(gameId);
                    }]
                }
            }).
            when('/games', {
                templateUrl: 'App/Games/Games.html',
                controller: 'GamesCtrl',
                resolve: {
                    games: ['GamesManager', function (GamesManager) {
                        return GamesManager.loadAllGames();
                    }],
                    teams : ['TeamsManager', function (TeamsManager) {
                        return TeamsManager.loadAllTeams();
                    }]
                }
            }).
            when('/stadiums/:stadiumId', {
                templateUrl: 'App/Stadiums/Stadium.html',
                controller: 'StadiumCtrl',
                resolve: {
                    stadium: ['$q','$route','StadiumsManager',  function ($q, $route, StadiumsManager) {
                        var stadiumId = $route.current.params.stadiumId;
                        return StadiumsManager.getStadium(stadiumId, true);
                    }]
                }
            }).
            when('/stadiums', {
                templateUrl: 'App/Stadiums/Stadiums.html',
                controller: 'StadiumsCtrl',
                resolve: {
                    stadiums : ['StadiumsManager', function (StadiumsManager) {
                        return StadiumsManager.loadAllStadiums();
                    }]
                }
            }).
            when('/login', {
                templateUrl: 'App/Accounts/Login.html'
            }).
            when('/forgot', {
                templateUrl: 'App/Accounts/ForgetPassword.html',
                controller: 'ForgetPasswordCtrl',
            }).
            when('/reset', {
                templateUrl: 'App/Accounts/ResetPassword.html',
                controller: 'ResetPasswordCtrl',
            }).
            when('/join', {
                templateUrl: 'App/Accounts/Register.html'
            }).
            when('/manage', {
                templateUrl: 'App/Accounts/Manage.html'
            }).
            otherwise({
                redirectTo: '/'
            });
    }])
    .run(['$rootScope', '$log', 'security', '$route', '$location', function ($rootScope, $log, security, $route, $location) {
        security.events.login = function (security, user) {
            $log.log('Current user details: ' + angular.toJson(user));
            $rootScope.mundialitoApp.authenticating = false;
        };
        security.events.reloadUser = function (security, user) {
            $log.log('User reloaded' + angular.toJson(user));
            $rootScope.mundialitoApp.authenticating = false;
        };
        security.events.logout = function (security) {
            $log.log('User logged out');
            security.authenticate();
        };
        $rootScope.mundialitoApp = {
            params: null,
            loading: true,
            authenticating: true,
            message: null
        };
        if (!['/reset', '/forgot', '/join', '/login'].includes($location.$$path)) {
            $log.log('Starting authentication')
            security.authenticate();
        }
        $rootScope.security = security;

        $rootScope.$on('$locationChangeStart', function () {
            $log.debug('$locationChangeStart');
            $rootScope.mundialitoApp.loading = true;
        });
        $rootScope.$on('$locationChangeSuccess', function () {
            $log.debug('$locationChangeSuccess');
            $rootScope.mundialitoApp.params = angular.copy($route.current.params);
            $rootScope.mundialitoApp.loading = false;
        });

        $rootScope.$on('$routeChangeStart', function () {
            $log.debug('$routeChangeStart');
            $rootScope.mundialitoApp.message = 'Loading...';
        });
        $rootScope.$on('$routeChangeSuccess', function () {
            $log.debug('$routeChangeSuccess');
            $rootScope.mundialitoApp.message = null;
        });

    }]);
angular.module('mundialitoApp').constant('Constants',
    {
        LOGIN_PATH   : '/login',
        REFRESH_TIME : 300000
    }
);
'use strict';
angular.module('mundialitoApp').controller('ForgetPasswordCtrl', ['$scope', '$rootScope', 'security', 'Alert', function ($scope, $rootScope, Security, Alert) {
    $rootScope.mundialitoApp.authenticating = false;

    var ForgetModel = function () {
        return {
            Email: ''
        }
    };

    $scope.user = new ForgetModel();
    $scope.forget = function () {
        if (!$scope.emailForm.$valid) return;
        $rootScope.mundialitoApp.message = "Processing...";
        Security.forgotPassword(angular.copy($scope.user)).then(() => {
            Alert.success('Reset password token was sent to your email, please follow the link from there');
        }).catch((e) => {
            Alert.error('Failed to generate reset password token: ' + e);
        }).finally(function () {
            $rootScope.mundialitoApp.message = null;
        });
    }
    $scope.schema = [
        { property: 'Email', label: 'Email Address', type: 'email', attr: { required: true } },
    ];
}]);
'use strict';
angular.module('mundialitoApp').controller('LoginCtrl', ['$scope', '$rootScope' , 'security', function ($scope, $rootScope, Security) {
    $rootScope.mundialitoApp.authenticating = false;

    var LoginModel = function () {
        return {
            username: '',
            password: '',
            rememberMe: false
        }
    };

    $scope.user = new LoginModel();
    $scope.login = function () {
        if (!$scope.loginForm.$valid) return;
        $rootScope.mundialitoApp.message = "Processing Login...";
        Security.login(angular.copy($scope.user)).finally(function () {
            $rootScope.mundialitoApp.message = null;
        });
        
    }
    $scope.schema = [
            { property: 'username', type: 'text', attr: { ngMinlength: 4, required: true } },
            { property: 'password', type: 'password', attr: { ngMinlength: 4, required: true } },
            { property: 'rememberMe', label: 'Keep me logged in', type: 'checkbox' }
    ];
}]);
'use strict';
angular.module('mundialitoApp').controller('ManageCtrl', ['$scope','Alert', function ($scope, Alert) {
    var ChangePasswordModel = function () {
        return {
            oldPassword: '',
            newPassword: '',
            confirmPassword: ''
        }
    };
    
    $scope.changingPassword = null;
    $scope.changePassword = function () {
        $scope.changingPassword = new ChangePasswordModel();
    }
    $scope.cancel = function () {
        $scope.changingPassword = null;
    }
    $scope.updatePassword = function () {
        if (!$scope.manageForm.$valid) return;
        var newPassword = angular.copy($scope.changingPassword);
        $scope.changingPassword = null;
        $scope.security.changePassword(newPassword).then(function () {
            Alert.success("Password was changed sucessfully");
        }, function () {
            Alert.error("Failed to change password");
            $scope.changingPassword = newPassword;
        });
    }
    $scope.changePasswordSchema = [
            { property: 'oldPassword', type: 'password', attr: { required: true } },
            { property: 'newPassword', type: 'password', attr: { ngMinlength: 4, required: true } },
            { property: 'confirmPassword', type: 'password', attr: { confirmPassword: 'changingPassword.newPassword', required: true } }
    ];
}]);
'use strict';
angular.module('mundialitoApp').controller('RegisterCtrl', ['$scope', 'security', function ($scope, Security) {
    $scope.mundialitoApp.authenticating = false;

    var User = function () {
        return {
            firstname: '',
            lastname: '',
            email: '',
            username: '',
            password: '',
            confirmPassword: ''
        }
    }

    $scope.user = new User();
    $scope.join = function () {
        if (!$scope.joinForm.$valid) return;
        $scope.isJoinActive = true;
        $scope.mundialitoApp.message = "Processing Registration...";
        Security.register(angular.copy($scope.user)).finally(function () {
            $scope.mundialitoApp.message = null;
            $scope.isJoinActive = false;
        });
    };

    $scope.schema = [
            { property: 'firstname', label: 'First Name', type: 'text', attr: { required: true } },
            { property: 'lastname', label: 'Last Name', type: 'text', attr: { required: true } },
            { property: 'email', label: 'Email Address', type: 'email', attr: { required: true } },
            { property: 'username', type: 'text', attr: { ngMinlength: 4, required: true } },
            { property: 'password', type: 'password', attr: { required: true } },
            { property: 'confirmPassword', label: 'Confirm Password', type: 'password', attr: { confirmPassword: 'user.password', required: true } }
    ];

    if ($scope.mundialitoApp.protect === true) {
        $scope.user.privateKey = '';
        $scope.schema.push({ property: 'privateKey', help: 'The Private Key was given in the e-mail of the payment confirmation', label: 'Private Key', type: 'text', attr: { required: true } });
    }

}]);
'use strict';
angular.module('mundialitoApp').controller('ResetPasswordCtrl', ['$scope', '$rootScope', 'security', '$location', 'Alert', function ($scope, $rootScope, Security, $location, Alert) {
    $rootScope.mundialitoApp.authenticating = false;

    var ResetPasswordModel = function () {
        return {
            confirmPassword: '',
            password: '',
            email: $location.search()['email'],
            token: $location.search()['token']
        }
    };

    $scope.user = new ResetPasswordModel();
    $scope.reset = function () {
        if (!$scope.resetForm.$valid) return;
        $rootScope.mundialitoApp.message = "Processing Reset Password...";
        Security.resetPassword(angular.copy($scope.user)).then(() => {
            Alert.success('Your was was reset successfully');
        }).finally(function () {
            $rootScope.mundialitoApp.message = null;
        });

    }
    $scope.schema = [
        { property: 'password', type: 'password', attr: { required: true } },
        { property: 'confirmPassword', label: 'Confirm Password', type: 'password', attr: { confirmPassword: 'user.password', required: true } }
    ];
}]);
'use strict';
angular.module('mundialitoApp').factory('Bet', ['$http','$log', function($http,$log) {
    function Bet(betData) {
        if (betData) {
            this.setData(betData);
        }
        // Some other initializations related to bet
    };

    Bet.prototype = {
        setData: function(betData) {
            angular.extend(this, betData);
        },
        update: function() {
            $log.debug('Bet: Will update bet ' + this.BetId)
            return $http.put('api/bets/' + this.BetId, this, { tracker: 'updateBet' });
        },
        getGameUrl: function() {
            return '/games/' + this.Game.GameId;
        },
        getClass: function() {
            if (this.Points === 7) {
                return 'success';
            }
            if (this.Points >= 5) {
                return 'primary';
            }
            if (this.Points >= 3) {
                return 'info';
            }
            if (this.Points > 0) {
                return 'warning';
            }
            return 'danger';
        }
    };
    return Bet;
}]);

'use strict';
angular.module('mundialitoApp').controller('BetsCenterCtrl', ['$scope', '$log', '$timeout', 'Alert', 'BetsManager', 'games', function ($scope, $log, $timeout, Alert, BetsManager, games) {
    $scope.games = games;
    $scope.bets = {};


    var loadUserBets = function() {
        if (!angular.isDefined($scope.security.user) || ($scope.security.user == null))
        {
            $log.debug('BetsCenterCtrl: user info not loaded yet, will retry in 1 second');
            $timeout(loadUserBets,1000);
        }
        else {
            BetsManager.getUserBets($scope.security.user.Username).then(function (bets) {
                for(var i=0; i < bets.length; i++) {
                    $scope.bets[bets[i].Game.GameId] = bets[i];
                    $scope.bets[bets[i].Game.GameId].GameId = bets[i].Game.GameId
                }

                for(var j=0; j < games.length; j++) {
                    if (!angular.isDefined($scope.bets[games[j].GameId])) {
                        $log.debug('BetsCenterCtrl: game ' + games[j].GameId + ' has not bet')
                        $scope.bets[games[j].GameId] = { BetId : -1, GameId : games[j].GameId};
                    }
                    else {
                        $scope.bets[$scope.bets[games[j].GameId]] = bets[i];
                    }
                }
            });
        }
    };

    loadUserBets();

    $scope.updateBet = function(gameId) {
        if ($scope.bets[gameId].BetId !== -1) {
            $log.debug('BetsCenterCtrl: Will update bet');
            $scope.bets[gameId].update().success(function(data) {
                Alert.success('Bet was updated successfully');
                BetsManager.setBet(data);
            }).catch(function () {
                Alert.error('Failed to update Bet, please try again');
            });
        }
        else {
            $log.debug('BetsCenterCtrl: Will create new bet');
            BetsManager.addBet($scope.bets[gameId]).then(function(data) {
                $log.log('BetsCenterCtrl: Bet ' + data.BetId + ' was added');
                $scope.bets[gameId] = data;
                Alert.success('Bet was added successfully');
            }).catch(function () {
                Alert.error('Failed to add Bet, please try again');
            });
        }
    };
    $scope.shuffleBet = function(gameId) {
        var homeGoals, awayGoals;
	    var toto = ['1', 'X', '2'];
	    var goals = [0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 3, 3, 4, 5];
	    var gameMark = toto[Math.floor((Math.random() * 3))];
	    do {
	        homeGoals = goals[Math.floor((Math.random() * goals.length))];
	        awayGoals = goals[Math.floor((Math.random() * goals.length))];
	    } while (gameMark !== 'X' && homeGoals === awayGoals);
	    $log.debug('Random game mark is ' + gameMark);
	    if (gameMark === 'X') {
	        awayGoals = homeGoals;
	    }
	    $log.debug('Home goals: ' + homeGoals);
	    $log.debug('Away goals: ' + awayGoals);
	    $scope.bets[gameId].HomeScore = homeGoals;
	    $scope.bets[gameId].AwayScore = awayGoals
        $scope.bets[gameId].CardsMark = toto[Math.floor((Math.random() * 3))];
		$scope.bets[gameId].CornersMark = toto[Math.floor((Math.random() * 3))];
    };
}]);

'use strict';
angular.module('mundialitoApp').factory('BetsManager', ['$http', '$q', 'Bet', '$log', 'MundialitoUtils', 'GamesManager', function ($http, $q, Bet, $log, MundialitoUtils, GamesManager) {
    var betsManager = {
        _pool: {},
        _retrieveInstance: function(betId, betData) {
            var instance = this._pool[betId];

            if (instance) {
                $log.debug('BetsManager: updating existing instance of bet ' + betId);
                instance.setData(betData);
            } else {
                $log.debug('BetsManager: saving new instance of bet ' + betId);
                instance = new Bet(betData);
                this._pool[betId] = instance;
            }
            instance.LoadTime = new Date();
            return instance;
        },
        _search: function(betId) {
            $log.debug('BetsManager: will fetch bet ' + betId + ' from local pool');
            var instance = this._pool[betId];
            if (angular.isDefined(instance) && MundialitoUtils.shouldRefreshInstance(instance)) {
                $log.debug('BetsManager: Instance was loaded at ' + instance,LoadTime + ', will reload it from server');
                return undefined;
            }
            return instance;
        },
        _load: function(betId, deferred) {
            var scope = this;
            $log.debug('BetsManager: will fetch bet ' + betId + ' from server');
            $http.get('api/bets/' + betId, { tracker: 'getBet' })
                .success(function(betData) {
                    var bet = scope._retrieveInstance(betData.BetId, betData);
                    deferred.resolve(bet);
                })
                .error(function() {
                    deferred.reject();
                });
        },

        /* Public Methods */
        /* Use this function in order to add a new bet */
        addBet: function(betData) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('BetsManager: will add new bet - ' + angular.toJson(betData));
            $http.post('api/bets/', betData, { tracker: 'addBetOnGame' }).success(function(data) {
                var bet = scope._retrieveInstance(data.BetId, data);
                GamesManager.clearGamesCache();
                deferred.resolve(bet);
            }).error(function (err) {
                $log.error('Failed to add bet');
                deferred.reject(err);
            });
            return deferred.promise;
        },

        /* Use this function in order to get a bet instance by it's id */
        getBet: function(betId,fresh) {
            var deferred = $q.defer();
            var bet = undefined;
            if ((!angular.isDefined(fresh) || (!fresh))) {
                bet = this._search(betId);
            }
            if (bet) {
                deferred.resolve(bet);
            } else {
                this._load(betId, deferred);
            }
            return deferred.promise;
        },

        /* Use this function in order to get instances of all the game bets */
        getGameBets: function(gameId) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('BetsManager: will fetch all bets of game ' + gameId + ' from server');
            $http.get('api/games/' + gameId + '/bets', { tracker: 'getGameBets' })
                .success(function(betsArray) {
                    var bets = [];
                    betsArray.forEach(function(betData) {
                        var bet = scope._retrieveInstance(betData.BetId, betData);
                        bets.push(bet);
                    });

                    deferred.resolve(bets);
                })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        getUserBets : function(username) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('BetsManager: will fetch user ' + username +' bets from server');
            $http.get('api/bets/user/' + username, { tracker: 'getUserBets' })
                .success(function(betsArray) {
                    var bets = [];
                    betsArray.forEach(function(betData) {
                        var bet = scope._retrieveInstance(betData.BetId, betData);
                        bets.push(bet);
                    });

                    deferred.resolve(bets);
                })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        getUserBetOnGame : function(gameId) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('BetsManager: will fetch user bet of game ' + gameId + ' from server');
            $http.get('api/games/' + gameId + '/mybet', { tracker: 'getUserBetOnGame' })
                .success(function(betData) {
                    if (betData.BetId != -1)
                    {
                        var bet = scope._retrieveInstance(betData.BetId, betData);
                        deferred.resolve(bet);
                    }
                    deferred.resolve(betData);
                })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        /*  This function is useful when we got somehow the bet data and we wish to store it or update the pool and get a general bet instance in return */
        setBet: function(betData) {
            $log.debug('BetsManager: will set bet ' + betData.BetId + ' to -' + angular.toJson(betData));
            var scope = this;
            var bet = this._search(betData.BetId);
            if (bet) {
                bet.setData(betData);
            } else {
                bet = scope._retrieveInstance(betData.BetId, betData);
            }
            return bet;
        }

    };
    return betsManager;
}]);

'use strict';
angular.module('mundialitoApp').controller('DashboardCtrl', ['$scope', '$log', '$location', '$timeout', 'GamesManager', 'UsersManager', 'GeneralBetsManager', 'teams', 'players',
    function ($scope, $log, $location, $timeout, GamesManager, UsersManager, GeneralBetsManager, teams, players) {
    $scope.generalBetsAreOpen = false;
    $scope.submittedGeneralBet = true;
    $scope.pendingUpdateGames = false;

    $scope.teamsDic = {};
    $scope.playersDic = {};

    for (var i = 0; i < teams.length; i++) {
        $scope.teamsDic[teams[i].TeamId] = teams[i];
    }

    for (var i = 0; i < players.length; i++) {
        $scope.playersDic[players[i].PlayerId] = players[i];
    }

    GamesManager.loadAllGames().then(function (games) {
        $scope.games = games;
        $scope.pendingUpdateGames = _.findWhere($scope.games, { IsPendingUpdate: true }) !== undefined;
    });

    var userHasGeneralBet = function () {
        if (!angular.isDefined($scope.security.user) || ($scope.security.user == null)) {
            $log.debug('DashboardCtrl: user info not loaded yet, will retry in 1 second');
            $timeout(userHasGeneralBet, 1000);
        }
        else {
            GeneralBetsManager.hasGeneralBet($scope.security.user.Username).then(function (data) {
                $scope.submittedGeneralBet = data === true;
            });
        }
    };

    userHasGeneralBet();

    GeneralBetsManager.canSubmtiGeneralBet().then(function (data) {
        $scope.generalBetsAreOpen = (data === true);
        if (!$scope.generalBetsAreOpen) {
            GeneralBetsManager.loadAllGeneralBets().then(function (data) {
                $scope.generalBets = data;
                $scope.winningTeams = {};
                $scope.winningPlayers = {};
                for (var i = 0; i < $scope.generalBets.length ; i++) {
                    if (!angular.isDefined($scope.winningTeams[$scope.generalBets[i].WinningTeamId])) {
                        $scope.winningTeams[$scope.generalBets[i].WinningTeamId] = 0;
                    }
                    $scope.winningTeams[$scope.generalBets[i].WinningTeamId] += 1;
                    if (!angular.isDefined($scope.winningPlayers[$scope.generalBets[i].GoldenBootPlayerId])) {
                        $scope.winningPlayers[$scope.generalBets[i].GoldenBootPlayerId] = 0;
                    }
                    $scope.winningPlayers[$scope.generalBets[i].GoldenBootPlayerId] += 1;
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
                for (var teamId in $scope.winningTeams) {
                    chart1.data.push([$scope.teamsDic[teamId].Name, $scope.winningTeams[teamId]]);
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
                for (var playerId in $scope.winningPlayers) {
                    chart1.data.push([$scope.playersDic[playerId].Name, $scope.winningPlayers[playerId]]);
                }
                $scope.playersChart = chart1;
            });
        }
    });

    UsersManager.getTable().then(function (users) {
        $scope.users = users;
    });

    $scope.isOpenForBetting = function () {
        return function (item) {
            return item.IsOpen;
        };
    };

    $scope.isPendingUpdate = function () {
        return function (item) {
            return item.IsPendingUpdate;
        };
    };

    $scope.isDecided = function () {
        return function (item) {
            return !item.IsOpen && !item.IsPendingUpdate;
        };
    };

    function getRowTemplate() {
        var rowtpl = '<div ng-click="grid.appScope.goToUser(row)" style="cursor: pointer" ng-class="{\'text-primary\':row.entity.Username===grid.appScope.security.user.Username }"><div ng-repeat="(colRenderIndex, col) in colContainer.renderedColumns track by col.colDef.name" class="ui-grid-cell" ng-class="{ \'ui-grid-row-header-cell\': col.isRowHeader }" ui-grid-cell></div></div>';
        return rowtpl;
    }

    $scope.gridOptions = {
        data: 'users',
        saveWidths: true,
        saveVisible: true,
        saveOrder: true,
        enableRowSelection: false,
        enableSelectAll: false,
        multiSelect: false,
        rowTemplate: getRowTemplate(),
        columnDefs: [
            { field: 'Place', displayName: '', resizable: false, maxWidth: 30 },
            { field: 'Name', displayName: 'Name', resizable: true, minWidth: 150},
            { field: 'TotalMarks', displayName: 'Total Marks', resizable: true },
            { field: 'Results', displayName: 'Results', resizable: true },
            { field: 'Marks', displayName: 'Marks', resizable: true },
            { field: 'YellowCards', displayName: 'Yellow Cards Marks', resizable: true },
            { field: 'Corners', displayName: 'Corners Marks', resizable: true },
            { field: 'Points', displayName: 'Points', resizable: true },
            { field: 'PlaceDiff', displayName: '', resizable: false, maxWidth: 45, cellTemplate: '<div ng-class="{\'text-success\': COL_FIELD.indexOf(\'+\') !== -1, \'text-danger\': (COL_FIELD.indexOf(\'+\') === -1) && (COL_FIELD !== \'0\')}"><div class="ngCellText">{{::COL_FIELD}}</div></div>' }
        ],
        onRegisterApi: function(gridApi){
            $scope.gridApi = gridApi;
            $scope.gridApi.colResizable.on.columnSizeChanged($scope, saveState);
            $scope.gridApi.core.on.columnVisibilityChanged($scope, saveState);
            $scope.gridApi.core.on.sortChanged($scope, saveState);
        }
    };

    function saveState() {
        var state = $scope.gridApi.saveState.save();
        localStorage.setItem('gridState', state);
    };

    function restoreState() {
        $timeout(function () {
            var state = localStorage.getItem('gridState');
            if (state) $scope.gridApi.saveState.restore($scope, state);
        });
    };

    $scope.getTableHeight = function () {
        var rowHeight = 30; // your row height
        var headerHeight = 30; // your header height
        var total = ( ($scope.users ? $scope.users.length : 0) * rowHeight + headerHeight);
        $log.debug('Total Height: ' + total);
        return {
            height: total + "px"
        };
    };

    $scope.goToUser = function (rowItem) {
        $location.path(rowItem.entity.getUrl())
    }

}]);

'use strict';
angular.module('mundialitoApp').factory('Game', ['$http','$log', function($http,$log) {
    function Game(gameData) {
        if (gameData) {
            this.setData(gameData);
        }
        // Some other initializations related to game
    };

    Game.prototype = {
        setData: function(gameData) {
            angular.extend(this, gameData);
        },
        delete: function() {
            if (confirm('Are you sure you would like to delete game ' + this.GameId)) {
                $log.debug('Game: Will delete game ' + this.GameId)
                return $http.delete("api/games/" + this.GameId, { tracker: 'deleteGame' });
            }
        },
        update: function() {
            $log.debug('Game: Will update game ' + this.GameId)
            return $http.put("api/games/" + this.GameId, this, { tracker: 'editGame' });
        },
        getUrl: function() {
            return '/games/' + this.GameId;
        }
    };
    return Game;
}]);

'use strict';
angular.module('mundialitoApp').controller('GameCtrl', ['$scope', '$log', 'GamesManager', 'BetsManager', 'game', 'userBet', 'Alert', function ($scope, $log, GamesManager, BetsManager, game, userBet, Alert) {
    $scope.game = game;
    $scope.userBet = userBet;
    $scope.userBet.GameId = game.GameId;
    $scope.showEditForm = false;

    if (!$scope.game.IsOpen)
    {
        BetsManager.getGameBets($scope.game.GameId).then(function (data) {
            $log.debug("GameCtrl: get game bets" + angular.toJson(data));
            $scope.gameBets = data;

            var chart1 = {};
            chart1.type = "PieChart";
            chart1.options = {
                displayExactValues: true,
                is3D: true,
                backgroundColor: { fill:'transparent' },
                chartArea: {left:10,top:20,bottom:0,height:"100%"},
                title: 'Bets Distribution'
            };
            var mark1 = _.filter(data, function(bet) { return bet.HomeScore > bet.AwayScore}).length;
            var markX = _.filter(data, function(bet) { return bet.HomeScore === bet.AwayScore}).length;
            var mark2 = _.filter(data, function(bet) { return bet.HomeScore < bet.AwayScore}).length;
            chart1.data = [
                ['Game Mark', 'Number Of Users'],
                ['1', mark1],
                ['X', markX],
                ['2', mark2]
            ];
            $scope.chart = chart1;
        });
    }

    $scope.updateGame = function() {
        if  ((angular.isDefined(game.Stadium.Games)) && (game.Stadium.Games != null)) {
            delete game.Stadium.Games;
        }
        $scope.game.update().success(function (data) {
            Alert.success('Game was updated successfully');
            GamesManager.setGame(data);
        }).catch(function (err) {
            Alert.error('Failed to update game, please try again');
            $log.error('Error updating game', err);
        });
    };

    $scope.updateBet = function() {
        if ($scope.userBet.BetId !== -1) {
            $scope.userBet.update().success(function (data) {
                Alert.success('Bet was updated successfully');
                BetsManager.setBet(data);
            }).error(function (err) {
                Alert.error('Failed to update bet, please try again');
                $log.error('Error updating bet', err);
            });
        }
        else {
            BetsManager.addBet($scope.userBet).then(function (data) {
                $log.log('GameCtrl: Bet ' + data.BetId + ' was added');
                $scope.userBet = data;
                Alert.success('Bet was added successfully');
            }, function (err) {
                Alert.error('Failed to add bet, please try again');
                $log.error('Error adding bet', err);
            });
        }
    };

    $scope.sort = function(column) {
        $log.debug('GameCtrl: sorting by ' + column);
        $scope.gameBets = _.sortBy($scope.gameBets, function (item) {
            switch (column)
            {
                case 'points': return item.Points;
                case 'cards': return item.CardsMark;
                case 'corners': return item.CornersMark;
                case 'user': return item.User.FirstName + item.User.LastName;
                case 'result': return item.HomeScore + '-' + item.AwayScore;
            }
        });
    };
}]);
'use strict';
angular.module('mundialitoApp').controller('GamesCtrl', ['$scope','$log','GamesManager','games','teams', 'StadiumsManager' ,'Alert',function ($scope,$log, GamesManager, games, teams, StadiumsManager, Alert) {
    $scope.newGame = null;
    $scope.gamesFilter = "All";
    $scope.games = games;
    $scope.teams = teams;
    

    StadiumsManager.loadAllStadiums().then(function (res) {
        $scope.stadiums = res;
    });

    $scope.addNewGame = function () {
        $('.selectpicker').selectpicker('refresh');
        $scope.newGame = GamesManager.getEmptyGameObject();
    };

    $scope.saveNewGame = function() {
        GamesManager.addGame($scope.newGame).then(function(data) {
            Alert.success('Game was added successfully');
            $scope.newGame = GamesManager.getEmptyGameObject();
            $scope.games.push(data);
        });
    };

    $scope.isPendingUpdate = function() {
        return function(item) {
            return item.IsPendingUpdate;
        };
    };

    $scope.updateGame = function(game) {
        if  ((angular.isDefined(game.Stadium.Games)) && (game.Stadium.Games != null)) {
            delete game.Stadium.Games;
        }
        game.update().success(function (data) {
            Alert.success('Game was updated successfully');
            GamesManager.setGame(data);
        });
    };
}]);
'use strict';
angular.module('mundialitoApp').factory('GamesManager', ['$http', '$q', 'Game', '$log', 'MundialitoUtils', 'DSCacheFactory', function ($http, $q, Game, $log, MundialitoUtils, DSCacheFactory) {
    var gamesPromise = undefined;
    var openGamesPromise = undefined;
    var gamesManager = {
        _cacheManager: DSCacheFactory('GamesManager', { cacheFlushInterval : 1800000 }),
        _pool: {},
        _retrieveInstance: function(gameId, gameData) {
            var instance = this._pool[gameId];

            if (instance) {
                $log.debug('GamesManager: updating existing instance of game ' + gameId);
                instance.setData(gameData);
            } else {
                $log.debug('GamesManager: saving new instance of game ' + gameId);
                instance = new Game(gameData);
                this._pool[gameId] = instance;
            }
            instance.LoadTime = new Date();
            return instance;
        },
        _search: function(gameId) {
            $log.debug('GamesManager: will fetch game ' + gameId + ' from local pool');
            var instance = this._pool[gameId];
            if (angular.isDefined(instance) && MundialitoUtils.shouldRefreshInstance(instance)) {
                $log.debug('GamesManager: Instance was loaded at ' + instance,LoadTime + ', will reload it from server');
                return undefined;
            }
            return instance;
        },
        _load: function(gameId, deferred) {
            var scope = this;
            $log.debug('GamesManager: will fetch game ' + gameId + ' from server');
            $http.get('api/games/' + gameId, { tracker: 'getGame'})
                .success(function(gameData) {
                    var game = scope._retrieveInstance(gameData.GameId, gameData);
                    deferred.resolve(game);
                })
                .error(function() {
                    deferred.reject();
                });
        },

        /* Public Methods */

        clearGamesCache: function () {
            this._cacheManager.remove('api/games');
        },
        /* Use this function in order to get a new empty game data object */
        getEmptyGameObject: function() {
            return {
                HomeTeam: '',
                AwayTeam: '',
                Date: '',
                Stadium: ''
            };
        },

        /* Use this function in order to add a new game */
        addGame: function(gameData) {
            var deferred = $q.defer();
            if (!angular.isObject(gameData.AwayTeam)) {
                gameData.AwayTeam = angular.fromJson(gameData.AwayTeam);
            }
            if (!angular.isObject(gameData.HomeTeam)) {
                gameData.HomeTeam = angular.fromJson(gameData.HomeTeam);
            }
            if (!angular.isObject(gameData.Stadium)) {
                gameData.Stadium = angular.fromJson(gameData.Stadium);
            }
            var scope = this;
            $log.debug('GamesManager: will add new game - ' + angular.toJson(gameData));
            $http.post("api/games", gameData, { tracker: 'addGame' }).success(function(data) {
                var game = scope._retrieveInstance(data.GameId, data);
                deferred.resolve(game);
            })
            .error(function() {
                deferred.reject();
            });
            return deferred.promise;
        },

        /* Use this function in order to get a game instance by it's id */
        getGame: function(gameId,fresh) {
            var deferred = $q.defer();
            var game = undefined;
            if ((!angular.isDefined(fresh) || (!fresh))) {
                game = this._search(gameId);
            }
            if (game) {
                deferred.resolve(game);
            } else {
                this._load(gameId, deferred);
            }
            return deferred.promise;
        },

        /* Use this function in order to get instances of all the games */
        loadAllGames: function () {
            if (gamesPromise) {
                return gamesPromise;
            }
            var deferred = $q.defer();
            var scope = this;
            $log.debug('GamesManager: will fetch all games from server');
            $http.get('api/games', { tracker: 'getGames'})
                .success(function(gamesArray) {
                    var games = [];
                    gamesArray.forEach(function(gameData) {
                        var game = scope._retrieveInstance(gameData.GameId, gameData);
                        games.push(game);
                    });

                    deferred.resolve(games);
                })
                .error(function() {
                    deferred.reject();
                });
            gamesPromise = deferred.promise;
            return deferred.promise;
        },

        /* Use this function in order to get instances of all the open games */
        loadOpenGames: function () {
            if (openGamesPromise) {
                return openGamesPromise;
            }
            var deferred = $q.defer();
            var scope = this;
            $log.debug('GamesManager: will fetch all open games from server');
            $http.get('api/games/open', { tracker: 'getOpenGames'})
                .success(function(gamesArray) {
                    var games = [];
                    gamesArray.forEach(function(gameData) {
                        var game = scope._retrieveInstance(gameData.GameId, gameData);
                        games.push(game);
                    });

                    deferred.resolve(games);
                })
                .error(function() {
                    deferred.reject();
                });
            openGamesPromise = deferred.promise;
            return deferred.promise;
        },

        /* Use this function in order to get instances of all the games of a specific team */
        getTeamGames: function(teamId) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('GamesManager: will fetch all games of team ' + teamId + '  from server');
            $http.get('api/teams/' + teamId + '/games', { tracker: 'getTeamGames'})
                .success(function(gamesArray) {
                    var games = [];
                    gamesArray.forEach(function(gameData) {
                        var game = scope._retrieveInstance(gameData.GameId, gameData);
                        games.push(game);
                    });

                    deferred.resolve(games);
                })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        } ,

        /* Use this function in order to get instances of all the games in specific stadium */
        getStadiumGames: function(stadiumId) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('GamesManager: will fetch all games in stadium ' + stadiumId + '  from server');
            $http.get('api/games/Stadium/' + stadiumId, { tracker: 'getStadiumGames'})
                .success(function(gamesArray) {
                    var games = [];
                    gamesArray.forEach(function(gameData) {
                        var game = scope._retrieveInstance(gameData.GameId, gameData);
                        games.push(game);
                    });

                    deferred.resolve(games);
                })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        } ,

        /*  This function is useful when we got somehow the game data and we wish to store it or update the pool and get a game instance in return */
        setGame: function(gameData) {
            $log.debug('GamesManager: will set game ' + gameData.GameId + ' to -' + angular.toJson(gameData));
            var scope = this;
            var game = this._search(gameData.GameId);
            if (game) {
                game.setData(gameData);
            } else {
                game = scope._retrieveInstance(gameData.GameId,gameData);
            }
            return game;
        }

    };
    return gamesManager;
}]);

'use strict';
angular.module('mundialitoApp').directive('mundialitoGames',['Alert', function (Alert) {
    return {
        restrict: 'E',
        scope: {
            games: '=info',
            gamesType: '=filter',
            showOnly : '=',
            onAdd: '&'
        },
        templateUrl: 'App/Games/gamesTemplate.html',
        link : function($scope) {
            $scope.deleteGame = function(game) {
                var scope = game;
                game.delete().success(function() {
                    Alert.success('Game was deleted successfully');
                    $scope.games.splice($scope.games.indexOf(scope),1);
                });
            }
            $scope.matchGameType = function(gameType) {
                return function(item) {
                    if ((!gameType) || (gameType === "All")) {
                        return true;
                    }
                    return item.IsOpen;
                }
            }
        }
    };
}]);

'use strict';
angular.module('mundialitoApp').factory('Alert', ['toaster', '$log', '$rootScope', function (toaster, $log, $rootScope) {
    var service = {
        success: success,
        error: error,
        note: note
    };

    return service;


     function success(message) {
        toaster.pop('success', 'Success', message);
    };

     function error(message, title) {
        toaster.pop('error', title || 'Error', message);
    }

    function note(message) {
        toaster.pop('note', 'Info', message);
    }
}]);
'use strict';
angular.module('mundialitoApp').factory('ErrorHandler', ['$rootScope', '$log', 'Alert', '$location', 'Constants', function ($rootScope, $log, Alert, $location, Constants) {
    var ErrorHandler = this;

    ErrorHandler.handle = function (data, status) {
        $log.log(data);
        if (status === 401) {
            localStorage.removeItem('accessToken');
            sessionStorage.removeItem('accessToken');
            $location.path(Constants.LOGIN_PATH);
            return;
        }
        var message = [];
        var title = undefined;
        if (data.Message) {
            title = data.Message;
        }
        if (data.errors) {
            angular.forEach(data.errors, function (errors) {
                angular.forEach(errors, function (errors) {
                    message.push(errors);
                });
            });
        }
        if (data.ModelState) {
            angular.forEach(data.ModelState, function (errors) {
                message.push(errors);
            });
        }
        if (data.ExceptionMessage) {
            message.push(data.ExceptionMessage);
        }
        if (data.error_description) {
            message.push(data.error_description);
        }
        if (message.length === 0 && !title) {
            title = "General Error";
            message.push("Looks like the server is down, please try again in few minutes")
        }
        Alert.error(message.join('\n'), title);
    }

    return ErrorHandler;
}])
    .factory('myHttpInterceptor', ['ErrorHandler', '$q', function (ErrorHandler, $q) {
        return {
            response: function (response) {
                return response;
            },
            responseError: function (response) {
                ErrorHandler.handle(response.data, response.status, response.headers, response.config);

                // do something on error
                return $q.reject(response);
            }
        };
    }]);
angular.module('mundialitoApp').factory('MundialitoUtils', [ 'Constants', function (Constants) {

    var Utils = {
        shouldRefreshInstance : function(instance) {
            if (!angular.isDefined(instance.LoadTime) || !angular.isDate(instance.LoadTime)) {
                return false;
            }
            var now = new Date().getTime();
            return ((now - instance.LoadTime.getTime()) > Constants.REFRESH_TIME);
        }
    };

    return Utils;
}]);
'use strict';
angular.module('mundialitoApp').directive('accessLevel', ['$log','security', function ($log,Security) {
    return {
        restrict: 'A',
        link: function ($scope, element, attrs) {
            var prevDisp = element.css('display')
                , userRole = ""
                , accessLevel;

            
            $scope.$watch(
              function () {
                  return Security.user;
              },

              function (newValue) {
                  $scope.user = newValue;
                  if (($scope.user === undefined) || ($scope.user === null)) {
                      userRole = "User"
                  } else if ($scope.user.Roles) {
                      //$log.debug('Security.user has been changed:' + $scope.user.Username);
                      userRole = $scope.user.Roles;
                  } else {
                      userRole = "User"
                  }
                  updateCSS();
              },
              true
            );
           
            attrs.$observe('accessLevel', function (al) {
                if (al) accessLevel = al;
                updateCSS();
            });

            function updateCSS() {
                if (userRole && accessLevel) {
                    if (userRole === accessLevel)
                        element.css('display', prevDisp);
                    else
                        element.css('display', 'none');
                }
            }
        }
    };
}]);
'use strict';
angular.module('mundialitoApp').directive('activeNav', ['$location', function ($location) {
    return {
        restrict: 'A',
        link: function (scope, element) {
            var nestedA = element.find('a')[0];
            var path = nestedA.href;

            scope.location = $location;
            scope.$watch('location.absUrl()', function (newPath) {
                if (path === newPath) {
                    element.addClass('active');
                } else {
                    element.removeClass('active');
                }
            });
        }
    };
}]);
'use strict';
angular.module('mundialitoApp').directive('confirmPassword', [function () {
    return {
        restrict: 'A',
        require: 'ngModel',
        link: function (scope, element, attrs, ngModel) {
            ngModel.$parsers.unshift(function (viewValue, $scope) {
                var password = scope.$eval(attrs.confirmPassword);
                var noMatch = viewValue != password;
                ngModel.$setValidity('noMatch', !noMatch);
                return viewValue;
            });
        }
    }
}]);
'use strict';
angular.module('mundialitoApp').directive('mundialitoToggleText', [function () {
    function link(scope, element, attrs) {
        var state;
        scope.$watch(attrs.varieble, function (value) {
            state = value;
            updateText();
        });

        function updateText() {
            var text = state == true ? attrs.trueLabel : attrs.falseLabel;
            element.text(text);
        }
    }
    return {
        link: link
    };
}]);
'use strict';
angular.module('mundialitoApp').factory('GeneralBet', ['$http','$log', function($http,$log) {
    function GeneralBet(betData) {
        if (betData) {
            this.setData(betData);
        }
        // Some other initializations related to general bet
    };

    GeneralBet.prototype = {
        setData: function(betData) {
            angular.extend(this, betData);
        },
        update: function() {
            $log.debug('General Bet: Will update general bet ' + this.GeneralBetId);
            return $http.put('api/generalbets/' + this.GeneralBetId, this, { tracker: 'updateGeneralBet' });
        },
        resolve: function() {
            $log.debug('General Bet: Will resolve general bet ' + this.GeneralBetId);
            var data = {
                TeamIsRight: this.TeamIsRight || false,
                PlayerIsRight: this.PlayerIsRight || false
            };
            return $http.put('api/generalbets/' + this.GeneralBetId + '/resolve', data, { tracker: 'resolveGeneralBet' });
        }
    };
    return GeneralBet;
}]);

'use strict';
angular.module('mundialitoApp').factory('GeneralBetsManager', ['$http', '$q', 'GeneralBet','$log','MundialitoUtils', function($http,$q,GeneralBet,$log,MundialitoUtils) {
    var generalBetsManager = {
        _pool: {},
        _retrieveInstance: function(betId, betData) {
            var instance = this._pool[betId];

            if (instance) {
                $log.debug('GeneralBetsManager: updating existing instance of bet ' + betId);
                instance.setData(betData);
            } else {
                $log.debug('GeneralBetsManager: saving new instance of bet ' + betId);
                instance = new GeneralBet(betData);
                this._pool[betId] = instance;
            }
            instance.LoadTime = new Date();
            return instance;
        },
        _search: function(betId) {
            $log.debug('GeneralBetsManager: will fetch bet ' + betId + ' from local pool');
            var instance = this._pool[betId];
            if (angular.isDefined(instance) && MundialitoUtils.shouldRefreshInstance(instance)) {
                $log.debug('GeneralBetsManager: Instance was loaded at ' + instance,LoadTime + ', will reload it from server');
                return undefined;
            }
            return instance;
        },
        _load: function(betId, deferred) {
            var scope = this;
            $log.debug('GeneralBetsManager: will fetch bet ' + betId + ' from server');
            $http.get('api/generalbets/' + betId, { tracker: 'getGeneralBet' })
                .success(function(betData) {
                    var bet = scope._retrieveInstance(betData.GeneralBetId, betData);
                    deferred.resolve(bet);
                })
                .error(function() {
                    deferred.reject();
                });
        },

        /* Public Methods */
        /* Use this function in order to add a new general bet */
        addGeneralBet: function(betData) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('GeneralBetsManager: will add new bet - ' + angular.toJson(betData));
            $http.post('api/generalbets/', betData, { tracker: 'addGeneralBet' }).success(function(data) {
                var bet = scope._retrieveInstance(data.GeneralBetId, data);
                deferred.resolve(bet);
            })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        /* Use this function in order to get a general bet instance by it's id */
        getGeneralBet: function(betId,fresh) {
            var deferred = $q.defer();
            var bet = undefined;
            if ((!angular.isDefined(fresh) || (!fresh))) {
                bet = this._search(betId);
            }
            if (bet) {
                deferred.resolve(bet);
            } else {
                this._load(betId, deferred);
            }
            return deferred.promise;
        },

        /* Use this function in order to get instances of all the general bets */
        loadAllGeneralBets: function() {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('GeneralBetsManager: will fetch all general bets from server');
            $http.get('api/generalbets', { tracker: 'getGeneralBets' })
                .success(function(gamesArray) {
                    var generalBets = [];
                    gamesArray.forEach(function(betData) {
                        var bet = scope._retrieveInstance(betData.GeneralBetId, betData);
                        generalBets.push(bet);
                    });

                    deferred.resolve(generalBets);
                })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        hasGeneralBet : function(username) {
            var deferred = $q.defer();
            $log.debug('GeneralBetsManager: will check if user ' + username + ' has general bets');
            $http.get('api/generalbets/has-bet/' + username, { tracker: 'getUserGeneralBet' })
                .success(function(answer) {
                    deferred.resolve(answer);
                })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        canSubmtiGeneralBet : function() {
            var deferred = $q.defer();
            $log.debug('GeneralBetsManager: will check if user general bets are closed');
            $http.get('api/generalbets/cansubmitbets/', { tracker: 'getCanSubmitGeneralBets' })
                .success(function(answer) {
                    deferred.resolve(answer);
                })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        /* Use this function in order to get a general bet instance by it's owner username */
        getUserGeneralBet: function(username) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('GeneralBetsManager: will fetch user ' + username +  ' general bet from server');
            $http.get('api/generalbets/user/' + username, { tracker: 'getUserGeneralBet' })
                .success(function(betData) {
                    var bet = scope._retrieveInstance(betData.GeneralBetId, betData);
                    deferred.resolve(bet);
                })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        /*  This function is useful when we got somehow the bet data and we wish to store it or update the pool and get a general bet instance in return */
        setGeneralBet: function(betData) {
            $log.debug('GeneralBetsManager: will set bet ' + betData.GeneralBetId + ' to -' + angular.toJson(betData));
            var scope = this;
            var bet = this._search(betData.GeneralBetId);
            if (bet) {
                bet.setData(betData);
            } else {
                bet = scope._retrieveInstance(betData.GeneralBetId,betData);
            }
            return bet;
        }
    };
    return generalBetsManager;
}]);

'use strict';
angular.module('mundialitoApp').factory('Player', ['$http', '$log', function ($http, $log) {
    function Player(playerData) {
        if (playerData) {
            this.setData(playerData);
        }
        // Some other initializations related to stadium
    };

    Player.prototype = {
        setData: function (playerData) {
            angular.extend(this, playerData);
        }
    };
    return Player;
}]);

'use strict';
angular.module('mundialitoApp').factory('PlayersManager', ['$http', '$q', 'Player', '$log', function ($http, $q, Player, $log) {
    var playersPromise = undefined;
    var playersManager = {
        _pool: {},
        _retrieveInstance: function (playerId, playerData) {
            var instance = this._pool[playerId];

            if (instance) {
                $log.debug('playersPromise: updating existing instance of player ' + playerId);
                instance.setData(playerData);
            } else {
                $log.debug('playersPromise: saving new instance of player ' + playerId);
                instance = new Player(playerData);
                this._pool[playerId] = instance;
            }
            instance.LoadTime = new Date();
            return instance;
        },

        /* Public Methods */

        getPlayerSchema: function () {
            return [
                { property: 'Name', label: 'Name', type: 'text', attr: { required: true } }
            ];
        },

        /* Use this function in order to get instances of all the players */
        loadAllPlayers: function () {
            if (playersPromise) {
                return playersPromise;
            }
            var deferred = $q.defer();
            var scope = this;
            $log.debug('PlayersManager: will fetch all players from server');
            $http.get("api/players", { tracker: 'getPlayers', cache: true })
                .success(function (playersArray) {
                    var players = [];
                    playersArray.forEach(function (playerData) {
                        var player = scope._retrieveInstance(playerData.PlayerId, playerData);
                        players.push(player);
                    });
                    deferred.resolve(players);
                })
                .error(function () {
                    deferred.reject();
                });
            playersPromise = deferred.promise;
            return deferred.promise;
        },

    };
    return playersManager;
}]);

'use strict';
angular.module('mundialitoApp').factory('Stadium', ['$http','$log', function($http,$log) {
    function Stadium(stadiumData) {
        if (stadiumData) {
            this.setData(stadiumData);
        }
        // Some other initializations related to stadium
    };

    Stadium.prototype = {
        setData: function(stadiumData) {
            angular.extend(this, stadiumData);
        },
        delete: function() {
            if (confirm('Are you sure you would like to delete stadium ' + this.Name)) {
                $log.debug('Stadium: Will delete stadium ' + this.StadiumId)
                return $http.delete("api/stadiums/" + this.StadiumId, { tracker: 'deleteStadium' });
            }
        },
        update: function() {
            $log.debug('Stadium: Will update stadium ' + this.StadiumId)
            var stadiumToUpdate = {};
            angular.copy(this,stadiumToUpdate);
            delete stadiumToUpdate.Games;
            return $http.put("api/stadiums/" + this.StadiumId, stadiumToUpdate, { tracker: 'editStadium' });
        },
        getUrl: function() {
            return '/stadiums/' + this.StadiumId;
        }
    };
    return Stadium;
}]);

'use strict';
angular.module('mundialitoApp').controller('StadiumCtrl', ['$scope', '$log', 'StadiumsManager', 'GamesManager', 'stadium', 'Alert', function ($scope, $log, StadiumsManager, GamesManager, stadium, Alert) {
    $scope.stadium = stadium;
    $scope.showEditForm = false;

    GamesManager.getStadiumGames($scope.stadium.StadiumId).then(function(data) {
        $log.debug('StadiumCtrl: Got games of stadium');
        $scope.games = data;
    });

    $scope.updateStadium = function() {
        $scope.stadium.update().success(function() {
            Alert.success('Stadium was updated successfully');
        })
    };

    $scope.schema =  StadiumsManager.getStaidumSchema();
}]);
'use strict';
angular.module('mundialitoApp').controller('StadiumsCtrl', ['$scope', '$log', 'StadiumsManager', 'stadiums', 'Alert', function ($scope, $log, StadiumsManager, stadiums, Alert) {
    $scope.stadiums = stadiums;
    $scope.showNewStadium = false;
    $scope.newStadium = null;

    $scope.addNewStadium = function () {
        $scope.newStadium = StadiumsManager.getEmptyStadiumObject();
    };

    $scope.saveNewStadium = function() {
        StadiumsManager.addStadium($scope.newStadium).then(function(data) {
            Alert.success('Stadium was added successfully');
            $scope.newStadium = null;
            $scope.stadiums.push(data);
        });
    };

    $scope.deleteStadium = function(stadium) {
        var scope = stadium;
        stadium.delete().success(function() {
            Alert.success('Stadium was deleted successfully');
            $scope.stadiums.splice($scope.stadiums.indexOf(scope),1);
        })
    };

    $scope.schema =  StadiumsManager.getStaidumSchema();
}]);
'use strict';
angular.module('mundialitoApp').factory('StadiumsManager', ['$http', '$q', 'Stadium', '$log', 'MundialitoUtils', function ($http, $q, Stadium, $log, MundialitoUtils) {
    var stadiumsPromise = undefined;
    var stadiumsManager = {
        _pool: {},
        _retrieveInstance: function(stadiumId, stadiumData) {
            var instance = this._pool[stadiumId];

            if (instance) {
                $log.debug('StadiumsManager: updating existing instance of stadium ' + stadiumId);
                instance.setData(stadiumData);
            } else {
                $log.debug('StadiumsManager: saving new instance of stadium ' + stadiumId);
                instance = new Stadium(stadiumData);
                this._pool[stadiumId] = instance;
            }
            instance.LoadTime = new Date();
            return instance;
        },
        _search: function(stadiumId) {
            $log.debug('StadiumsManager: will fetch stadium ' + stadiumId + ' from local pool');
            var instance = this._pool[stadiumId];
            if (angular.isDefined(instance) && MundialitoUtils.shouldRefreshInstance(instance)) {
                $log.debug('StadiumsManager: Instance was loaded at ' + instance,LoadTime + ', will reload it from server');
                return undefined;
            }
            return instance;
        },
        _load: function(stadiumId, deferred) {
            var scope = this;
            $log.debug('StadiumsManager: will fetch stadium ' + stadiumId + ' from server');
            $http.get('api/stadiums/' + stadiumId, { tracker: 'getStadium' })
                .success(function(stadiumData) {
                    var stadium = scope._retrieveInstance(stadiumData.StadiumId, stadiumData);
                    deferred.resolve(stadium);
                })
                .error(function() {
                    deferred.reject();
                });
        },

        /* Public Methods */

        getStaidumSchema: function() {
            return [
                { property: 'Name', label: 'Name', type: 'text', attr: { required: true } },
                { property: 'City', label: 'City', type: 'text', attr: { required: true } },
                { property: 'Capacity', label: 'Capacity', type: 'number', attr: { required: true } }
            ];
        },

        /* Use this function in order to get a new empty stadium data object */
        getEmptyStadiumObject: function() {
            return {
                HomeTeam: '',
                AwayTeam: ''
            };
        },

        /* Use this function in order to add a new stadium */
        addStadium: function(stadiumData) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('StadiumsManager: will add new stadium - ' + angular.toJson(stadiumData));
            $http.post("api/stadiums", stadiumData, { tracker: 'addStadium' }).success(function(data) {
                var stadium = scope._retrieveInstance(data.StadiumId, data);
                deferred.resolve(stadium);
            })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        /* Use this function in order to get a stadium instance by it's id */
        getStadium: function(stadiumId,fresh) {
            var deferred = $q.defer();
            var stadium = undefined;
            if ((!angular.isDefined(fresh) || (!fresh))) {
                stadium = this._search(stadiumId);
            }
            if (stadium) {
                deferred.resolve(stadium);
            } else {
                this._load(stadiumId, deferred);
            }
            return deferred.promise;
        },

        /* Use this function in order to get instances of all the stadiums */
        loadAllStadiums: function () {
            if (stadiumsPromise) {
                return stadiumsPromise;
            }
            var deferred = $q.defer();
            var scope = this;
            $log.debug('StadiumsManager: will fetch all games from server');
            $http.get("api/stadiums", { tracker: 'getStadiums', cache: true })
                .success(function(stadiumsArray) {
                    var stadiums = [];
                    stadiumsArray.forEach(function(stadiumData) {
                        var stadium = scope._retrieveInstance(stadiumData.StadiumId, stadiumData);
                        stadiums.push(stadium);
                    });
                    deferred.resolve(stadiums);
                })
                .error(function() {
                    deferred.reject();
                });
            stadiumsPromise = deferred.promise;
            return deferred.promise;
        },

        /*  This function is useful when we got somehow the stadium data and we wish to store it or update the pool and get a stadium instance in return */
        setStadium: function(stadiumData) {
            $log.debug('StadiumsManager: will set stadium ' + stadiumData.StadiumId + ' to -' + angular.toJson(stadiumData));
            var scope = this;
            var stadium = this._search(stadiumData.StadiumId);
            if (stadium) {
                stadium.setData(stadiumData);
            } else {
                stadium = scope._retrieveInstance(stadiumData.StadiumId,stadiumData);
            }
            return stadium;
        }
    };
    return stadiumsManager;
}]);

angular.module('mundialitoApp').factory('Team', ['$http','$log', function($http,$log) {
    function Team(teamData) {
        if (teamData) {
            this.setData(teamData);
        }
        // Some other initializations related to game
    };

    Team.prototype = {
        setData: function(teamData) {
            angular.extend(this, teamData);
            this.Logo = this.Logo.toLowerCase();
            this.Flag = this.Flag.toLowerCase();
        },
        delete: function() {
            if (confirm('Are you sure you would like to delete team ' + this.Name)) {
                $log.debug('Team: Will delete team ' + this.TeamId)
                return $http.delete("api/teams/" + this.TeamId, { tracker: 'deleteTeam'});
            }
        },
        update: function() {
            $log.debug('Team: Will update game ' + this.TeamId)
            return $http.put("api/teams/" + this.TeamId, this, { tracker: 'editTeam'});
        },
        getUrl: function() {
            return '/teams/' + this.TeamId;
        }
    };
    return Team;
}]);

'use strict';
angular.module('mundialitoApp').controller('TeamCtrl', ['$scope', '$log', 'TeamsManager', 'team', 'games', 'Alert', function ($scope, $log, TeamsManager, team, games, Alert) {
    $scope.team = team;
    $scope.games = games;
    $scope.showEditForm = false;

    $scope.updateTeam = function() {
        $scope.team.update().success(function(data) {
            Alert.success('Team was updated successfully');
            TeamsManager.setTeam(data);
        })
    };

    $scope.schema =  TeamsManager.getTeamSchema();
    
}]);
'use strict';
angular.module('mundialitoApp').controller('TeamsCtrl', ['$scope', '$log', 'TeamsManager', 'teams', 'Alert', function ($scope, $log, TeamsManager, teams, Alert) {
    $scope.teams = teams;
    $scope.showNewTeam = false;
    $scope.newTeam = null;

    $scope.addNewTeam = function () {
        $scope.newTeam = TeamsManager.getEmptyTeamObject();
    };

    $scope.saveNewTeam = function() {
        TeamsManager.addTeam($scope.newTeam).then(function(data) {
            Alert.success('Team was added successfully');
            $scope.newTeam = null;
            $scope.teams.push(data);
        });
    };

    $scope.deleteTeam = function(team) {
        var scope = team;
        team.delete().success(function() {
            Alert.success('Team was deleted successfully');
            $scope.teams.splice($scope.teams.indexOf(scope),1);
        })
    };

    $scope.schema =  TeamsManager.getTeamSchema();
}]);
angular.module('mundialitoApp').factory('TeamsManager', ['$http', '$q', 'Team','$log','MundialitoUtils', function($http,$q,Team,$log,MundialitoUtils) {
    var teamsManager = {
        _pool: {},
        _retrieveInstance: function(teamId, teamData) {
            var instance = this._pool[teamId];

            if (instance) {
                $log.debug('TeamsManager: updating existing instance of team ' + teamId);
                instance.setData(teamData);
            } else {
                $log.debug('TeamsManager: saving new instance of team ' + teamId);
                instance = new Team(teamData);
                this._pool[teamId] = instance;
            }
            instance.LoadTime = new Date();
            return instance;
        },
        _search: function(teamId) {
            $log.debug('TeamsManager: will fetch team ' + teamId + ' from local pool');
            var instance = this._pool[teamId];
            if (angular.isDefined(instance) && MundialitoUtils.shouldRefreshInstance(instance)) {
                $log.debug('TeamsManager: Instance was loaded at ' + instance,LoadTime + ', will reload it from server');
                return undefined;
            }
            return instance;
        },
        _load: function(teamId, deferred) {
            var scope = this;
            $log.debug('TeamsManager: will fetch team ' + teamId + ' from server');
            $http.get("api/teams/" + teamId, { tracker: 'getTeam'})
                .success(function(teamData) {
                    var team = scope._retrieveInstance(teamData.TeamId, teamData);
                    deferred.resolve(team);
                })
                .error(function() {
                    deferred.reject();
                });
        },

        /* Public Methods */

        getTeamSchema: function() {
            return [
                { property: 'Name', label: 'Name', type: 'text', attr: { required: true } },
                { property: 'Flag', label: 'Flag', type: 'url', attr: { required: true } },
                { property: 'Logo', label: 'Logo', type: 'url', attr: { required: true } },
                { property: 'ShortName', label: 'Short Name', type: 'text', attr: { ngMaxlength: 3, ngMinlength: 3, required: true } },
            ];
        },

        /* Use this function in order to get a new empty team data object */
        getEmptyTeamObject: function() {
            return {
                Name: '',
                Flag: '',
                Logo: '',
                ShortName: ''
            }
        },

        /* Use this function in order to add a new team */
        addTeam: function(teamData) {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('TeamsManager: will add new team - ' + angular.toJson(teamData));
            $http.post("api/teams", teamData, {  tracker: 'addTeam'})
                .success(function(data) {
                    var team = scope._retrieveInstance(data.TeamId, data);
                    deferred.resolve(team);
                })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        /* Use this function in order to get a team instance by it's id */
        getTeam: function(teamId,fresh) {
            var deferred = $q.defer();
            var team = undefined;
            if ((!angular.isDefined(fresh) || (!fresh))) {
                team = this._search(teamId);
            }
            if (team) {
                deferred.resolve(team);
            } else {
                this._load(teamId, deferred);
            }
            return deferred.promise;
        },

        /* Use this function in order to get instances of all the teams */
        loadAllTeams: function() {
            var deferred = $q.defer();
            var scope = this;
            $log.debug('TeamsManager: will fetch all teams from server');
            $http.get("api/teams", { tracker: 'getTeams', cache: true })
                .success(function(teamsArray) {
                    var teams = [];
                    teamsArray.forEach(function(teamData) {
                        var team = scope._retrieveInstance(teamData.TeamId, teamData);
                        teams.push(team);
                    });

                    deferred.resolve(teams);
                })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        /*  This function is useful when we got somehow the team data and we wish to store it or update the pool and get a team instance in return */
        setTeam: function(teamData) {
            $log.debug('TeamsManager: will set team ' + teamData.TeamId + ' to -' + angular.toJson(teamData));
            var scope = this;
            var team = this._search(teamData.TeamId);
            if (team) {
                team.setData(teamData);
            } else {
                team = scope._retrieveInstance(teamData.TeamId,teamData);
            }
            return team;
        }

    };
    return teamsManager;
}]);

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

    $scope.deleteUser = function(user) {
        var scope = user;
        user.delete().success(function () {
            Alert.success('User was deleted successfully');
            $scope.users.splice($scope.users.indexOf(scope), 1);
        })
    };

    $scope.resolveBet = function(bet) {
        bet.resolve().success(function() {
            Alert.success('General bet was resolved successfully');
        });
    };

    $scope.generateKey = function() {
        $scope.privateKey.key = '';
        UsersManager.generatePrivateKey($scope.privateKey.email).then(function(data) {
            $log.debug('ManageAppCtrl: got private key ' + data);
            $scope.privateKey.key = data;
        });
    };

    $scope.makeAdmin = function(user) {
        user.makeAdmin().success(function () {
            Alert.success('User was is now admin');
            user.IsAdmin = true;
        })
    };

}]);
'use strict';
angular.module('mundialitoApp').factory('User', ['$http','$log', function($http, $log) {
    function User(userData) {
        if (userData) {
            this.setData(userData);
        }
        // Some other initializations related to user
    };

    User.prototype = {
        setData: function(userData) {
            angular.extend(this, userData);
        },
        getUrl: function() {
            return '/users/' + this.Username;
        },
        delete: function() {
            if (confirm('Are you sure you would like to delete user ' + this.Username)) {
                $log.debug('User: Will delete user ' + this.Username)
                return $http.delete("api/users/" + this.Id, { tracker: 'deleteUser' });
            }
        },
        makeAdmin :function() {
            if (confirm('Are you sure you would like to make ' + this.Name + ' Admin?')) {
                $log.debug('User: Will make user ' + this.Username + ' admin')
                return $http.post("api/users/makeadmin/" + this.Id, { tracker: 'makeAdmin' });
            }
        }
    };
    return User;
}]);

'use strict';
angular.module('mundialitoApp').controller('UserProfileCtrl', ['$scope', '$log', '$routeParams', 'Alert', 'GeneralBetsManager', 'profileUser', 'userGameBets', 'teams', 'generalBetsAreOpen', 'players', function ($scope, $log, $routeParams, Alert, GeneralBetsManager, profileUser, userGameBets, teams, generalBetsAreOpen, players) {
    $scope.profileUser = profileUser;
    $scope.userGameBets = userGameBets;
    $scope.teams = teams;
    $scope.players = players;
    $scope.noGeneralBetWasSubmitted = false;
    $scope.generalBetsAreOpen = (generalBetsAreOpen === true);
    $log.debug('UserProfileCtrl: generalBetsAreOpen = ' + generalBetsAreOpen);

    $scope.isLoggedUserProfile = function() {
        var res = ($scope.security.user != null) && ($scope.security.user.Username === $scope.profileUser.Username);
        $log.debug('UserProfileCtrl: isLoggedUserProfile = ' + res);
        return ($scope.security.user != null) && ($scope.security.user.Username === $scope.profileUser.Username);
    };

    $scope.isGeneralBetClosed = function() {
        var res = !$scope.generalBetsAreOpen;
        $log.debug('UserProfileCtrl: isGeneralBetClosed = ' + res);
        return res;
    };

    $scope.isGeneralBetReadOnly = function() {
        var res = (!$scope.isLoggedUserProfile() || ($scope.isGeneralBetClosed()));
        $log.debug('UserProfileCtrl: isGeneralBetReadOnly = ' + res);
        return res;
    }

    $scope.shoudLoadGeneralBet = function() {
        var res = ($scope.isLoggedUserProfile() || ($scope.isGeneralBetClosed()));
        $log.debug('UserProfileCtrl: shoudLoadGeneralBet = ' + res);
        return res;
    }

    if ($scope.shoudLoadGeneralBet()) {
        GeneralBetsManager.hasGeneralBet($scope.profileUser.Username).then(function (answer) {
            $log.debug('UserProfileCtrl: hasGeneralBet = ' + answer);
            if (answer === true) {
                GeneralBetsManager.getUserGeneralBet($scope.profileUser.Username).then(function (generalBet) {
                    $log.debug('UserProfileCtrl: got user general bet - ' + angular.toJson(generalBet));
                    $scope.generalBet = generalBet
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

    $scope.saveGeneralBet = function() {
        if (angular.isDefined($scope.generalBet.GeneralBetId))
        {
            $scope.generalBet.update().then(function() {
                Alert.success('General Bet was updated successfully');
            }, function () {
                Alert.error('Failed to update General Bet, please try again');
            });
        }
        else
        {
            GeneralBetsManager.addGeneralBet($scope.generalBet).then(function(data) {
                $log.log('UserProfileCtrl: General Bet ' + data.GeneralBetId + ' was added');
                $scope.generalBet = data;
                Alert.success('General Bet was added successfully');
            }, function () {
                Alert.error('Failed to add General Bet, please try again');
            });
        }
    };


}]);

'use strict';
angular.module('mundialitoApp').factory('UsersManager', ['$http', '$q', 'User', '$log', 'MundialitoUtils', 'DSCacheFactory', function ($http, $q, User, $log, MundialitoUtils, DSCacheFactory) {
    var usersPromise = undefined;
    var usersManager = {
        _cacheManager: DSCacheFactory('UsersManager', { cacheFlushInterval : 1800000 }),
        _pool: {},
        _retrieveInstance: function(username, userData) {
            var instance = this._pool[username];

            if (instance) {
                $log.debug('UsersManager: updating existing instance of user ' + username);
                instance.setData(userData);
            } else {
                $log.debug('UsersManager: saving new instance of user ' + username);
                instance = new User(userData);
                this._pool[username] = instance;
            }
            instance.LoadTime = new Date();
            return instance;
        },
        _search: function(username) {
            $log.debug('UsersManager: will fetch user ' + username + ' from local pool');
            var instance = this._pool[username];
            if (angular.isDefined(instance) && MundialitoUtils.shouldRefreshInstance(instance)) {
                $log.debug('UsersManager: Instance was loaded at ' + instance,LoadTime + ', will reload it from server');
                return undefined;
            }
            return instance;
        },
        _load: function(username, deferred) {
            var scope = this;
            $log.debug('UsersManager: will fetch user ' + username + ' from server');
            $http.get('api/users/' + username, { tracker: 'getUser'})
                .success(function(userData) {
                    var user = scope._retrieveInstance(userData.Username, userData);
                    deferred.resolve(user);
                })
                .error(function() {
                    deferred.reject();
                });
        },

        /* Public Methods */
        /* Use this function in order to get a user instance by it's id */
        getUser: function(username,fresh) {
            var deferred = $q.defer();
            var user = undefined;
            if ((!angular.isDefined(fresh) || (!fresh))) {
                user = this._search(username);
            }
            if (user) {
                deferred.resolve(user);
            } else {
                this._load(username, deferred);
            }
            return deferred.promise;
        },

        generatePrivateKey : function(email) {
            var deferred = $q.defer();
            $log.debug('UsersManager: will generate private key for ' + email);
            $http.get('api/users/generateprivatekey/' + encodeURIComponent(email) + '/', { tracker: 'generatePrivateKey' })
                .success(function(answer) {
                    deferred.resolve(answer);
                })
                .error(function() {
                    deferred.reject();
                });
            return deferred.promise;
        },

        getTable: function() {
            var scope = this;
            $log.debug('UsersManager: will fetch table from server');
            return $http.get('api/users/table', { tracker: 'getUsers'})
                .then(function (usersArray) {
                    var users = [];
                    usersArray.data.forEach(function (userData) {
                        var user = scope._retrieveInstance(userData.Username, userData);
                        users.push(user);
                    });
                    return users;
                });
        },

        /* Use this function in order to get instances of all the users */
        loadAllUsers: function () {
            if (usersPromise) {
                return usersPromise;
            }
            var deferred = $q.defer();
            var scope = this;
            $log.debug('UsersManager: will fetch all users from server');
            $http.get('api/users', { tracker: 'getUsers'})
                .success(function(usersArray) {
                    var users = [];
                    usersArray.forEach(function(userData) {
                        var user = scope._retrieveInstance(userData.Username, userData);
                        users.push(user);
                    });

                    deferred.resolve(users);
                })
                .error(function() {
                    deferred.reject();
                });
            usersPromise = deferred.promise;
            return deferred.promise;
        },

        /*  This function is useful when we got somehow the user data and we wish to store it or update the pool and get a user instance in return */
        setUser: function(userData) {
            $log.debug('UsersManager: will set user ' + userData.Username + ' to -' + angular.toJson(userData));
            var scope = this;
            var user = this._search(userData.Username);
            if (user) {
                user.setData(userData);
            } else {
                user = scope._retrieveInstance(userData.Username,userData);
            }
            return user;
        }

    };
    return usersManager;
}]);
