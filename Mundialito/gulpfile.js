const gulp = require('gulp');
const concat = require('gulp-concat');
const cleanCss = require('gulp-minify-css');
const minify = require('gulp-minify');

const allCss = [
    "Client/css/angular-busy.min.css",
    "Client/css/angular-key-value-editor.css",
    "Client/css/bootstrap.css",
    "Client/css/bootstrap-theme.css",
    "Client/css/datetimepicker.css",
    "Client/css/fontawesome.css",
    "Client/css/solid.css",
    "Client/css/brands.css",
    "Client/css/flag-css.css",
    "Client/css/select_.css",
    "Client/css/ui-grid.css",
    "Client/css/toaster.css",
    "Client/css/site.css",
    "Client/css/ng-sortable.css",
    "Client/css/ng-sortable.style.css"
];
gulp.task('build-css-space-lab', () => {
    return gulp.src(allCss.concat(['Client/css/bootstrap-spacelab.min.css']))
        .pipe(concat('app-space-lab.css'))
        .pipe(cleanCss())
        .pipe(gulp.dest('wwwroot/css'));
});

gulp.task('build-css-cerulean', () => {
    return gulp.src(allCss.concat(['Client/css/bootstrap-cerulean.min.css']))
        .pipe(concat('app-cerulean.css'))
        .pipe(cleanCss())
        .pipe(gulp.dest('wwwroot/css'));
});

gulp.task('compress-lib', function () {
    return gulp.src([
        "Client/lib/moment.js",
        "Client/lib/jquery.js",
        "Client/lib/angular.js",
        "Client/lib/angular-animate.js",
        "Client/lib/angular-sanitize.js",
        "Client/lib/angular-resource.js",
        "Client/lib/angular-route.js",
        "Client/lib/select_.js",
        "Client/lib/bootstrap.js",
        "Client/lib/bootstrap-select.min.js",
        "Client/lib/ui-bootstrap-tpls.js",
        "Client/lib/autofields-bootstrap.min.js",
        "Client/lib/autofields.min.js",
        "Client/lib/angular-spa-security.js",
        "Client/lib/promise-tracker.min.js",
        "Client/lib/datetimepicker.js",
        "Client/lib/underscore.js",
        "Client/lib/d3.min.js",
        "Client/lib/line-chart.min.js",
        "Client/lib/ui-grid.min.js",
        "Client/lib/ng-google-chart.js",
        "Client/lib/angular-cache.min.js",
        "Client/lib/toaster.js",
        "Client/lib/angular-busy.js",
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

gulp.task('copy-html', function () {
    return gulp.src(['Client/src/**/*.html'])
        .pipe(gulp.dest('wwwroot/App'));
});

gulp.task('copy-templates', function () {
    return gulp.src(['Client/resources/**/*.html'])
        .pipe(gulp.dest('wwwroot/'));
});

gulp.task('default', gulp.series(['build-css-cerulean', 'build-css-space-lab', 'compress-lib', 'compress-app', 'copy-html', 'copy-templates']));