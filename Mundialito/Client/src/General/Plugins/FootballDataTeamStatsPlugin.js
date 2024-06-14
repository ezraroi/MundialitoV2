angular.module('mundialitoApp')
    .factory('FootballDataTeamStatsPlugin', ['$q', '$rootScope', 'GenericProxyService', function ($q, $rootScope, GenericProxyService) {
        var baseUrl = 'https://api.football-data.org/v4/teams/';
        const integrationKey = 'football-data'

        function calcAge(dateOfBirth) {
            return (new Date().getFullYear()) - parseInt(dateOfBirth.substring(0, 4), 10);
        }

        function getTeamDetails(team) {
            var url = baseUrl + team.IntegrationsData[integrationKey];
            if ((!$rootScope.mundialitoApp.clientConfig) || (!$rootScope.mundialitoApp.clientConfig['football-data-api-key'])) {
                return $q.reject('Skipping football-data as no api key provided');
            }
            return GenericProxyService.proxyRequest('GET', url, undefined, {
                'X-Auth-Token': $rootScope.mundialitoApp.clientConfig['football-data-api-key']
            }).then((response) => {
                response.coach.age = calcAge(response.coach.dateOfBirth);
                response.squad.forEach(player => {
                    player.age = calcAge(player.dateOfBirth);
                    player.icon = player.position.toLowerCase() === 'goalkeeper' ? 'goalkeeper' : 'player'
                });
                return {
                    data: response,
                    property: 'team-squad',
                    template: 'App/General/Plugins/FootballDataTeamStatsTemplate.html'
                };
            }).catch((error) => {
                return $q.reject(error);
            });
        }

        return {
            getTeamDetails: getTeamDetails,
            integrationKey: integrationKey
        };
    }]);
