angular.module('mundialitoApp').constant('Constants',
    {
        LOGIN_PATH: '/login',
        REFRESH_TIME: 300000,
        TABLE_GRID_OPTIONS: {
            saveWidths: true,
            saveVisible: true,
            saveOrder: true,
            enableRowSelection: false,
            enableSelectAll: false,
            multiSelect: false,
            rowTemplate: '<div ng-click="grid.appScope.goToUser(row)" style="cursor: pointer" ng-class="{\'text-primary\':row.entity.Username===grid.appScope.security.user.Username }"><div ng-repeat="(colRenderIndex, col) in colContainer.renderedColumns track by col.colDef.name" class="ui-grid-cell" ng-class="{ \'ui-grid-row-header-cell\': col.isRowHeader }" ui-grid-cell></div></div>',
            columnDefs: [
                { field: 'Place', displayName: '', resizable: false, maxWidth: 30 },
                { field: 'Name', displayName: 'Name', resizable: true, minWidth: 115 },
                { field: 'Points', displayName: 'Points', resizable: true, minWidth: 45, maxWidth: 75},
                { field: 'GeneralBet.WinningTeam', displayName: 'Team', resizable: false, maxWidth: 45, cellTemplate: '<div class="ui-grid-cell-contents text-center" title="TOOLTIP"><i ng-class="[\'flag\',\'flag-fifa-{{grid.appScope.teamsDic[row.entity.GeneralBet.WinningTeamId].ShortName | lowercase}}\']" tooltip="{{grid.appScope.teamsDic[row.entity.GeneralBet.WinningTeamId].Name}}"></i></div>' },
                { field: 'GeneralBet.GoldenBootPlayer', displayName: 'Player', resizable: false, minWidth: 50, maxWidth: 50 },
                { field: 'TotalMarks', displayName: 'Total Marks', resizable: true },
                { field: 'Results', displayName: 'Results', resizable: true },
                { field: 'YellowCards', displayName: 'Yellow Cards Marks', maxWidth: 55, resizable: false, headerCellTemplate: '<div class="text-center" style="margin-top: 5px;"><i class="fa fa-stop fa-2xl" style="color: #ffff00"></i></div>' },
                { field: 'Corners', displayName: 'Corners Marks', maxWidth: 55, resizable: false, headerCellTemplate: '<div class="text-center" style="margin-top: 5px;"><i class="fa fa-flag fa-xl"></i></div>' },,
                { field: 'PlaceDiff', displayName: '', resizable: false, maxWidth: 45, cellTemplate: '<div ng-class="{\'text-success\': COL_FIELD.indexOf(\'+\') !== -1, \'text-danger\': (COL_FIELD.indexOf(\'+\') === -1) && (COL_FIELD !== \'0\')}"><div class="ngCellText">{{::COL_FIELD}}</div></div>' }
            ],
        }
    }
);