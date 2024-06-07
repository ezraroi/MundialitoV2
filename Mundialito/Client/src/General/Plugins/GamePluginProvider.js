angular.module('mundialitoApp')
    .factory('GamePluginProvider', ['$q', '$log', function ($q, $log) {
        var factories = [];

        function getGameDetailsFromAll(integrationData) {
            let relevantPlugins = [];
            if (integrationData !== null) {
                relevantPlugins = _.filter(factories, (plugin) => { return integrationData[plugin.integrationKey] !== undefined;});    
            }
            var promises = relevantPlugins.map((factory) => factory.getGameDetails(integrationData[factory.integrationKey]));
            return $q.all(promises).then((results) => {
                return results;
            }).catch((e) => { 
                $log.error('Error fetching game details: ' + e);
                return $q.reject(e)
            });
        }

        return {
            getGameDetailsFromAll: getGameDetailsFromAll,
            registerFactory: (factory) => {
                factories.push(factory);
            }
        };
    }]);
