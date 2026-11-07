<!-- cratis-ai-managed: skills/cratis-engineering-local-cross-repo-verification/references/train-mode.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Train mode

A local release train combines several branches into one pull request, one CI run and one
release. Verify a downstream repository against the upstream train's packages, not against
per-branch packages.

## Stamp

`stamp --train PRODUCER...` prints `X.Y.Z-train.g<sha7>`. X.Y.Z is the highest next patch over the
producers' latest reachable tags, so no stamped package sorts below a released pin. sha7 is the first
seven hex characters of the SHA-1 of the sorted `<repository> <full HEAD sha>` lines of every
producer (the repository is its origin's last path part, else its folder name). There is no minute: one
combined code state yields one stamp, a change in any producer changes it, and the order of the
producers does not matter. The grammar is fixed so tools can rely on it. Every command that takes a
stamp accepts it: `pack-*`, `use-*`, `verify-*`, `check` and `clean --stamp`. A train stamp
sorts above the released pin like a local stamp does.

## Rules

- One stamp per train state. The train tool records it in the train manifest; read it from there
  instead of recomputing it for a different HEAD.
- A fold into the upstream train changes the upstream HEAD and so the stamp. Downstream
  verification against the old stamp is stale: pack again, overlay the new stamp, rebuild and
  re-verify. Do not carry a pass over a fold.
- Train versions are as local as local stamps. `check` reports `-train.g<sha7>` in changed
  tracked, staged and untracked files, so a train version never reaches a commit or pull request.
- Upstream first. The downstream ships only after the upstream has released and the downstream pin
  is bumped to the released version; run `check` (0 leaks) before the downstream push.

## Recording consumed versions

`pack-*` and `use-*` accept `--task ID` (or `CRATIS_LOCAL_TASK`, convention
`owner/repo#number`). Successful packs record producer HEADs and IDs in the stamp
registry; use records consumers. `list [--json]` includes train stamps and their
owners, tasks and consumers. `finish --task ID` previews completion; add `--apply`
to mark the task finished without removing artifacts. See
[Registry and list](dotnet-nuget.md#registry-and-list).

`verify-pack STAMP --consumer C --producer P... --json` and
`verify-assets CONSUMER STAMP [--npm] [--consumer C --producer P...] --json` print one line:
`{"stamp":"...","packages":[{"id":"...","version":"..."}]}`. Findings go to stderr and the exit status
is unchanged, so a train tool can record the consumed versions without scraping text.

## Clean

`clean --stamp STAMP` lists and removes the feed files, cache folders and Yarn cache entries of
that one train stamp; a local stamp is not matched. Review the dry run before `--apply`.
Applying also removes `FEED/stamps/STAMP`, including its registry record.

For retention-based cleanup of both local and train stamps, review `clean --expired`
before adding `--apply`. Unrecorded stamps, active compositions and live references
are kept; otherwise finished producing tasks, all consumers gone, or a real release
at or above the base can expire a stamp. Offline evidence uses NuGet shared/private
caches; `--online` opts into NuGet/npm registry checks with a 10-second timeout.
Network failure is not evidence. Kept stamps succeed (`0`); apply also prunes gone
consumers and missing sessions. `--expired` cannot combine with `--stamp` or `--all`.
