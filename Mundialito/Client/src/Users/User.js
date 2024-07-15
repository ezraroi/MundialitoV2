'use strict';
angular.module('mundialitoApp').factory('User', ['$http','$log', function($http, $log) {
    function User(userData) {
        if (userData) {
            this.setData(userData);
        }
        // Some other initializations related to user
    };

    User.prototype = {
        setData: function(userData) {
            angular.extend(this, userData);
        },
        getUrl: function() {
            return '/users/' + this.Username;
        },
        delete: function() {
            if (confirm('Are you sure you would like to delete user ' + this.Username)) {
                $log.debug('User: Will delete user ' + this.Username)
                return $http.delete("api/users/" + this.Id, { tracker: 'deleteUser' });
            }
        },
        makeAdmin :function() {
            if (confirm('Are you sure you would like to make ' + this.Name + ' Admin?')) {
                $log.debug('User: Will make user ' + this.Username + ' admin')
                return $http.post("api/users/makeadmin/" + this.Id, { tracker: 'makeAdmin' });
            }
        },
        activate: function () {
            if (confirm('Are you sure you would like to activate ' + this.Name)) {
                $log.debug('User: Will actiavte user ' + this.Username)
                return $http.post('api/users/' + this.Id + '/activate');
            }
        },
        deactivate: function () {
            if (confirm('Are you sure you would like to deactiavte ' + this.Name)) {
                $log.debug('User: Will deactiavte user ' + this.Username)
                return $http.delete('api/users/' + this.Id + '/activate');
            }
        },
    };
    return User;
}]);
