var gulp = require('gulp');
var $ = require('gulp-load-plugins')();

gulp.task('test', function() {
    gulp.src('')
        .pipe($.phpunit('phpunit --bootstrap ./server/src/bootstrap-mac.php --group target ./server/tests'));
});
