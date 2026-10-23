<!-- cratis-ai-managed: skills/cratis-screenplay-streams-and-consistency/references/stream-id-rules.md -->
# Stream-id literals, composite ids and the reserved `All` name

Rules for stream-id literals, composite ids and the reserved `All` stream name.

- Stream-id literals must be nonempty, well-formed NFC text (refused,
  not normalized); integer ids in Double mode stay within ±(2^53−1). Exact mode
  has no such bound but is unadmitted. PLAY0504 is an error for route/destination
  nominal type mismatch. Composite ids use at least two named scalar parts,
  complete named mappings and canonical comparison, not guessed legacy splits.
  Stored key-schema changes require a new stream identity or migration; see
  toolchain `references/sources-and-streams.md`. A stored stream name exactly `All` (name or `id "All"`) is refused with
  PLAY0802 at authoring time, parallel to stored source `Default`; binding keeps
  PLAY0273 as a backstop. Streams `all`, `ALL` and `Default`
  remain valid. Rename via `propose-rename`; a pin retaining `All` cannot fix it.
  Existing `All` history reads as source-only appends, not the renamed stream.
  Rename a composite part with `propose-rename` too: it rewrites the part and every
  `part = ...` mapping together and refuses a name that collides with another part;
  never hand-write a `propose-ast` batch for it (needs a server that supports part
  renames, see model-authoring `references/mcp-tools.md`).
  Source evidence and migration limits are in that reference.
