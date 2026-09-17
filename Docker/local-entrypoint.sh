#!/usr/bin/env bash
set -e

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

# The server aborts when something already holds its port, the container exits 134, and every test
# sharing the fixture fails on an unresolved fixture argument. That is a container-start failure, so
# the retry wrapper deliberately does not retry it and the matrix has to be rerun by hand - and the
# log has never said what was holding the port. Name the holder before the bind, and again if a
# supervised process dies. See #4048.
source /app/report-port-holder.sh
chronicle_port="$(resolve_chronicle_port)"
report_port_before_binding "$chronicle_port" "the Chronicle server"

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
