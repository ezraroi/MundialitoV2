'use strict';
angular.module('mundialitoApp').factory('ErrorHandler', ['$log', 'Alert', '$location', 'Constants', '$injector', function ($log, Alert, $location, Constants, $injector) {
    var ErrorHandler = this;

    /* Bets are the only thing an unapproved player can try to write, so a 403 there means
       "your account is not active yet" rather than "you are not an admin". The bet upsert
       lives under api/games/{id}/mybet, so matching on api/bets alone misses it. */
    var BETTING_URL = /api\/(bets|generalbets|games\/\d+\/mybet)/i;

    function clearSession() {
        localStorage.removeItem('accessToken');
        sessionStorage.removeItem('accessToken');
        /* $http and security are resolved lazily on purpose - injecting them would close
           the loop $http -> myHttpInterceptor -> ErrorHandler -> $http. */
        try {
            delete $injector.get('$http').defaults.headers.common.Authorization;
            $injector.get('security').user = null;
        } catch (e) {
            $log.warn('ErrorHandler: could not fully clear the session', e);
        }
        if (typeof Sentry !== 'undefined' && Sentry.setUser) {
            Sentry.setUser(null);
        }
    }

    ErrorHandler.handle = (data, status, headers, config) => {
        $log.log(data);
        config = config || {};
        if (config.ignoreError) {
            return;
        }
        if (status === 401) {
            clearSession();
            $location.path(Constants.LOGIN_PATH);
            return;
        }
        /* A dropped connection, a CORS block or a timeout gives status 0 (or -1) and a
           null body. This is the only case where blaming the server is accurate. */
        if (status === 0 || status === -1) {
            Alert.error('Looks like the server is unreachable, please try again in a few minutes', 'Connection Problem');
            return;
        }
        /* Anything below may be reached with a null body (an empty 403/404), so normalise
           it before touching a property. */
        data = data || {};
        if (status === 403 && !data.Message) {
            if (BETTING_URL.test(config.url || '')) {
                Alert.error('Your account is not approved for betting yet. Please contact the admin.', 'Not Approved');
            } else {
                Alert.error('You do not have permission to perform this action.', 'Not Allowed');
            }
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
            title = "Error";
            message.push("Something went wrong, please try again");
        }
        Alert.error(message.join('\n'), title);
    }

    return ErrorHandler;
}])
    .factory('myHttpInterceptor', ['ErrorHandler', '$q', '$log', function (ErrorHandler, $q, $log) {

        /* Strip ids out of the path so api/bets/12 and api/bets/34 group as one issue. */
        function fingerprintUrl(url) {
            return (url || 'unknown').split('?')[0].replace(/\/\d+(?=\/|$)/g, '/:id');
        }

        /* A 4xx whose body carries a Message is a rule the API meant to enforce - "this
           player is picked by a general bet", "general bets are already closed", "your
           account is not approved yet". Nothing failed: the server was asked to do
           something it is designed to refuse, the user was told why, and there is nobody
           to page. Reporting those buries the 4xx that are real defects under refusals
           that recur by design.

           The body shape is what separates them. That sentence is only ever written by a
           deliberate BadRequest/NotFound/Forbidden branch (ErrorMessage in Mundialito.Models
           - no controller turns a caught exception into one). A bug or a contract mismatch
           surfaces as ASP.NET's own shapes instead - ProblemDetails {type,title,errors},
           MundialitoValidationModelAttribute's {ModelState}, or an empty body - and is
           still reported, which is what caught the PUT /api/bets/:id 400. 5xx is never a
           deliberate refusal, so it reports whatever the body says. */
        function isDeliberateRefusal(response) {
            return response.status >= 400 && response.status < 500
                && response.data
                && typeof response.data.Message === 'string'
                && response.data.Message.length > 0;
        }

        function captureHttpError(response) {
            /* Sentry is a bare global loaded by Views/Home/Index.cshtml. An ad-blocker
               eating sentry/*.js must not take HTTP error handling down with it. */
            if (typeof Sentry === 'undefined' || !Sentry.captureException) {
                return;
            }
            var config = response.config || {};
            /* Deliberately ignored errors and routine token expiry are not worth reporting. */
            if (config.ignoreError || response.status === 401) {
                return;
            }
            if (isDeliberateRefusal(response)) {
                return;
            }
            var method = (config.method || 'GET').toUpperCase();
            var url = config.url || 'unknown';
            /* A real Error, not response.data - an empty body used to be captured as ""
               and every such failure collapsed into one untitled, stackless issue. */
            var error = new Error('HTTP ' + response.status + ' ' + method + ' ' + url);
            error.name = 'HttpError';
            Sentry.withScope(function (scope) {
                scope.setTag('http.status', String(response.status));
                scope.setTag('http.method', method);
                scope.setContext('response', {
                    status: response.status,
                    method: method,
                    url: url,
                    body: response.data
                });
                scope.setFingerprint(['http', String(response.status), method, fingerprintUrl(url)]);
                Sentry.captureException(error);
            });
        }

        return {
            response: (response) => response,
            responseError: (response) => {
                try {
                    ErrorHandler.handle(response.data, response.status, response.headers, response.config);
                } catch (e) {
                    /* Reporting a failure must never swallow the failure itself. */
                    $log.error('ErrorHandler threw while handling a response', e);
                }
                captureHttpError(response);
                return $q.reject(response);
            }
        };
    }]);
