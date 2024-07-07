'use strict';
angular.module('mundialitoApp').controller('TeamCtrl', ['$scope', '$log', 'TeamsManager', 'team', 'games', 'Alert', 'PluginsProvider', 'MundialitoUtils', function ($scope, $log, TeamsManager, team, games, Alert, PluginsProvider, MundialitoUtils) {
    $scope.team = team;
    $scope.games = games;
    $scope.plugins = {};
    $scope.teamsForm = {};
    $scope.showEditForm = false;
    $scope.toKeyValue = (object) => {
        return _.keys(object).map((key) => { return { 'name': key, 'value': object[key] } });
    };
    $scope.IntegrationsData = $scope.toKeyValue($scope.team.IntegrationsData);
    $scope.fromKeyValue = (array) => {
        let res = {};
        array.forEach((item) => {
            if (item.name !== '') {
                res[item.name] = item.value;
            }
        })
        return res;
    };

    const form = _.chain($scope.games).sortBy((game) => new Date(game.Date)).filter((game) => game.IsBetResolved).map((game) => {
        return MundialitoUtils.getGameMark(game, $scope.team.TeamId);
    }).value();
    $scope.teamsForm[$scope.team.TeamId] = {
        form : form,
        games: _.filter($scope.games, (game) => game.IsBetResolved)
    }
    PluginsProvider.getTeamDetailsFromAll($scope.team).then((results) => {
        results.forEach((result) => {
            $scope.plugins[result.property] = { data: result.data, template: result.template };
        });
    });

    $scope.updateTeam = () => {
        $scope.team.IntegrationsData = $scope.fromKeyValue($scope.IntegrationsData);
        $scope.team.update().then((data) => {
            Alert.success('Team was updated successfully');
            TeamsManager.setTeam(data.data);
        });
    };

    $scope.schema =  TeamsManager.getTeamSchema();
    
}]);