<!-- cratis-ai-managed: skills/cratis-screenplay-read-surface/references/passive.md -->
# Passive read models

## Contents

- When to declare `passive`
- Syntax
- What a passive read model is, and is not
- Diagnostics
- Compiler support

## When to declare `passive`

Declare a read model `passive` only when it exists to inform a command's decision and no query or
screen shows it. A command names it in `reads`; the realization replays the key's events for each
decision instead of keeping a stored copy. Without the directive the read model is active, which
is the default and the right choice for anything a user reads.

`passive` is an explicit choice. A read model is never made passive because of how it happens to be
used, and never made active because a command reads it. Declare it, or leave it out.

## Syntax

`passive` is a bare line in the read model body, beside `description`, `file` and `sink`. Excerpt,
inside a `StateChange` slice:

```screenplay excerpt
readmodel InvoiceReservationState
  passive
  invoiceId InvoiceId key
  isReserved Bool
```

The command reads it, and an ordinary projection or reducer builds it; `passive` changes only how
the read model is realized:

```screenplay excerpt
command ReserveInvoiceNumber
  reads InvoiceReservationState as reservation by invoiceId
  validate
    require reservation.isReserved == false
```

Only the exact line `passive` is the directive:

- `passive Bool` is a property named `passive`.
- `passive true`, `passive false` and `readmodel Name passive` are errors.

## What a passive read model is, and is not

- Realization is never inferred. The directive is the only way to make a read model passive.
- It is realization metadata, not behavior: toggling it changes neither the executable model bytes
  nor their revision, and the compiler reports it as information `PLAY0270`. A consumer that keys
  rendering on the revision alone misses the change; see `cratis-screenplay-render-and-gap-fill`.
- A renderer realizes it as a Chronicle passive projection (no sink, inactive; `[Passive]` in C#),
  so a command's decision read rebuilds one key from that key's events. It trades sink writes for a
  replay per command, which is why it is opt-in.
- Stage renders a passive read model only when it has no joins, no child or nested scopes and is
  keyed by the event source id; anything else is refused with `STAGE-ESM-061`. Compiler support
  alone does not mean your Stage release renders it: check the Stage release notes.
- It cannot name a sink (`PLAY0935`): see [sinks](sinks.md).

## Diagnostics

| Code | Severity | Cause | Fix |
| --- | --- | --- | --- |
| `PLAY0859` | Error | A `query` returns a passive read model (including `[]`, `optional` and `observable` forms), or a `screen` `data` line binds one | Remove `passive` if users read the model; otherwise point the query or screen at an active read model |
| `PLAY0860` | Warning | No command `reads` the passive read model | Add the intended command read, or remove `passive` |
| `PLAY0858` | Warning | The `passive` line is repeated | Keep one line |

Only the C# compiler reports `PLAY0859` and `PLAY0860`; the TypeScript compiler keeps the directive
and reports `PLAY0858` only. A clean TypeScript result does not prove that no query or screen
consumes a passive read model.

## Compiler support

Requires Screenplay 5.3.0 (`screenplay --version`). Older compilers do not know the directive: they
reject syntax JSON that carries `isPassive`, and a bare `passive` line is not a valid property
there. Remove the line, or re-export the model from a compiler that omits it, before downgrading.
