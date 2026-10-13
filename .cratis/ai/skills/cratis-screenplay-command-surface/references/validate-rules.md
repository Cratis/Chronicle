<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/validate-rules.md -->
# Validation rule forms

## Rule vocabulary

| Rule | Example |
| --- | --- |
| `not empty` | `name not empty` |
| `max <n>` / `min <n>` | `reason max 500` (length on text), `quantity min 1` (value on numbers) |
| `> <v>` / `>= <v>` / `< <v>` / `<= <v>` | `quantity > 0`, `discountPct <= 100` |
| `== <v>` / `!= <v>` | `currency == "NOK"`, `status != draft` |
| `length == <n>` | `currency length == 3` |
| `matches email` | the only named pattern; any other name is `PLAY0366` |
| `matches "<regex>"` | ECMAScript; matches **any substring** unless anchored with `^…$`; an invalid pattern is `PLAY0367` |
| `all > <v>` / `all >= <v>` | `lines.quantity all > 0` |
| `rule <Name>` | `orgNumber rule BeAValidOrganizationNumber` |

## Rules whose logic is code

**Rules whose logic is code.** A bare `rule <Name>` records that a rule exists but
has no portable meaning (`PLAY0268`). Give it a body when the logic can live in the
model — a `file` or a tagged ` ```csharp ` fence indented under the rule, or a
fenced `validate` block for cross-field rules. Excerpt, inside a command:

```screenplay excerpt
validate
  orgNumber rule BeAValidOrganizationNumber message "Must be a valid organization number"
    file Validations/BeAValidOrganizationNumber.cs
```

A bodied rule or fenced block on a validation, rule or policy binds as opaque code
(ESM v3): the reference runner reports it unsupported, and a target must supply the
implementation. Two exceptions never bind: the command `handler`, and a `file`
constraint (the binder records the requirement, then rejects it with `PLAY0268`;
only `unique` constraints bind).
Stage 4.73.1 renders pure reducer transitions and opaque policy bodies, but refuses command and
concept code validations and rule predicates (STAGE-ESM-005): those are gap-fill code. Prefer a
declarative rule when one can say it. What binds and what the executable model rejects (Date and
DateTime comparisons, nested-path rules): `cratis-screenplay-toolchain` `references/executable-subset.md`.
