<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/stream-id-rules.md -->
# Stream-id literals, composite ids and the reserved `All` name

Rules for stream-id literals, composite ids and the reserved `All` stream name.

- Stream-id literals must be nonempty, well-formed NFC text (refused,
  not normalized); integer ids in Double mode stay within ±(2^53−1). Exact mode
  has no such bound but is unadmitted. PLAY0504 is an error for route/destination
  nominal type mismatch. Composite ids use at least two named scalar parts,
  complete named mappings and canonical comparison, not guessed legacy splits.
  Stored key-schema changes require a new stream identity or migration; see
  toolchain `references/sources-and-streams.md`. A stored stream name exactly `All` (name or `id "All"`) refuses binding with
  PLAY0273, parallel to source `Default`. Streams `all`, `ALL` and `Default`
  remain valid. Rename via `propose-rename`; a pin retaining `All` cannot fix it.
  Existing `All` history reads as source-only appends, not the renamed stream.
  Source evidence and migration limits are in that reference.
