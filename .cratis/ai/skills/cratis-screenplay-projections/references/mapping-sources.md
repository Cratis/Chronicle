<!-- cratis-ai-managed: skills/cratis-screenplay-projections/references/mapping-sources.md -->
# Mapping sources and key diagnostics

Read this when choosing what a property mapping reads from, using `$eventContext`, or diagnosing a key error.

## Mapping sources

A property path (`name`, `contactInfo.email`), a literal
(`true`, `"Pending"`, `42`, `null`), `$eventSourceId`, and
`$eventContext.<path>` from the event-context catalog (for example `occurred`,
`sequenceNumber`, `correlationId`, `causedBy.subject`; an unknown path warns with
`PLAY0295`). Templates (`` `${first} ${last}` ``) and `$causedBy.<…>` parse but
do not bind to the executable model; write `$eventContext.causedBy.subject`
instead of `$causedBy.subject`.

⚠️ Only the event source identity (`$eventSourceId`,
`$eventContext.eventSourceId`) runs in the reference runner. Any other
`$eventContext` path in a mapping or key — `occurred`, `sequenceNumber`,
`causedBy.subject` — binds, but the reference execution plan
refuses it, so no specification in that model runs there. Use it when the target
needs it, and report those specifications as needing a target.

`clear <property>` removes a value and is equivalent to `= null`; prefer `clear`.
It takes a dotted path (`clear Owner.Note`) and escapes reserved names
(`clear @with`).

## Key diagnostics

A read model with several flat `key` parts needs that block in every root `from` and
`remove with` (`PLAY0661`/`PLAY0662`); other blocks and reducers there are `PLAY0268`
(see [pdl-grammar.md](pdl-grammar.md)).

A second key on one `from` is error `PLAY0060`. A template expression in a
composite key is error `PLAY0073`; an empty composite key is `PLAY0074`.
