#!/usr/bin/env bash
set -e

FLAG_FILE="/data/db/.initialized"
LOG_FILE="/var/log/mongodb/mongo.log"
mkdir -p /var/log/mongodb
touch "$LOG_FILE"
chown -R mongodb:mongodb /var/log/mongodb

if [ ! -f "$FLAG_FILE" ]; then
    echo "Initializing MongoDB as a single node cluster"

    mkdir -p /data/db /data/configdb
    chown -R mongodb:mongodb /data/db /data/configdb

    echo "Starting MongoDB for initialization..."
    mongod --logpath /var/log/mongodb/initdb.log --replSet "rs0" --bind_ip 0.0.0.0 --fork || true

    echo "Initializing replica set..."
    mongosh --eval 'rs.initiate({_id: "rs0", members: [{ _id: 0, host: "localhost:27017"}]})' || true

    echo "Shutting down MongoDB after initialization..."
    mongod --shutdown || true
    touch "$FLAG_FILE"
fi

/usr/bin/mongod --replSet "rs0" --bind_ip 0.0.0.0 > /dev/null &
./Cratis.Chronicle.Server &

wait -n
