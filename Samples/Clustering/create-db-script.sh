#!/bin/bash
echo CREATE DATABASE Orleans > create-db.sql
echo USE Orleans >> create-db.sql
wget -O - https://raw.githubusercontent.com/dotnet/orleans/main/src/AdoNet/Shared/SQLServer-Main.sql >> create-db.sql
wget -O - https://raw.githubusercontent.com/dotnet/orleans/main/src/AdoNet/Orleans.Persistence.AdoNet/SQLServer-Persistence.sql >> create-db.sql
wget -O - https://raw.githubusercontent.com/dotnet/orleans/main/src/AdoNet/Orleans.Reminders.AdoNet/SQLServer-Reminders.sql >> create-db.sql
wget -O - https://raw.githubusercontent.com/dotnet/orleans/main/src/AdoNet/Orleans.Clustering.AdoNet/SQLServer-Clustering.sql >> create-db.sql

