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
    [ -n "${CERT_DIR:-}" ] && rm -rf "$CERT_DIR"
    return 0
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

# Writes a throwaway PKCS#12 certificate for the variants that require one.
#
# The single Chronicle port serves gRPC and HTTP/1.1 together, which it can only do over TLS, so the
# production image refuses to start without a certificate - deliberately, with a clear message. A boot
# test that omits one is not testing the image, it is re-proving that requirement, which is how this
# gate came to fail every production publish on the day it landed. Development generates its own
# certificate when none is configured, so only production is given one here.
#
# The same certificate serves both requirements the production image has: the TLS listener, and
# OpenIddict's token encryption, which is also mandatory in production and refuses to start without
# one. Sharing a throwaway certificate between them is fine for a boot test and wrong for a
# deployment, where the two have different lifetimes and rotation stories.
write_certificate() {
    CERT_DIR="$(mktemp -d)"
    openssl req -x509 -newkey rsa:2048 -sha256 -days 1 -nodes \
        -keyout "$CERT_DIR/key.pem" -out "$CERT_DIR/cert.pem" -subj "/CN=localhost" > /dev/null 2>&1
    openssl pkcs12 -export -out "$CERT_DIR/boot-test.pfx" \
        -inkey "$CERT_DIR/key.pem" -in "$CERT_DIR/cert.pem" -passout pass: > /dev/null 2>&1
    chmod 0644 "$CERT_DIR/boot-test.pfx"
}

PORT=35000
# The port on the host, so a machine already running Chronicle can still run this.
HOST_PORT="${BOOT_TEST_HOST_PORT:-$PORT}"
case "$VARIANT" in
    development)
        # The documented zero-configuration consumer invocation - no environment, embedded MongoDB.
        docker run -d --name "$SERVER" --network "$NETWORK" -p "$HOST_PORT:$PORT" "$IMAGE" > /dev/null
        ;;
    production|development-slim)
        # These images use external storage - run them the way Docker/README.md documents.
        start_mongo
        write_certificate
        docker run -d --name "$SERVER" --network "$NETWORK" -p "$HOST_PORT:$PORT" \
            -v "$CERT_DIR:/boot-test-certificate:ro" \
            -e Cratis__Chronicle__Tls__CertificatePath=/boot-test-certificate/boot-test.pfx \
            -e Cratis__Chronicle__EncryptionCertificate__CertificatePath=/boot-test-certificate/boot-test.pfx \
            -e Cratis__Chronicle__Storage__Type=MongoDB \
            -e "Cratis__Chronicle__Storage__ConnectionDetails=mongodb://$MONGO:27017" \
            "$IMAGE" > /dev/null
        ;;
    workbench)
        PORT=80
        HOST_PORT="${BOOT_TEST_HOST_PORT:-8080}"
        docker run -d --name "$SERVER" -p "$HOST_PORT:$PORT" "$IMAGE" > /dev/null
        ;;
    *)
        echo "Unknown variant: $VARIANT" >&2
        exit 64
        ;;
esac

echo "Waiting up to ${TIMEOUT}s for $IMAGE to listen on port $HOST_PORT..."
for i in $(seq 1 "$TIMEOUT"); do
    state=$(docker inspect -f '{{.State.Status}} {{.State.ExitCode}}' "$SERVER")
    if [[ "$state" != running* ]]; then
        fail "container stopped (state: $state) after ${i}s"
    fi
    if (exec 3<> "/dev/tcp/127.0.0.1/$HOST_PORT") 2>/dev/null; then
        echo "$IMAGE is listening on port $HOST_PORT after ${i}s - confirming it stays up..."
        sleep 10
        state=$(docker inspect -f '{{.State.Status}} {{.State.ExitCode}}' "$SERVER")
        if [[ "$state" != running* ]]; then
            fail "container bound port $HOST_PORT but stopped right after (state: $state)"
        fi
        echo "$IMAGE is running and listening on port $HOST_PORT."
        exit 0
    fi
    sleep 1
done
fail "never listened on port $HOST_PORT within ${TIMEOUT}s (container still running - startup hang)"
