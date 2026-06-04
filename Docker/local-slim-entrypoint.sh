#!/usr/bin/env bash
set -e

# Kernel-only image — every backing database (MongoDB, PostgreSQL, MSSQL, SQLite)
# is configured by the test harness via Cratis__Chronicle__Storage__* environment
# variables and runs in its own container (or a file, for SQLite).
exec ./Cratis.Chronicle.Server
