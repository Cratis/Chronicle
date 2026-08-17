#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Boots a freshly built Chronicle image the way a consumer would and fails unless it comes up healthy.
# Runs in publish.yml before an image is pushed: a published image that cannot start must fail the
# publish, not the consumer - server images 15.37.0-15.38.2 shipped unable to start at all, and every
# development image from 16.33.1 to 16.34.x aborted at startup, because nothing ever booted what was pushed.
#
# Usage: boot-test.sh <image> <variant>
#   variant: development | development-slim | production | workbench

set -euo pipefail

IMAGE="$1"
VARIANT="$2"

NETWORK="boot-test-net"
SERVER="boot-test-server"
MONGO="boot-test-mongo"
TIMEOUT="${BOOT_TEST_TIMEOUT:-180}"

cleanup() {
    docker rm -f "$SERVER" "$MONGO" > /dev/null 2>&1 || true
    docker network rm "$NETWORK" > /dev/null 2>&1 || true
}
trap cleanup EXIT

fail() {
    echo "::error::Boot test failed for $IMAGE ($VARIANT): $1"
    echo "--- container logs ($SERVER) ---"
    docker logs "$SERVER" 2>&1 || true
    exit 1
}

start_mongo() {
    docker run -d --name "$MONGO" --network "$NETWORK" mongo:8 --replSet rs0 --bind_ip_all > /dev/null
    for _ in $(seq 1 60); do
        if docker exec "$MONGO" mongosh --quiet --eval "db.adminCommand('ping')" > /dev/null 2>&1; then
            docker exec "$MONGO" mongosh --quiet --eval \
                "try { rs.status(); } catch (e) { rs.initiate({_id: 'rs0', members: [{_id: 0, host: '$MONGO:27017'}]}); }" > /dev/null
            return
        fi
        sleep 1
    done
    echo "::error::MongoDB for the boot test never became ready"
    docker logs "$MONGO" 2>&1 || true
    exit 1
}

docker network create "$NETWORK" > /dev/null

PORT=35000
case "$VARIANT" in
    development)
        # The documented zero-configuration consumer invocation - no environment, embedded MongoDB.
        docker run -d --name "$SERVER" --network "$NETWORK" -p 35000:35000 "$IMAGE" > /dev/null
        ;;
    production|development-slim)
        # These images use external storage - run them the way Docker/README.md documents.
        start_mongo
        docker run -d --name "$SERVER" --network "$NETWORK" -p 35000:35000 \
            -e Cratis__Chronicle__Storage__Type=MongoDB \
            -e "Cratis__Chronicle__Storage__ConnectionDetails=mongodb://$MONGO:27017" \
            "$IMAGE" > /dev/null
        ;;
    workbench)
        docker run -d --name "$SERVER" -p 8080:80 "$IMAGE" > /dev/null
        PORT=8080
        ;;
    *)
        echo "Unknown variant: $VARIANT" >&2
        exit 64
        ;;
esac

echo "Waiting up to ${TIMEOUT}s for $IMAGE to listen on port $PORT..."
for i in $(seq 1 "$TIMEOUT"); do
    state=$(docker inspect -f '{{.State.Status}} {{.State.ExitCode}}' "$SERVER")
    if [[ "$state" != running* ]]; then
        fail "container stopped (state: $state) after ${i}s"
    fi
    if (exec 3<> "/dev/tcp/127.0.0.1/$PORT") 2>/dev/null; then
        echo "$IMAGE is listening on port $PORT after ${i}s - confirming it stays up..."
        sleep 10
        state=$(docker inspect -f '{{.State.Status}} {{.State.ExitCode}}' "$SERVER")
        if [[ "$state" != running* ]]; then
            fail "container bound port $PORT but stopped right after (state: $state)"
        fi
        echo "$IMAGE is running and listening on port $PORT."
        exit 0
    fi
    sleep 1
done
fail "never listened on port $PORT within ${TIMEOUT}s (container still running - startup hang)"
