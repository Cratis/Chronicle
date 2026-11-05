<!-- cratis-ai-managed: skills/cratis-screenplay-captures-and-reactions/references/capture-map-and-example.md -->
# Capture map and example

## Contents

- Capture excerpt
- Capture `map`
- Keying by the event source

## Capture excerpt

```screenplay excerpt
slice Translate LegacyInvoiceSync
  capture LegacyInvoiceCapture
    source api
      api   LegacyInvoicingApi
      route /invoices
      poll  5m
    key id
    map
      status = status translate
        "utkast" => draft
        "sendt"  => sent
        "betalt" => paid
    append InvoiceStatusChanged
      tag legacy
      when status
        invoiceId = $.id
        status    = $.status
        changedAt = $context.occurred
    append InvoicePaidFromSent
      when status from "sent" to "paid"
        invoiceId = $.id
    children lineItems identified by lineNumber
      append InvoiceLineItemAdded
        when added
          invoiceId  = $.id
          lineNumber = $.lineNumber
```

## Capture `map`

**`map`** reshapes before events are appended: direct rename
(`productName = name`), a backtick template, `translate` with indented
`"source" => target` entries, and `split <source> by "<sep>"` with indented target
properties.

**Mapping sources:** `$.` for a value from the current source item, `$context.` for
capture context, `$env.` for an environment variable, plus literals and templates.

**`source <kind>`** with indented settings. The documented kinds and settings are
`api` (`api`, `route`, `poll`), `webhook` (`path`) and `message` (`topic`).
⚠️ The grammar is **open** — the parser accepts any kind and any setting name as
free-form name/value pairs, so a typo is not caught. The three kinds are
convention, not enforcement. `source events` (`from <Event>` lines, ESM v9) is the exception, with
its own closed grammar (PLAY0610/0612/0616/0619/0620).

**`key <property>`** names the source property identifying an instance. From ESM v6
a capture without a `key` does not bind (`PLAY0268`). For record sources the
reference is handed records (`given capture`, `when capture`) and never contacts a
`source`, which stays realization metadata. For `source events`, present a declared
`from` event with `given <Event>` / `when append <Event>` (PLAY0701 otherwise);
`key` must be a required property of every source event, not a context key
(PLAY0700). Events append to the event source the key names, and
`$context.occurred` is supplied by the scenario's `given clock`.

When every consumed event declares the key as one required scalar property of the same
type (for example a concept over Uuid), the appended event's identity carries that type
instead of Text, and `for` in specifications binds as that type. A model that uses this
selects ESM v32 (Requires Screenplay 4.241.0; `screenplay --version`); untyped, non-scalar or
disagreeing keys keep Text and the earlier version. Rendering needs a Stage that admits v32:
see toolchain [language admission](../../cratis-screenplay-toolchain/references/language-admission.md).

For a producer that sources every event by this key, declare
`key <property> is event source` (ESM v33, Screenplay 4.242.0).
See [Keying by the event source](#keying-by-the-event-source) below for the promise, fixture `for`
checks and the PLAY0809 restriction to single-property `source events` keys.

An event's external origin is declared on the `event` itself with `event X from "<origin>"` (ESM v9);
outside records still enter through a `capture` in a `Translate` slice
(`cratis-screenplay-automations-and-translations` `references/public-events-and-direction.md`).

**`map`** reshapes properties before events are appended (rename, template, `translate`, `split`; mapping sources are `$.`, `$context.` and `$env.`); syntax is in the reference file linked above.

**First observation is added only**, at root, child and nested levels. Field-change
and transition guards need a previous record of the same key (`given capture`), or
earlier typed events for `source events`. Use a separate `when added` append for
first sight; keep the field guard for updates. Maps, expression guards and unguarded
record appends are unchanged. Complete runnable record cases:
[record-capture-specifications.md](record-capture-specifications.md).

**`append <Event>`** takes optional `tag` lines and at most one `when` clause:

| `when` form | Appends when |
| --- | --- |
| `when added` | an item appears in the source |
| `when removed` | an item disappears from the source |
| `when <Path>` | that property changes |
| `when <Path> from <v> to <v>` | that property makes that exact transition |
| `when <a> or <b> [or <c>]` | any of those properties change |
| `when <a> and <b> [and <c>]` | all of those properties change |
| ``when `<expression>` `` | a raw template expression is true (captured verbatim; from ESM v6 the reference evaluates fields, literals, `==`, `!=`, the ordering operators, `&&`, `||`, `!` and parentheses, and an expression outside that grammar does not bind) |

⚠️ **`or` and `and` cannot be mixed in one clause** — `when a or b and c` is a
compile error. Split it into two `append` blocks.

**`children <collection> identified by <path>`** and **`nested <path>`** each take
an optional `map` and any number of `append` blocks.

## Keying by the event source

Requires Screenplay 4.242.0 or later (`screenplay --version`). The marker selects
ESM v33 and serializes `keyIsEventSource` beside `sourceEvents`. A capture without
it keeps its earlier behavior, version and bytes.

Declare `key <property> is event source` only when the producer sources **every**
captured event by that property's value. This is a promise about the foreign
stream, not a request to move an event to another source. It lets a renderer that
admits v33 narrow an inbox history read by event source id. Binding and reference
execution do not prove the renderer or deployed observer realizes that promise.

Only a `source events` capture with a single property key may declare the marker.
Other capture sources or key shapes report PLAY0809, an error with no automatic
repair. The key must still be required on every `from` event (PLAY0700); the marker
does not permit `$eventSourceId` as the key expression.

### Typed fixtures

- An explicit fixture `for` must equal the key. A mismatch rejects execution with
  a contract rejection naming the capture, event and both values. Do not turn it
  into an accepted `then no events` scenario.
- A fixture without `for` takes the key as its source **before** projections and
  reactions see it, so `$eventSourceId` is the key.
- The check also runs while establishing givens, including a world with no action.
- Without the marker, an optional foreign fixture `for` stays opaque non-empty
  text, independent of the capture key. Do not add the marker just to fix a fixture.

This complete model uses a producer that sources dispatches by `orderId`:

```screenplay
module Shipping
  feature Tracking
    slice Translate ReceiveDispatches
      direction inbound
      event ParcelDispatched from "carrier"
        orderId String
      event DispatchRecorded
        orderId String
      capture DispatchFeed
        source events
          from ParcelDispatched
        key orderId is event source
        append DispatchRecorded
          when added
            orderId = $.orderId
      specification RecordingADispatch
        when append ParcelDispatched
          for "order-42"
          orderId = "order-42"
        then DispatchRecorded
          for "order-42"
          orderId = "order-42"
      specification TakingTheKeyAsSource
        when append ParcelDispatched
          orderId = "order-42"
        then DispatchRecorded
          for "order-42"
          orderId = "order-42"
```

Changing the first fixture's `for` to `"other-order"` rejects the run; it is not a
new stream to accept silently. Test that rejection and the given-only mismatch
through evaluator outcomes as well as the two successful source forms.

Authority: Screenplay [captures](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/captures.md#keying-by-the-event-source)
and [capture grammar](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/captures/grammar.md).
