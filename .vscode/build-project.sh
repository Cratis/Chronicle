#!/bin/bash
dotnet build \
    --no-restore \
    /property:GenerateFullPaths=true \
    /consoleloggerparameters:NoSummary \
    $1
