#!/usr/bin/env bash
set -e

# Only start the embedded MongoDB when the kernel is actually going to use it as its own
# storage. Cratis.Chronicle.XUnit.Integration's ChronicleOutOfProcessFixture relies on exactly
# this embedded instance (it never sets Cratis__Chronicle__Storage__ConnectionDetails, so the
# kernel falls back to localhost:27017). Cratis.Chronicle.Integration's ChronicleConfigurableFixture
# never does that - even for MongoDB it always starts a dedicated, separate MongoDB container and
# points ConnectionDetails at that container's hostname - so this embedded mongod would be entirely
# redundant there. Redundant is not harmless: this mongod is a lone-member replica set that can
# never find a sync source ("Could not find member to sync from"), so it re-elects itself every
# few seconds for as long as the container lives, continuously opening and closing loopback
# connections while the Chronicle server's own Kestrel bind to the fixed, in-ephemeral-range port
# 35000 (see Docker/report-port-holder.sh) is still in flight - a plausible source of the
# intermittent "address already in use" container-start failures tracked in #4048.
STORAGE_TYPE="${Cratis__Chronicle__Storage__Type:-MongoDB}"
CONNECTION_DETAILS="${Cratis__Chronicle__Storage__ConnectionDetails:-}"
mongod_pid=""

if [ "${STORAGE_TYPE,,}" = "mongodb" ] && { [ -z "$CONNECTION_DETAILS" ] || [[ "$CONNECTION_DETAILS" == *"localhost"* || "$CONNECTION_DETAILS" == *"127.0.0.1"* ]]; }; then
  /usr/bin/mongod --replSet "rs0" --bind_ip 0.0.0.0 > /dev/null &
  mongod_pid=$!

  # Wait for MongoDB to start
  until mongosh --quiet --eval "db.adminCommand('ping')" > /dev/null 2>&1; do
    sleep 1
  done

  # Initialize replica set if not already initialized
  mongosh --quiet --eval "
try {
  rs.status();
} catch(e) {
  rs.initiate({
    _id: 'rs0',
    members: [{ _id: 0, host: 'localhost:27017' }]
  });
  // Wait for replica set to be ready
  while (rs.status().ok !== 1) {
    sleep(1000);
  }
}
"
fi

# The server aborts when something already holds its port, the container exits 134, and every test
# sharing the fixture fails on an unresolved fixture argument. That is a container-start failure, so
# the retry wrapper deliberately does not retry it and the matrix has to be rerun by hand - and the
# log has never said what was holding the port. Name the holder before the bind, and again if a
# supervised process dies. See #4048.
# Guarded because this entrypoint runs under set -e: a diagnostic that cannot be sourced must degrade
# to silence, never abort the container it exists to explain.
if [ -r /app/report-port-holder.sh ]; then
  source /app/report-port-holder.sh
  report_port_before_binding "$(resolve_chronicle_port)" "the Chronicle server"
else
  echo "Port diagnostics unavailable: /app/report-port-holder.sh is missing."
  report_all_listening_sockets() { echo "  (port diagnostics unavailable)"; }
fi

server_pid=""
shutdown_requested=false
signal_count=0
pending_signal=""

forward_signal() {
  shutdown_requested=true
  signal_count=$((signal_count + 1))
  # Before the launch there is no server to signal; the launch forwards the pending signal.
  # The server may also have exited just before the signal arrived.
  if [ -n "$server_pid" ]; then
    kill -s "$1" "$server_pid" 2>/dev/null || true
  else
    pending_signal=$1
  fi
}
# Install the traps before the launch so a stop request that arrives in between is not lost.
trap 'forward_signal TERM' TERM
# Bash starts a background command with SIGINT ignored, so the server cannot act on an interrupt;
# forward it as TERM, which the server handles as a stop request.
trap 'forward_signal TERM' INT

./Cratis.Chronicle.Server &
server_pid=$!
if [ -n "$pending_signal" ]; then
  kill -s "$pending_signal" "$server_pid" 2>/dev/null || true
fi

# Allow at least 30 seconds of container stop grace for the host's 10-second shutdown
# timeout and the subsequent embedded MongoDB shutdown.
# A trap firing while "wait" is blocked can make bash report 128 + signal even though it
# already reaped the server, discarding its real exit status for good (seen under CI
# scheduling pressure). Poll for the server's own exit first, so the "wait" below rarely has
# to block where a trap could still interrupt it.
set +e
while kill -0 "$server_pid" 2>/dev/null; do
  sleep 0.02
done
wait "$server_pid"
exit_status=$?

if [ "$shutdown_requested" = true ]; then
  # A trap can still land in the instant between the poll above finding the server gone and
  # the "wait" above collecting it, reporting 128 + the forwarded signal instead of the real
  # status even though the server has definitely already exited by then. The server is gone
  # either way, so retry until a real status lands.
  retry_count=0
  while { [ "$exit_status" -eq 143 ] || [ "$exit_status" -eq 130 ]; } && [ "$retry_count" -lt 100 ]; do
    wait "$server_pid"
    exit_status=$?
    retry_count=$((retry_count + 1))
    sleep 0.01
  done

  if [ -n "$mongod_pid" ]; then
    # Keep storage available until the server has drained. Cleanup must not replace
    # the server's exit status, including when mongod has already exited.
    kill -TERM "$mongod_pid" 2>/dev/null || true
    wait "$mongod_pid" || true
  fi
fi
set -e

if [ "$exit_status" -ne 0 ]; then
  echo "A process supervised by this entrypoint exited with status ${exit_status}. Listening sockets now:"
  report_all_listening_sockets
fi

exit "$exit_status"
