angular.module('mundialitoApp')
    .factory('FootballDataGamePlugin', ['$q', '$rootScope', 'GenericProxyService', function ($q, $rootScope, GenericProxyService) {
        var baseUrl = 'https://api.football-data.org/v4/matches/';
        // var apiKey = '7edaa34b2da744eab36fd60aba7d2665'; 
        const integrationKey = 'football-data'

        function getGameDetails(gameId) {
            var url = baseUrl + gameId;
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
