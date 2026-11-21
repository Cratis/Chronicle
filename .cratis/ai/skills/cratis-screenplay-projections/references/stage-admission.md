<!-- cratis-ai-managed: skills/cratis-screenplay-projections/references/stage-admission.md -->
# `STAGE-ESM-017` on a projection passes `validate` and fails at `render`

Read this when `cratis render` refuses a projection that `cratis screenplay validate --executable` accepted.

Symptom: `cratis screenplay validate --executable` exits 0, then `cratis render` (or `render --check`)
refuses with `STAGE-ESM-017` and a generic message whose location is a `sem1:<hash>`, not a source line
(seen on older Stage and CLI builds). Cause: `validate --executable` binds only Screenplay's model;
Stage's admission runs only in `render`. Check every `from` block in turn:

1. **Key.** An event property or the event source. A root `from` also accepts a constant text key (nonempty, only letters, digits, `_`, space and `. / : * + -`); any other literal key, and a literal key outside a root, is refused.
2. **Do not overwrite the key.** A mapping onto the key property must restate the key itself (`from E` keyed by the event source with `berthId = $eventSourceId`, or the key's own event property); mapping a different value onto it is refused (`MatchesKey`). Simplest is to leave the key property unmapped.
3. **`$eventSourceId` into a non-key target** is admitted in a `from` block keyed by an event property, when every command and reaction producing the event addresses it with a non-collection value of exactly the target's type (optionality ignored). It is refused in `join`s, for a mismatched type, and for an event nothing produces.
4. **No `$context.*`.** Map an event property instead and have the command set it. The compiler may refuse it first (`PLAY0152`); Stage refuses a context value in a projection with `STAGE-ESM-017`.
5. **Targets.** A target path through an optional or non-composite intermediate is refused; an optional leaf and a path through required composites are fine. The event property mapped from must exist on the event.

Bisect: run `cratis render <root> --target cratis --name <App> --destination <dir> --check` (plans,
writes nothing), comment out `from` blocks one at a time until the error clears, then apply the list to
the block that cleared it. Reference: `cratis-stage-rendering-and-sandbox` `references/admission.md`.
