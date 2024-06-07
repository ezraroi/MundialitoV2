angular.module('mundialitoApp')
    .factory('GamePluginProvider', ['$q', function ($q) {
        var factories = [];

        function getGameDetailsFromAll(integrationData) {
            let relevantPlugins = [];
            if (integrationData !== null) {
                relevantPlugins = _.filter(factories, (plugin) => { return integrationData[plugin.integrationKey] !== undefined;});    
            }
            var promises = relevantPlugins.map((factory) => factory.getGameDetails(integrationData[factory.integrationKey]));
            return $q.all(promises).then((results) => {
                return results;
            });
        }

        return {
            getGameDetailsFromAll: getGameDetailsFromAll,
            registerFactory: (factory) => {
                factories.push(factory);
            }
        };
    }]);
