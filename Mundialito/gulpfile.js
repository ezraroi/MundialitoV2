const fs = require('node:fs');
const path = require('node:path');
const gulp = require('gulp');
const concat = require('gulp-concat');
const cleanCss = require('gulp-minify-css');
const minify = require('gulp-minify');
const hash = require('gulp-hash-filename');
const replace = require('gulp-replace');
const clean = require('gulp-clean');
const { console } = require('node:inspector');

const allCss = [
    "Client/css/angular-busy.css",
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
    "Client/css/angular-bootstrap-toggle.css",
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
        "Client/lib/ng-sortable.js",
        "Client/lib/angular-bootstrap-toggle.js",
        "Client/lib/html2canvas.min.js",
    ])
        .pipe(concat('lib.js'))
        .pipe(minify())
        .pipe(hash())
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
        .pipe(hash())
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

gulp.task('copy-sentry', function () {
    return gulp.src(['Client/lib/sentry/*.js'])
        .pipe(gulp.dest('wwwroot/sentry'));
});


gulp.task('cache-bust-lib', () => {
    const libPattern = /^lib-[a-z0-9]+\.js$/;
    let lib = fs.readdirSync('wwwroot/lib').filter(fileName => {
        return libPattern.test(fileName);
    });
    if (lib.length !== 1) {
        throw new Error('There should be exactly one lib file: ' + lib.join(', '));
    }
    return gulp.src('Views/Home/Index.cshtml')
        .pipe(replace(/lib-[a-z0-9]+\.js/, lib[0]))
        .pipe(gulp.dest('Views/Home'));
});

gulp.task('cache-bust-lib-min', () => {
    const libMinPattern = /^lib-min-[a-z0-9]+\.js$/;
    let libMinified = fs.readdirSync('wwwroot/lib').filter(fileName => {
        return libMinPattern.test(fileName);
    });
    if (libMinified.length !== 1) {
        throw new Error('There should be exactly one lib file: ' + libMinified.join(', '));
    }
    return gulp.src('Views/Home/Index.cshtml')
        .pipe(replace(/lib-min-[a-z0-9]+\.js/, libMinified[0]))
        .pipe(gulp.dest('Views/Home'));
});

gulp.task('cache-bust-app', () => {
    const appPattern = /^app-[a-z0-9]+\.js$/;
    let app = fs.readdirSync('wwwroot/js').filter(fileName => {
        return appPattern.test(fileName);
    });
    if (app.length !== 1) {
        throw new Error('There should be exactly one app file: ' + app.join(', '));
    }
    return gulp.src('Views/Home/Index.cshtml')
        .pipe(replace(/app-[a-z0-9]+\.js/, app[0]))
        .pipe(gulp.dest('Views/Home'));
});

gulp.task('cache-bust-app-min', () => {
    const appMinPattern = /^app-min-[a-z0-9]+\.js$/;
    let appMinified = fs.readdirSync('wwwroot/js').filter(fileName => {
        return appMinPattern.test(fileName);
    });
    if (appMinified.length !== 1) {
        throw new Error('There should be exactly one app file: ' + appMinified.join(', '));
    }
    return gulp.src('Views/Home/Index.cshtml')
        .pipe(replace(/app-min-[a-z0-9]+\.js/, appMinified[0]))
        .pipe(gulp.dest('Views/Home'));
});

gulp.task('clean', () => gulp.src(['wwwroot/js/*.js', 'wwwroot/lib/*.js'], { read: false })
    .pipe(clean()));

gulp.task('default', gulp.series(['clean', 'build-css-cerulean', 'build-css-space-lab', 'compress-lib', 'compress-app', 'copy-html', 'copy-templates', 'copy-sentry', 'cache-bust-lib', 'cache-bust-lib-min', 'cache-bust-app', 'cache-bust-app-min']));
