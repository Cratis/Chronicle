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

if [ "${STORAGE_TYPE,,}" = "mongodb" ] && { [ -z "$CONNECTION_DETAILS" ] || [[ "$CONNECTION_DETAILS" == *"localhost"* || "$CONNECTION_DETAILS" == *"127.0.0.1"* ]]; }; then
  /usr/bin/mongod --replSet "rs0" --bind_ip 0.0.0.0 > /dev/null &

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

./Cratis.Chronicle.Server &

# wait -n decides the exit status exactly as it did before; it is captured rather than left to
# set -e only so the failure can be described on the way out.
set +e
wait -n
exit_status=$?
set -e

if [ "$exit_status" -ne 0 ]; then
  echo "A process supervised by this entrypoint exited with status ${exit_status}. Listening sockets now:"
  report_all_listening_sockets
fi

exit "$exit_status"
