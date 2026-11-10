#!/usr/bin/env bash
set -e

# Only start MongoDB when the storage type is MongoDB (the default).
# SQL backends (PostgreSQL, MsSql, Sqlite) use an external database and do not
# need the embedded mongod — starting it wastes memory and can crash under the
# extra load of running alongside heavy containers such as SQL Server.
STORAGE_TYPE="${Cratis__Chronicle__Storage__Type:-MongoDB}"
mongod_pid=""

if [ "${STORAGE_TYPE,,}" = "mongodb" ]; then
  /usr/bin/mongod --replSet "rs0" --bind_ip 0.0.0.0 > /dev/null &
  mongod_pid=$!

  # The kernel requires explicit storage connection details; point it at the embedded instance unless set.
  if [ -z "${Cratis__Chronicle__Storage__ConnectionDetails:-}" ] && [ -z "${Cratis__Chronicle__Storage__ConnectionDetailsFile:-}" ]; then
    export Cratis__Chronicle__Storage__ConnectionDetails="mongodb://localhost:27017"
  fi

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
pending_signal=""

forward_signal() {
  shutdown_requested=true
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

# Reap the server in a shell with no signal traps: Bash can lose a child's status when
# a trapped signal interrupts wait, even with wait -n -p. Send the PID and status through
# a pipe instead. The parent forwards signals to the server, not to this waiting shell.
# The handshake keeps the coprocess alive until its read descriptor has been duplicated;
# Bash closes the original coprocess descriptors when the coprocess exits.
exec {server_stdout_fd}>&1
coproc {
  IFS= read -r start
  set +e
  ./Cratis.Chronicle.Server >&"$server_stdout_fd" &
  child_pid=$!
  printf '%s\n' "$child_pid"
  wait "$child_pid"
  printf '%s\n' "$?"
}
exec {server_status_fd}<&"${COPROC[0]}"
exec {server_stdout_fd}>&-
printf 'start\n' >&"${COPROC[1]}"

# read can also be interrupted by a trap. Retain any partial line and resume reading;
# unlike an interrupted wait, this cannot discard the server's recorded exit status.
read_server_value() {
  server_value=""
  local part read_status
  while :; do
    if IFS= read -r part <&"$server_status_fd"; then
      server_value+=$part
      return 0
    else
      read_status=$?
      server_value+=$part
      [ "$read_status" -gt 128 ] || return "$read_status"
    fi
  done
}

read_server_value
server_pid=$server_value
if [ -n "$pending_signal" ]; then
  kill -s "$pending_signal" "$server_pid" 2>/dev/null || true
fi

# Allow at least 30 seconds of container stop grace for the host's 10-second shutdown
# timeout and the subsequent embedded MongoDB shutdown.
# Keep reporting an unexpected MongoDB exit, but never use an interrupted wait's status
# for the server. Its untrapped waiter records the authoritative status in the pipe.
set +e
wait -n -p completed_pid
exit_status=$?
if [ "$shutdown_requested" = true ] || [ -z "$mongod_pid" ] || [ "${completed_pid:-}" != "$mongod_pid" ]; then
  if read_server_value; then
    exit_status=$server_value
  fi
fi
exec {server_status_fd}<&-

if [ "$shutdown_requested" = true ]; then
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
