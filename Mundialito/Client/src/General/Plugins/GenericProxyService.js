angular.module('mundialitoApp')
    .factory('GenericProxyService', ['$http', '$q', function($http, $q) {
        var baseUrl = 'api/genericproxy/';  // Proxy server URL

        function proxyRequest(method, url, data, headers) {
            var deferred = $q.defer();

            $http({
                method: method,
                url: baseUrl + url,
                data: data,
                headers: headers,
                ignoreError: true
            }).then((response) => {
                deferred.resolve(response.data);
            }).catch((error) => {
                deferred.reject(error);
            });

            return deferred.promise;
        }

        return {
            proxyRequest: proxyRequest
        };
    }]);
