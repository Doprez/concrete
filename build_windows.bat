@echo off
dotnet publish ./Engine/Editor/Editor.csproj -o ./Build/Windows -r win-x64 -c release --sc true