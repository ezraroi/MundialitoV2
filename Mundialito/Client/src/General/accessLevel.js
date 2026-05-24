'use strict';
angular.module('mundialitoApp').directive('accessLevel', ['$log', 'security', function ($log, Security) {
    return {
        restrict: 'A',
        link: function ($scope, element, attrs) {
            var prevDisp = element.css('display')
                , userRole = ''
                , accessLevel = attrs.accessLevel;

            if (accessLevel === 'Admin') {
                element.css('display', 'none');
                element.addClass('mu-access-denied');
            }

            $scope.$watch(
                function () {
                    return Security.user;
                },
                function (newValue) {
                    $scope.user = newValue;
                    if (($scope.user === undefined) || ($scope.user === null)) {
                        userRole = 'Active';
                    } else if ($scope.user.Roles) {
                        userRole = $scope.user.Roles;
                    } else {
                        userRole = 'Active';
                    }
                    updateCSS();
                },
                true
            );

            attrs.$observe('accessLevel', function (al) {
                if (al) {
                    accessLevel = al;
                }
                updateCSS();
            });

            function updateCSS() {
                if (!accessLevel) {
                    return;
                }
                if (userRole === accessLevel) {
                    element.removeClass('mu-access-denied');
                    element.css('display', prevDisp);
                } else {
                    element.addClass('mu-access-denied');
                    element.css('display', 'none');
                }
            }

            updateCSS();
        }
    };
}]);
