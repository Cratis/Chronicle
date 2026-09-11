---
applyTo: "**/*"
---
<!-- cratis-ai-managed: rules/exit-codes-and-wrappers.md -->

# Exit codes and wrappers

An exit code is a verdict, and a wrapper that loses it turns a red run green. Every line
is tagged **[contract]** (binding) or **[convention]** (the house default) per the Three
Levels of Authority in [`general.md`](./general.md).

- **[contract] Three codes, three meanings.** `0` ran clean, `1` found defects, `2` could
  not run. A tool that cannot distinguish "found nothing" from "never looked" has no
  usable verdict.
- **[contract] `2` is never reported as a pass.** Could-not-run is an unknown, and
  unknown is not pass; see [`verification-discipline.md`](./verification-discipline.md).
- **[contract] A wrapper's own success is not the child's verdict.** A script that runs a
  checker and then exits `0` because *the script* finished has thrown the result away.
  Propagate the child's status.
- **[contract] Pipelines and loops lose exit codes by default.** Use `set -euo pipefail`,
  check `PIPESTATUS` where a pipeline's left side matters, and accumulate a failure flag
  inside a loop rather than relying on the last iteration.
- **[contract] Green prints counts.** A clean run says how many subjects it examined. A
  bare "OK" cannot be distinguished from a run over an empty set; see
  [`guards-and-fuses.md`](./guards-and-fuses.md).
- **[contract] `--self-test` plants defects.** A checker ships a self-test that seeds
  known violations and fails if it does not find every one of them. That is the only way
  to know the checker still detects anything.
- **[contract] Never swallow output to make a gate quiet.** Redirecting stderr, adding
  `|| true`, or catching and ignoring is a decision to stop checking. Say so out loud or
  do not do it.
- **[convention] Name the subject in the failure line.** The path, the id, the rule — a
  failure a reader cannot locate costs more than it saves.
- **[convention] Keep the wrapper thin.** Logic in a wrapper is logic no test covers.
