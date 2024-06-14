angular.module('mundialitoApp')
    .factory('PluginsProvider', ['$q', '$log', function ($q, $log) {
        var gamesFactories = [];
        var teamsFactories = [];


        function getGameDetailsFromAll(game) {
            let relevantPlugins = [];
            if (!!game.IntegrationsData) {
                relevantPlugins = _.filter(gamesFactories, (plugin) => { return game.IntegrationsData[plugin.integrationKey] !== undefined;});    
            }
            var promises = relevantPlugins.map((factory) => factory.getGameDetails(game));
            return $q.all(promises).then((results) => {
                return results;
            }).catch((e) => { 
                $log.warn('Error fetching game details: ' + e);
                return $q.reject(e)
            });
        }

        function getTeamDetailsFromAll(team) {
            let relevantPlugins = [];
            if (!!team.IntegrationsData) {
                relevantPlugins = _.filter(teamsFactories, (plugin) => { return team.IntegrationsData[plugin.integrationKey] !== undefined;});    
            }
            var promises = relevantPlugins.map((factory) => factory.getTeamDetails(team));
            return $q.all(promises).then((results) => {
                return results;
            }).catch((e) => { 
                $log.warn('Error fetching team details: ' + e);
                return $q.reject(e)
            });
        }

        return {
            getGameDetailsFromAll: getGameDetailsFromAll,
            getTeamDetailsFromAll: getTeamDetailsFromAll,
            registerGameFactory: (factory) => {
                gamesFactories.push(factory);
            },
            registerTeamFactory: (factory) => {
                teamsFactories.push(factory);
            }
        };
    }]);
