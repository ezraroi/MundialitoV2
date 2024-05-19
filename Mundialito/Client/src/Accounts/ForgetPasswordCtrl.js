'use strict';
angular.module('mundialitoApp').controller('ForgetPasswordCtrl', ['$scope', '$rootScope', 'security', 'Alert', function ($scope, $rootScope, Security, Alert) {
    $rootScope.mundialitoApp.authenticating = false;

    var ForgetModel = function () {
        return {
            Email: ''
        }
    };

    $scope.user = new ForgetModel();
    $scope.forget = function () {
        if (!$scope.emailForm.$valid) return;
        $rootScope.mundialitoApp.message = "Processing...";
        Security.forgotPassword(angular.copy($scope.user)).then(() => {
            Alert.success('Reset password token was sent to your email, please follow the link from there');
        }).catch((e) => {
            Alert.error('Failed to generate reset password token: ' + e);
        }).finally(function () {
            $rootScope.mundialitoApp.message = null;
        });
    }
    $scope.schema = [
        { property: 'Email', label: 'Email Address', type: 'email', attr: { required: true } },
    ];
}]);