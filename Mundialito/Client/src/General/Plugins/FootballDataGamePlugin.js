angular.module('mundialitoApp')
    .factory('FootballDataGamePlugin', ['$q', '$rootScope', 'GenericProxyService', function ($q, $rootScope, GenericProxyService) {
        var baseUrl = 'https://api.football-data.org/v4/matches/';
        const integrationKey = 'football-data'

        function getGameDetails(gameId) {
            var url = baseUrl + gameId;
            if ((!$rootScope.mundialitoApp.clientConfig) || (!$rootScope.mundialitoApp.clientConfig['football-data-api-key'])) {
                return $q.reject('Skipping football-data as no api key provided');
            }
            return GenericProxyService.proxyRequest('GET', url, undefined, {
                'X-Auth-Token': $rootScope.mundialitoApp.clientConfig['football-data-api-key']
            }).then((response) => {
                return {
                    data: response,
                    property: integrationKey,
                    template: 'App/General/Plugins/FootballDataGameTemplate.html'
                };
            }).catch((error) => {
                return $q.reject(error);
            });
        }

        return {
            getGameDetails: getGameDetails,
            integrationKey: integrationKey
        };
    }]);
