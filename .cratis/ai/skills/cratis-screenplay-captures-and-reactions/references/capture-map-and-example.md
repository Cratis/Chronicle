<!-- cratis-ai-managed: skills/cratis-screenplay-captures-and-reactions/references/capture-map-and-example.md -->
# Capture map and example

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
a capture without a `key` does not bind (`PLAY0268`). In a specification the
reference is handed records (`given capture`, `when capture`) and never contacts a
`source`, which stays realization metadata; events append to the event source the
record's `key` names, and `$context.occurred` is the scenario's `given clock`.

An event's external origin is declared on the `event` itself with `event X from "<origin>"` (ESM v9);
outside records still enter through a `capture` in a `Translate` slice
(`cratis-screenplay-automations-and-translations` `references/public-events-and-direction.md`).

**`map`** reshapes properties before events are appended (rename, template, `translate`, `split`; mapping sources are `$.`, `$context.` and `$env.`); syntax is in the reference file linked above.

**`append <Event>`** with optional `tag` lines and one `when` clause:

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
