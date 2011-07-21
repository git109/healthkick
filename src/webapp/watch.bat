@echo off
echo ******************************************************************
echo    SOURCE CODE WATCHER (HAML,COMPASS+SASS,LIVERELOAD)
echo ******************************************************************

start /B ruby haml_watch.rb
start /B compass watch
start /B livereload
start /B c:\ncoffee\ncoffee -w .
