#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Name the process inside this container that is listening on a TCP port.
#
# The kernel intermittently aborts at startup with "Failed to bind to address
# https://[::]:35000: address already in use", the container exits 134, and every test sharing
# that fixture fails with an unresolved fixture argument. Because it is a container-start failure
# rather than a test failure the retry wrapper deliberately does not retry it, so the whole matrix
# has to be rerun by hand - and the log never says what was already holding the port. See #4048.
#
# This reads /proc directly rather than calling ss, lsof or netstat: none of them is guaranteed to
# be in these images, and a diagnostic that needs a package installed before it can report is a
# diagnostic that is missing on the day it is needed.

# Print every listening socket on the given port, with the process holding it.
# Returns 0 when at least one holder was found, 1 when the port is free.
report_port_holder() {
    local port="$1"
    local hex_port
    hex_port="$(printf '%04X' "$port")"

    local inodes=()
    local file
    for file in /proc/net/tcp /proc/net/tcp6; do
        [ -r "$file" ] || continue

        local slot local_address remote_address state queues timers retransmits uid timeout inode
        while read -r slot local_address remote_address state queues timers retransmits uid timeout inode _; do
            # 0A is TCP_LISTEN. Anything else is a connection, not something that blocks a bind.
            [ "$state" = "0A" ] || continue
            [ "${local_address##*:}" = "$hex_port" ] || continue
            inodes+=("$inode")
        done < <(tail -n +2 "$file")
    done

    if [ ${#inodes[@]} -eq 0 ]; then
        return 1
    fi

    local inode
    for inode in "${inodes[@]}"; do
        local identified=0
        local fd
        for fd in /proc/[0-9]*/fd/*; do
            [ -e "$fd" ] || continue

            local target
            target="$(readlink "$fd" 2>/dev/null)" || continue
            [ "$target" = "socket:[$inode]" ] || continue

            local pid
            pid="$(printf '%s' "$fd" | cut -d/ -f3)"

            local command="unknown"
            [ -r "/proc/$pid/comm" ] && command="$(cat "/proc/$pid/comm" 2>/dev/null)"

            local arguments=""
            if [ -r "/proc/$pid/cmdline" ]; then
                # Newlines are folded away too, so one holder is always one line in the job log.
                arguments="$(tr '\0\n' '  ' < "/proc/$pid/cmdline" 2>/dev/null)"
            fi

            echo "  port $port is held by pid $pid ($command): ${arguments:-no command line}"
            identified=1
            break
        done

        if [ "$identified" -eq 0 ]; then
            # A socket with no owning file descriptor in this namespace - most often a process that
            # has already exited leaving the socket in TIME_WAIT, or one owned by another container
            # sharing this network namespace. Worth saying out loud rather than reporting nothing.
            echo "  port $port is held by socket inode $inode, which no visible process owns"
        fi
    done

    return 0
}

# List every listening socket in this container, with the process holding it.
#
# The pre-flight check below looks at one port, so it is only ever as right as its idea of which
# port the server will bind. This is the port-agnostic fallback: when the server has already failed,
# print everything, so a wrong guess about the port cannot hide the answer.
report_all_listening_sockets() {
    local file
    local ports=()

    for file in /proc/net/tcp /proc/net/tcp6; do
        [ -r "$file" ] || continue

        local slot local_address remote_address state
        while read -r slot local_address remote_address state _; do
            [ "$state" = "0A" ] || continue
            ports+=("$((16#${local_address##*:}))")
        done < <(tail -n +2 "$file")
    done

    if [ ${#ports[@]} -eq 0 ]; then
        echo "  nothing is listening inside this container."
        return 0
    fi

    local port
    for port in $(printf '%s\n' "${ports[@]}" | sort -n -u); do
        report_port_holder "$port"
    done

    return 0
}

# Resolve the port the Chronicle server is expected to bind, preferring the configuration it actually
# reads over a literal repeated here. CHRONICLE_DIAGNOSTIC_PORT overrides it for a container that
# moves the port some other way.
resolve_chronicle_port() {
    if [ -n "${CHRONICLE_DIAGNOSTIC_PORT:-}" ]; then
        echo "$CHRONICLE_DIAGNOSTIC_PORT"
        return 0
    fi

    local configured=""
    if [ -r /app/chronicle.json ]; then
        configured="$(sed -nE 's/^[[:space:]]*"port"[[:space:]]*:[[:space:]]*([0-9]+).*/\1/p' /app/chronicle.json | head -1)"
    fi

    echo "${configured:-35000}"
}

# Report the state of a port before something tries to bind it. Always succeeds, so it can never
# be the reason a container fails to start - it only ever adds to the log.
report_port_before_binding() {
    local port="$1"
    local label="${2:-the server}"

    if report_port_holder "$port"; then
        echo "::warning::Port $port is already in use inside this container before $label starts."
    else
        echo "Port $port is free before $label starts."
    fi

    return 0
}
