angular.module('mundialitoApp', ['key-value-editor', 'security', 'ngSanitize', 'ngRoute', 'ngAnimate', 'ui.bootstrap', 'autofields.bootstrap', 'cgBusy', 'ajoslin.promise-tracker', 'ui.select',
    'ui.bootstrap.datetimepicker', 'ui.grid', 'ui.grid.autoResize', 'googlechart', 'angular-data.DSCacheFactory', 'toaster', 'ui.grid.saveState', 'ui.grid.resizeColumns'])
    .value('cgBusyTemplateName', 'App/Partials/angular-busy.html')
    .config(['$routeProvider', '$httpProvider', '$locationProvider', '$parseProvider', 'securityProvider', 'Constants', function ($routeProvider, $httpProvider, $locationProvider, $parseProvider, securityProvider, Constants) {
        $locationProvider.html5Mode(true);
        $httpProvider.defaults.headers.common['X-Requested-With'] = 'XMLHttpRequest';
        $httpProvider.interceptors.push('myHttpInterceptor');
        securityProvider.urls.login = Constants.LOGIN_PATH;
        securityProvider.usePopups = false;

        $routeProvider.
            when('/', {
                templateUrl: 'App/Dashboard/Dashboard.html',
                controller: 'DashboardCtrl',
                resolve: {
                    teams: ['TeamsManager', (TeamsManager) => TeamsManager.loadAllTeams()],
                    players: ['PlayersManager', (PlayersManager) => PlayersManager.loadAllPlayers()]
                }
            }).
            when('/bets_center', {
                templateUrl: 'App/Bets/BetsCenter.html',
                controller: 'BetsCenterCtrl',
                resolve: {
                    games: ['GamesManager', (GamesManager) => GamesManager.loadOpenGames()]
                }
            }).
            when('/users/:username', {
                templateUrl: 'App/Users/UserProfile.html',
                controller: 'UserProfileCtrl',
                resolve: {
                    profileUser: ['$route', 'UsersManager', ($route, UsersManager) => {
                        var username = $route.current.params.username;
                        return UsersManager.getUser(username, true);
                    }],
                    userGameBets: ['$route', 'BetsManager', ($route, BetsManager) => {
                        var username = $route.current.params.username;
                        return BetsManager.getUserBets(username);
                    }],
                    teams: ['TeamsManager', (TeamsManager) => TeamsManager.loadAllTeams()],
                    generalBetsAreOpen: ['GeneralBetsManager', (GeneralBetsManager) => GeneralBetsManager.canSubmtiGeneralBet()],
                    players: ['PlayersManager', (PlayersManager) => PlayersManager.loadAllPlayers()],
                }
            }).
            when('/manage_users', {
                templateUrl: 'App/Users/ManageApp.html',
                controller: 'ManageAppCtrl',
                resolve: {
                    users: ['UsersManager', (UsersManager) => UsersManager.loadAllUsers()],
                    teams: ['TeamsManager', (TeamsManager) => TeamsManager.loadAllTeams()],
                    generalBets: ['GeneralBetsManager', (GeneralBetsManager) => GeneralBetsManager.loadAllGeneralBets()],
                    players: ['PlayersManager', (PlayersManager) => PlayersManager.loadAllPlayers()]
                }
            }).
            when('/teams', {
                templateUrl: 'App/Teams/Teams.html',
                controller: 'TeamsCtrl',
                resolve: {
                    teams: ['TeamsManager', (TeamsManager) => TeamsManager.loadAllTeams()]
                }
            }).
            when('/teams/:teamId', {
                templateUrl: 'App/Teams/Team.html',
                controller: 'TeamCtrl',
                resolve: {
                    team: ['$route', 'TeamsManager', ($route, TeamsManager) => {
                            var teamId = $route.current.params.teamId;
                            return TeamsManager.getTeam(teamId);
                        }],
                    games: ['$route', 'GamesManager', ($route, GamesManager) => {
                            var teamId = $route.current.params.teamId;
                            return GamesManager.getTeamGames(teamId);
                        }]
                }
            }).
            when('/games/:gameId', {
                templateUrl: 'App/Games/Game.html',
                controller: 'GameCtrl',
                resolve: {
                    game: ['$route', 'GamesManager', ($route, GamesManager) => {
                            var gameId = $route.current.params.gameId;
                            return GamesManager.getGame(gameId);
                        }],
                    userBet: ['$route', 'BetsManager', ($route, BetsManager) => {
                            var gameId = $route.current.params.gameId;
                            return BetsManager.getUserBetOnGame(gameId);
                        }]
                }
            }).
            when('/games', {
                templateUrl: 'App/Games/Games.html',
                controller: 'GamesCtrl',
                resolve: {
                    games: ['GamesManager', (GamesManager) => GamesManager.loadAllGames()],
                    teams: ['TeamsManager', (TeamsManager) => TeamsManager.loadAllTeams()]
                }
            }).
            when('/stadiums/:stadiumId', {
                templateUrl: 'App/Stadiums/Stadium.html',
                controller: 'StadiumCtrl',
                resolve: {
                    stadium: ['$q', '$route', 'StadiumsManager', (_$q, $route, StadiumsManager) => {
                            var stadiumId = $route.current.params.stadiumId;
                            return StadiumsManager.getStadium(stadiumId, true);
                        }]
                }
            }).
            when('/stadiums', {
                templateUrl: 'App/Stadiums/Stadiums.html',
                controller: 'StadiumsCtrl',
                resolve: {
                    stadiums: ['StadiumsManager', (StadiumsManager) => StadiumsManager.loadAllStadiums()]
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
    .run(['$rootScope', '$log', 'security', '$route', '$location', 'PluginsProvider', 'FootballDataGamePlugin', 'FootballDataTeamStatsPlugin', function ($rootScope, $log, security, $route, $location, PluginsProvider, FootballDataGamePlugin, FootballDataTeamStatsPlugin) {
        PluginsProvider.registerGameFactory(FootballDataGamePlugin);
        PluginsProvider.registerTeamFactory(FootballDataTeamStatsPlugin);
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