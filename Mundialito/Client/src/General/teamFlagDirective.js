'use strict';
angular.module('mundialitoApp').directive('teamFlag', ['$rootScope', ($rootScope) => ({
    restrict: 'E',
    scope: {
        team: '='
    },
    templateUrl: 'App/General/teamFlagTemplate.html',
    link: (scope) => {
        scope.useFlagsCss = $rootScope.mundialitoApp.clientConfig.UseFlagsCss;
    }
})]);
