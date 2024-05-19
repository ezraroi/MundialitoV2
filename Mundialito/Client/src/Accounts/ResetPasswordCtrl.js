'use strict';
angular.module('mundialitoApp').controller('ResetPasswordCtrl', ['$scope', '$rootScope', 'security', '$location', 'Alert', function ($scope, $rootScope, Security, $location, Alert) {
    $rootScope.mundialitoApp.authenticating = false;

    var ResetPasswordModel = function () {
        return {
            confirmPassword: '',
            password: '',
            email: $location.search()['email'],
            token: $location.search()['token']
        }
    };

    $scope.user = new ResetPasswordModel();
    $scope.reset = function () {
        if (!$scope.resetForm.$valid) return;
        $rootScope.mundialitoApp.message = "Processing Reset Password...";
        Security.resetPassword(angular.copy($scope.user)).then(() => {
            Alert.success('Your was was reset successfully');
        }).finally(function () {
            $rootScope.mundialitoApp.message = null;
        });

    }
    $scope.schema = [
        { property: 'password', type: 'password', attr: { required: true } },
        { property: 'confirmPassword', label: 'Confirm Password', type: 'password', attr: { confirmPassword: 'user.password', required: true } }
    ];
}]);