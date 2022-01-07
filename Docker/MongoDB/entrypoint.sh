#!/usr/bin/env bash
set -e

/usr/bin/mongod --replSet "rs0" --bind_ip 0.0.0.0

wait -n
