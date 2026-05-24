'use strict';
angular.module('mundialitoApp').directive('activeNav', ['$location', function ($location) {
    return {
        restrict: 'A',
        link: function (scope, element) {
            var nestedA = element.find('a')[0];
            var linkPath = nestedA.pathname.replace(/\/$/, '') || '/';

            function isActive(currentPath) {
                var current = currentPath.replace(/\/$/, '') || '/';
                if (linkPath === '/') {
                    return current === '/';
                }
                return current === linkPath || current.indexOf(linkPath + '/') === 0;
            }

            function updateActive() {
                if (isActive($location.path())) {
                    element.addClass('active');
                } else {
                    element.removeClass('active');
                }
            }

            updateActive();
            scope.$on('$locationChangeSuccess', updateActive);
        }
    };
}]);
