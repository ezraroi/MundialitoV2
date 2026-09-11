'use strict';
/* Reports the errors AngularJS catches itself - in controllers, watchers, promise callbacks,
   directives - which never reach window.onerror, so the Sentry SDK cannot see them on its own.
   Until v7 the SDK did this through its AngularJS integration (ngSentry); this is that
   decorator, ported from @sentry/integrations 6.19.7. Unlike ngSentry it is part of the app,
   so a Sentry script that fails to load no longer stops Angular booting, and nothing that goes
   wrong inside Sentry can keep Angular's own handler from running. */
angular.module('mundialitoApp').config(['$provide', function ($provide) {
    // '[$rootScope:inprog] $digest already in progress\nhttps://errors.angularjs.org/...'
    var ANGULAR_ERROR = /^\[((?:[$a-zA-Z0-9]+:)?(?:[$a-zA-Z0-9]+))\] (.*?)\n?(\S+)$/;

    /* Title the issue '$rootScope:inprog: $digest already in progress' rather than 'Error: ...'
       and keep the docs link out of the title, as ngSentry did. */
    function angularShape(event) {
        var exception = event.exception && event.exception.values && event.exception.values[0];
        var match = exception && ANGULAR_ERROR.exec(exception.value || '');
        if (match) {
            exception.type = match[1];
            exception.value = match[2];
            event.message = exception.type + ': ' + exception.value;
            event.extra = angular.extend({}, event.extra, { angularDocs: match[3].substr(0, 250) });
        }
        return event;
    }

    $provide.decorator('$exceptionHandler', ['$delegate', function ($delegate) {
        return function (exception, cause) {
            try {
                if (typeof Sentry !== 'undefined' && Sentry.withScope) {
                    Sentry.withScope(function (scope) {
                        if (cause) {
                            scope.setExtra('cause', cause);
                        }
                        scope.addEventProcessor(angularShape);
                        Sentry.captureException(exception);
                    });
                }
            } catch (e) {
                // Reporting is best effort; Angular's own handling below must always run.
            }
            $delegate(exception, cause);
        };
    }]);
}]);
