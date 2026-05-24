'use strict';
angular.module('mundialitoApp').controller('LoginCtrl', ['$scope', '$rootScope', '$timeout', 'security', function ($scope, $rootScope, $timeout, Security) {
    $rootScope.mundialitoApp.authenticating = false;

    var LoginModel = function () {
        return {
            username: '',
            password: '',
            rememberMe: false
        }
    };

    $scope.user = new LoginModel();
    $scope.login = () => {
        if (!$scope.loginForm.$valid) return;
        $rootScope.mundialitoApp.message = "Processing Login...";
        Security.login(angular.copy($scope.user)).finally(function () {
            $rootScope.mundialitoApp.message = null;
        });
    }
    $scope.schema = [
            { property: 'username', type: 'text', attr: { ngMinlength: 4, required: true } },
            { property: 'password', type: 'password', attr: { ngMinlength: 4, required: true } },
            { property: 'rememberMe', label: 'Keep me logged in', type: 'checkbox' }
    ];

    function onGoogleCredential(response) {
        $scope.$apply(function () {
            $rootScope.mundialitoApp.message = "Processing Login...";
            Security.googleLogin({ Credential: response.credential }).finally(function () {
                $rootScope.mundialitoApp.message = null;
            });
        });
    }

    function initGoogleSignIn(attempt) {
        attempt = attempt || 0;
        var clientId = $rootScope.mundialitoApp.GoogleClientId;
        if (!clientId) {
            return;
        }

        if (typeof google === 'undefined' || !google.accounts || !google.accounts.id) {
            if (attempt < 10) {
                $timeout(function () { initGoogleSignIn(attempt + 1); }, 100);
            }
            return;
        }

        var buttonEl = document.getElementById('buttonDiv');
        if (!buttonEl) {
            if (attempt < 10) {
                $timeout(function () { initGoogleSignIn(attempt + 1); }, 100);
            }
            return;
        }

        buttonEl.innerHTML = '';

        google.accounts.id.initialize({
            client_id: clientId,
            callback: onGoogleCredential
        });
        google.accounts.id.renderButton(
            buttonEl,
            { theme: "filled_blue", size: "large", text: "continue_with", shape: "circle" }
        );
        google.accounts.id.prompt();
    }

    $scope.$on('$destroy', function () {
        if (typeof google !== 'undefined' && google.accounts && google.accounts.id) {
            google.accounts.id.cancel();
        }
        var buttonEl = document.getElementById('buttonDiv');
        if (buttonEl) {
            buttonEl.innerHTML = '';
        }
    });

    if ($rootScope.mundialitoApp.GoogleClientId) {
        $timeout(initGoogleSignIn, 0);
    }

}]);
