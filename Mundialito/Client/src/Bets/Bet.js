'use strict';
angular.module('mundialitoApp').factory('Bet', [function() {
    function Bet(betData) {
        if (betData) {
            this.setData(betData);
        }
        // Some other initializations related to bet
    };

    Bet.prototype = {
        setData: function(betData) {
            angular.extend(this, betData);
        },
        getTeamByCornersMark: function() {
            if (this.CornersMark === '1') {
                return this.Game.HomeTeam;
            }
            if (this.CornersMark === '2') {
                return this.Game.AwayTeam;
            }
            return null;
        },
        getTeamByCardsMark: function() {
            if (this.CardsMark === '1') {
                return this.Game.HomeTeam;
            }
            if (this.CardsMark === '2') {
                return this.Game.AwayTeam;
            }
            return null;
        },
        getGameUrl: function() {
            return '/games/' + this.Game.GameId;
        },
        getClass: function() {
            if (this.Points >= 7) {
                return 'success';
            }
            if (this.Points >= 5) {
                return 'primary';
            }
            if (this.Points >= 3) {
                return 'info';
            }
            if (this.Points > 0) {
                return 'warning';
            }
            return 'danger';
        }
    };
    return Bet;
}]);
