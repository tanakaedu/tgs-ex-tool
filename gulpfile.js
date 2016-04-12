var gulp = require('gulp');
var $ = require('gulp-load-plugins')();
var settings = require('./settings.js');

gulp.task('test', function() {
    gulp.src('')
        .pipe($.phpunit('phpunit --bootstrap ./server/src/bootstrap-mac.php --group target ./server/tests'));
});

gulp.task('test-sakura', function() {
    gulp.src('')
        .pipe($.phpunit('phpunit --bootstrap ./server/src/bootstrap-sakura.php --group target ./server/tests'));
});

gulp.task('deploy-index', function() {
    gulp.src('server/api/*')
        .pipe($.ftp({
            host: settings.FTP_URL,
            user: settings.FTP_USER,
            pass: settings.FTP_PASS,
            remotePath: settings.FTP_REMOTE_PATH+'/api'
        }));
});

gulp.task('deploy-router', function() {
    gulp.src('server/vendor/slim/slim/Slim/Router.php')
        .pipe($.ftp({
            host: settings.FTP_URL,
            user: settings.FTP_USER,
            pass: settings.FTP_PASS,
            remotePath: settings.FTP_REMOTE_PATH+'/vendor/slim/slim/Slim/'
        }));
});

gulp.task('deploy', function() {
    var dirs = [
        'config',
        'api',
        'src',
        'src/db',
        'src/libs',
        'templates',
    ];
    for (var dir in dirs)
    {
        gulp.src('server/'+dirs[dir]+'/*')
            .pipe($.ftp({
                host: settings.FTP_URL,
                user: settings.FTP_USER,
                pass: settings.FTP_PASS,
                remotePath: settings.FTP_REMOTE_PATH+'/'+dirs[dir]
            }));
    }

});
