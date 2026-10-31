<!-- cratis-ai-managed: skills/cratis-screenplay-event-modeling/references/slice-type-rules.md -->
## Slice type rules

**A slice is one behavior, not one artifact of each kind.** Every construct may
appear as many times as the behavior needs. Only `description` is limited to one.

**Automation has four required components** - a triggering occurrence, the state
it consults (the trigger's `reads`), conditional logic, and a resulting command
or event. If the events are always unconditionally co-produced, it is **not** an
automation: model it as one `StateChange` slice with several `produces` blocks.

**Translation is workflow-specific anti-corruption.** Generic infrastructure every
workflow needs (event persistence, message transport) is not a `Translate` slice.

**Events have no optional properties for the `cratis` target.** An event property
marked `optional` validates, but `cratis render --check` refuses it (`STAGE-ESM-005`).
Make it required, or leave it out of the event; whether the detail is a separate fact is a modeling decision.
