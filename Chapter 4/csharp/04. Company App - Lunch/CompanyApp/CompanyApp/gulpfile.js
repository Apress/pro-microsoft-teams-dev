/// <binding AfterBuild='packageCreation' />
const gulp = require('gulp');
const zip = require('gulp-zip');
 
gulp.task('packageCreation', () =>
    gulp.src('TeamsManifest/*')
    .pipe(zip('manifest.zip'))
    .pipe(gulp.dest('bin\\debug'))
);