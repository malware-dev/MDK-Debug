@echo off
if not '%1' == 'Release' goto exit
XCopy /Y /I %2 %3

:exit