const gulp = require('gulp');
const concat = require('gulp-concat');
const cleanCss = require('gulp-minify-css');
const minify = require('gulp-minify');

gulp.task('build-css-space-lab', () => {
    return gulp.src([
        "Client/css/bootstrap.css",
        "Client/css/bootstrap-spacelab.min.css",
        "Client/css/datetimepicker.css",
        "Client/css/font-awesome.min.css",
        "Client/css/flag-css.css",
        "Client/css/angular-busy.min.css",
        "Client/css/select2.min.css",
        "Client/css/select2-bootstrap.min.css",
        "Client/css/ui-grid.min.css",
        "Client/css/toaster.min.css",
        "Client/css/site.css",
        "Client/css/angular-key-value-editor.css",
        "Client/css/ng-sortable.css",
        "Client/css/ng-sortable.style.css"
        
    ])
        .pipe(concat('app-space-lab.css'))
        .pipe(cleanCss())
        .pipe(gulp.dest('wwwroot/css'));
});

gulp.task('build-css-cerulean', () => {
    return gulp.src([
        "Client/css/bootstrap.css",
        "Client/css/bootstrap-cerulean.min.css",
        "Client/css/datetimepicker.css",
        "Client/css/font-awesome.min.css",
        "Client/css/flag-css.css",
        "Client/css/angular-busy.min.css",
        "Client/css/select2.min.css",
        "Client/css/select2-bootstrap.min.css",
        "Client/css/ui-grid.min.css",
        "Client/css/toaster.min.css",
        "Client/css/site.css",
        "Client/css/angular-key-value-editor.css",
        "Client/css/ng-sortable.css",
        "Client/css/ng-sortable.style.css"
    ])
        .pipe(concat('app-cerulean.css'))
        .pipe(cleanCss())
        .pipe(gulp.dest('wwwroot/css'));
});

gulp.task('compress-lib', function () {
    return gulp.src([
        "Client/lib/jquery-2.1.1.js",
        "Client/lib/angular.js",
        "Client/lib/angular-animate.js",
        "Client/lib/angular-sanitize.js",
        "Client/lib/angular-resource.js",
        "Client/lib/angular-route.js",
        "Client/lib/select2.js",
        "Client/lib/bootstrap.min.js",
        "Client/lib/bootstrap-select.min.js",
        "Client/lib/angular-ui/ui-bootstrap-tpls.min.js",
        "Client/lib/autofields-bootstrap.min.js",
        "Client/lib/autofields.min.js",
        "Client/lib/moment.min.js",
        "Client/lib/angular-spa-security.js",
        "Client/lib/promise-tracker.min.js",
        "Client/lib/datetimepicker.min.js",
        "Client/lib/facebookPluginDirectives.min.js",
        "Client/lib/underscore.min.js",
        "Client/lib/d3.min.js",
        "Client/lib/line-chart.min.js",
        "Client/lib/ui-grid.min.js",
        "Client/lib/angular-select2.js",
        "Client/lib/ng-google-chart.js",
        "Client/lib/angular-cache.min.js",
        "Client/lib/toaster.min.js",
        "Client/lib/angular-busy.min.js",
        "Client/lib/angular-key-value-editor.js",
        "Client/lib/compiled-templates.js",
        "Client/lib/ng-sortable.js"
        
    ])
        .pipe(concat('lib.js'))
        .pipe(minify())
        .pipe(gulp.dest('wwwroot/lib'))
});

gulp.task('compress-app', function () {
    return gulp.src([
        'Client/src/app.js',
        'Client/src/Constants.js',
        'Client/src/**/*.js',
    ])
        .pipe(concat('app.js'))
        .pipe(minify())
        .pipe(gulp.dest('wwwroot/js'))
});

gulp.task('copy-html', function() {
    return gulp.src('Client/src/**/*.html')
      .pipe(gulp.dest('wwwroot/App'));
  });

gulp.task('default', gulp.series(['build-css-cerulean', 'build-css-space-lab', 'compress-lib', 'compress-app', 'copy-html']));