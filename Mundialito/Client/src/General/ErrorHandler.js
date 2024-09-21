'use strict';
angular.module('mundialitoApp').factory('ErrorHandler', ['$rootScope', '$log', 'Alert', '$location', 'Constants', function ($rootScope, $log, Alert, $location, Constants) {
    var ErrorHandler = this;

    ErrorHandler.handle = (data, status, headers, config) => {
        $log.log(data);
        if (config.ignoreError) {
            return;
        }
        if (status === 401) {
            localStorage.removeItem('accessToken');
            sessionStorage.removeItem('accessToken');
            $location.path(Constants.LOGIN_PATH);
            return;
        }
        var message = [];
        var title = undefined;
        if (data.Message) {
            title = data.Message;
        }
        if (data.errors) {
            angular.forEach(data.errors, (errors) => {
                    angular.forEach(errors, (errors) => {
                            message.push(errors);
                        });
                });
        }
        if (data.ModelState) {
            angular.forEach(data.ModelState, function (errors) {
                message.push(errors);
            });
        }
        if (data.ExceptionMessage) {
            message.push(data.ExceptionMessage);
        }
        if (data.error_description) {
            message.push(data.error_description);
        }
        if (message.length === 0 && !title) {
            title = "General Error";
            message.push("Looks like the server is down, please try again in few minutes");
        }
        Alert.error(message.join('\n'), title);
    }

    return ErrorHandler;
}])
    .factory('myHttpInterceptor', ['ErrorHandler', '$q', function (ErrorHandler, $q) {
        return {
            response: (response) => response,
            responseError: (response) => {
                ErrorHandler.handle(response.data, response.status, response.headers, response.config);
                Sentry.captureException(response.data);
                // do something on error
                return $q.reject(response);
            }
        };
    }]);