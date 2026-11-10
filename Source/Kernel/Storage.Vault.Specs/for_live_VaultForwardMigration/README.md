<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Live Vault forward-migration specs

This is the container-backed forward proof for #5746, not a rollback rehearsal or a claim of live acceptance. It runs in `Storage.Vault.Specs` through the existing CI lane with the train's `run-integration` label; there is no new workflow. The production baseline when the proof was added was `183bddc7ff`. Production migration, runtime, fixtures, and public APIs are unchanged.

`LiveVaultForwardMigration` owns one throwaway `hashicorp/vault:1.18.3` dev-mode container and a random synthetic token scoped to that fixture, never a process-global environment variable. Each spec gets a separate KV-v2 mount. All payloads are synthetic. Assertions compare payload equality as booleans, and command output is discarded; neither keys nor tokens are printed. Disposal removes the container and its mounts. The existing `VaultFixture` and `VaultCollection` are not modified or reused as shared authentication state.

## Individually named cases

Every name below is a `when_` context with named facts (no skips):

- `migrating_multiple_addresses`: multiple stores/namespaces, Unicode, tilde and permitted punctuation; exact k1 names, verified counts, intact legacy payloads.
- `migrating_ineligible_delimiters`: slash, query and fragment delimiters require review, not guessed identities.
- `preserving_erasure_fences`: erased-through floor, fingerprint fencing, both erasure payloads, and a soft-deleted erased identity cannot be resurrected at k1.
- `copying_multi_version_history`: copy only the latest current payload; leave legacy version metadata and both readable versions intact. Destination version numbers are not source history.
- `latest_is_soft_deleted`: older readable versions remain at legacy names; do not restore an older value into k1, and do not delete history during review.
- `history_contains_destroyed_versions`: migrate a readable latest value while preserving destroyed-version metadata and unavailability.
- `all_versions_are_tombstones`: both all-soft-deleted and all-destroyed leaves remain unavailable.
- `a_version_has_scheduled_deletion`: confirm a future deletion remains live; require the copy to preserve its deadline or explicitly refuse migration.
- `migrating_an_empty_store`: clean completion and layout marker.
- `store_is_already_migrated`: clean no-op, including version metadata.
- `destination_conflicts`: preserve both values and refuse destructive cleanup.
- `resuming_a_partial_migration`: verify a prior copy without rewriting it; copy the remaining entry.
- `a_marker_already_exists`: a marker alone does not prove the legacy entries migrated.
- `dry_running`: snapshot payload versions, metadata and leaves before/after; no marker, lock, copy or removal writes (the report counts proposed copies).
- `startup_refuses_earlier_material`: actual `EncryptionKeyCommandRunner` Refuse exit 2 before migration, Warn continuation, verification exit 0, then Refuse continuation. Test-only reflection checks exact signatures and fails if the seam changes; the spec project references Server only to make that assembly available.
- `migrating_verifying_and_running_again`: actual command migrate/verify/second-run exits, all-copy readback observed before any source deletion, exact cleanup count, legacy tombstones, second-run metadata/payload stability.
- `verification_fails`: after a real acknowledged Vault copy, inject a failed readback; no deletion attempt or legacy removal is allowed.
- `interrupted_with_a_lock`: pause after a real acknowledged copy, refuse a competitor, cancel and inject failed lock release, refuse the orphaned lock, then explicitly remove it only after its owner has quiesced and reverify the prior copy. This models interruption/recovery, not process-kill behavior or automatic lock stealing.

## Explicit unsupported / unexercised boundaries

**Scheduled deletion is a documented limitation** (independent design check: advisory, accepted by the lane coordinator and Infrastructure). Only the key value is copied; a deletion deadline set by hand on one key outside Chronicle is not transferred. The assertion accepts a preserved deadline, an absent deadline on the copy, or an explicit refusal, and fails only if the copy carries a different deadline. The pinned Vault must support native `delete_version_after`; lack of support fails setup rather than bypassing coverage. Post-deadline wall-clock behavior is unexercised: the proof inspects the native deadline metadata without sleeping or polling.

The erasure assertion concerns migration fencing and unavailable erased material, not a new claim that every explicit-revision legacy runtime read checks erasure records. Historical versions are not copied. The startup command/gate seam is exercised, but a full server/cluster startup and host-process kill are unexercised. A separately required Infrastructure rollback rehearsal remains uncertified and outside this collection.

No containers or integration specs were run locally for this addition. The ordinary non-container Vault specs and the Release warnings-as-errors build are separate checks, not live proof evidence. Existing CI must run the collection before live results can be reported; a green result must not be inferred from test discovery or the local build.
