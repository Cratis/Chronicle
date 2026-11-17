<!-- cratis-ai-managed: skills/cratis-screenplay-reverse-engineering/references/target-and-first-validation.md -->
# Running target, first validation and no-MCP fallback

Short recipes for turning a recovered model into a running Stage app. The commands, verdicts and
syntax are owned by `cratis-screenplay-toolchain`, `cratis-screenplay-render-and-gap-fill` and
`cratis-stage-rendering-and-sandbox`; this page only sequences them.

## From recovered model to running app

1. Validate the folder with warnings as errors, `screenplay <folder> --warnaserror`
   (`cratis-screenplay-toolchain`). Fix the model, never silence a diagnostic.
2. Run the reference specifications, `screenplay test <folder>`. `Unsupported` is not a pass.
   Neither is `unbound` or `0 executed`: the folder still has errors, so every specification was
   skipped. The gate passes only when the command exits 0 and the executed count equals the
   discovered count, apart from specifications that read an opaque performer, each listed by name.
   The usual cause of `unbound` is PLAY0524: an event field stamped from `$context.occurred`
   missing from a `given` or `then`. Paste the final `screenplay test` summary line
   ("N discovered, N executed") into the report; a run without it is not validation.
   Report that executed count, never a per-file compile result.
3. Start it with `cratis run <model folder>` (Stage sandbox, needs Docker), or render with
   `cratis render` and gap-fill (`cratis-screenplay-render-and-gap-fill`).
   Commands are served over HTTP under `/api/`; read each command's route from the generated
   OpenAPI or `/stage/status`, do not guess it.
4. Re-walk the observed journeys on the running app (`fidelity-check.md`).

`render --force`, `prologue` start, the Extractor and `prologue interpret` need approval and stay
outside this recipe.

## First-validation traps

Errors that cost iterations on a first pass; each is confirmed in the toolchain references.

- A scalar type is `Bool`, not `Boolean`; an unknown type is PLAY0165.
- A queryless or list read model needs an explicit `key` mark; a read model with two `*Id`
  properties needs one keyed query (`cratis-screenplay-toolchain` traps 12 and 46).
- A `require` message goes on the next indented line (trap 38).
- `given clock "<ISO timestamp>"` is one line.
- Productions from different streams need explicit destinations (PLAY0470).
- Prove a projection with `given` events, then `when`, then `then readmodel`/`then query`
  (trap 15). When an update-style command (archive, rename, complete) depends on state that
  an earlier event created, give that event in its spec's `given`; a first run that
  reports Unsupported often lacks it. See `cratis-screenplay-specifications`.
- Add a per-command screen only when no data screen carries the action; command forms come
  from actions on data screens.

## Without the Screenplay MCP server

The propose, read, apply loop needs the MCP server. When it is unavailable, write the `.play`
file directly, then validate with `screenplay <folder> --warnaserror` and run
`screenplay test <folder>` after every slice. A diagnostic is still a finding: fix the model,
never weaken protection to pass. Record in the report that the MCP loop was not used. Write
helper scripts to files rather than long inline shell, which guards may block.
