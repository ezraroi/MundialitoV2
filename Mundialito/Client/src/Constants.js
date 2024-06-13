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
                { field: 'Name', displayName: 'Name', resizable: true, minWidth: 150 },
                { field: 'Points', displayName: 'Points', resizable: true },
                { field: 'TotalMarks', displayName: 'Total Marks', resizable: true },
                { field: 'Results', displayName: 'Results', resizable: true },
                { field: 'Marks', displayName: 'Marks', resizable: true },
                { field: 'YellowCards', displayName: 'Yellow Cards Marks', resizable: true },
                { field: 'Corners', displayName: 'Corners Marks', resizable: true },
                { field: 'PlaceDiff', displayName: '', resizable: false, maxWidth: 45, cellTemplate: '<div ng-class="{\'text-success\': COL_FIELD.indexOf(\'+\') !== -1, \'text-danger\': (COL_FIELD.indexOf(\'+\') === -1) && (COL_FIELD !== \'0\')}"><div class="ngCellText">{{::COL_FIELD}}</div></div>' }
            ],
        }
    }
);