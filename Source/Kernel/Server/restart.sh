#!/bin/bash
docker compose down --remove-orphans
docker compose up -d
dotnet run
