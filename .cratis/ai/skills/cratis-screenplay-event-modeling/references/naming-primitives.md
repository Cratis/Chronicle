<!-- cratis-ai-managed: skills/cratis-screenplay-event-modeling/references/naming-primitives.md -->
## Naming the primitives is the highest-value work

- **Concepts before events.** `concept InvoiceId : Uuid` once, and every construct
  using it is typed end to end. The seven primitives are `Uuid`, `String`, `Int`,
  `Decimal`, `Bool`, `Date` and `DateTime`; `Enum` is a separate concept kind,
  with its values indented beneath.
- **Classify personal data at the concept**, with a reason:
  `concept PersonName : String pii` plus an indented `pii reason "..."`. Every
  usage inherits it; a reason for an attribute the concept does not declare is an
  error. Decide this before fixing event shapes; purpose/basis/retention belong in
  processing-purpose declarations, not a reason. An event subject defaults to the
  source unless supplied; the trailing event-property `subject` mark is report-only,
 not runtime lineage. See command-surface `references/compliance.md`.
- **Events are past tense and self-describing** - `InvoiceRegistered`, never
  `Created`. One purpose per event; an event needing an optional property to cover
  two situations is two events.
- **The event-source identity is never an event property.** The command binds it
  with `identifier` on at most one property; marking an *event* property
  `identifier` is an error: *an event never carries its event source id*.
- **Inline events remain slice-owned contracts.** A command may introduce one
  with `produces event <Name>` and typed mappings; other slices still consume it
  by name. Extracting it changes placement, not its contract or projection meaning.
- **Storage identity is not payload identity.** `for` selects the event source;
  `id "<old name>"` preserves an event type's old persisted name on rename. New
  events omit the pin, and the pin does not replace catalog identity. Do not copy
  the same-source identifier into payload merely to project it; use `$eventSourceId`.
- **Domain facts, not runtime context.** Test: would this field have the same value
  if the event were replayed on a different machine? If not, it does not belong.
