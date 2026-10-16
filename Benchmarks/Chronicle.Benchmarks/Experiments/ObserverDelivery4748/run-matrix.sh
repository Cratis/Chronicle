#!/bin/bash
# Sequential step-2 matrix driver (orchestrator). One pi-phase run per row; per-row exit codes in status.tsv.
# Stops on: exit 1 (correctness DEFECT), 3 consecutive non-zero non-75 exits, 10 admission failures for one row.
cd "$(dirname "$0")"
start=${1:-1}; end=${2:-128}
consec=0
for i in $(seq -f '%03g' "$start" "$end"); do
  for attempt in $(seq 1 10); do
    # A previous row's fixture that failed to clean up under load holds our task-unique ports: remove only that.
    for c in $(docker ps -a --filter ancestor=cratis/chronicle:bench-4748 --filter publish=27448 -q); do
      echo "cleanup: removing leftover task container $c before row $i"; docker rm -f "$c" >/dev/null
    done
    bash runs/$i.sh; rc=$?
    echo -e "$i\t$attempt\t$rc\t$(date -u +%FT%TZ)" >> status.tsv
    [ $rc -ne 75 ] && break
  done
  if [ $rc -eq 75 ]; then echo "STOP: row $i admission exhausted"; exit 75; fi
  if [ $rc -eq 1 ]; then echo "STOP: row $i DEFECT (exit 1)"; exit 1; fi
  if [ $rc -ne 0 ]; then consec=$((consec+1)); else consec=0; fi
  if [ $consec -ge 3 ]; then echo "STOP: 3 consecutive failures ending at row $i"; exit 3; fi
done
echo "DONE rows $start-$end"
