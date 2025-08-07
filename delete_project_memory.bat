@echo off

if exist "%TEMP%\memory.txt" (
    del /f /q "%TEMP%\memory.txt"
)

if exist "%TEMP%\TempConcreteProject" (
    rmdir /s /q "%TEMP%\TempConcreteProject"
)