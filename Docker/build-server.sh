#!/bin/bash
CPU=$(dpkg --print-architecture)
if [ $CPU == "amd64" ]; then
    PID="linux-x64"
else
    PID="linux-arm64"
fi

dotnet publish -c Release -r $PID -p:PublishReadyToRun=true --self-contained -o out
