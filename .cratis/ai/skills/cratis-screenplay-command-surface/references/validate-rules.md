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

## `not empty` means non-blank text

On text, `not empty` rejects null, empty text and whitespace-only text; at least
one non-whitespace character is required. Concept and command property rules
have the same meaning in **every ESM version**.

Requires Screenplay 4.151.0 or later (`screenplay --version`) for the Unicode `not empty` correction.
The rule does not trim or normalize the value: `" a "` passes unchanged.
Whitespace is Unicode `White_Space` (.NET `char.IsWhiteSpace`): U+0009–U+000D,
U+0020, U+0085, U+00A0, U+1680, U+2000–U+200A, U+2028, U+2029, U+202F,
U+205F and U+3000. U+0085 and U+00A0 fail; a lone U+FEFF passes. JavaScript
`trim()` is not equivalent (it misses U+0085 and removes U+FEFF).

On a collection, `not empty` still means **at least one element**, regardless
of that element's value: a collection containing empty or whitespace text is
nonempty. Identity text is separate and unchanged: event-source identities and
stream-id parts treat whitespace as a value, and only exact empty text is empty.
Do not apply this validation correction as identity normalization.

Add a whitespace-only rule rejection with the exact message; existing reference
specifications that expected acceptance now fail, even on an older ESM version.
The release changes reference outcomes, not canonical bytes or the ESM version.
A standalone pass is not proof that a bundled compiler or renderer has adopted
this correction. Complete exercised model:
[whitespace rule specifications](../../cratis-screenplay-specifications/references/whitespace-rules.md).

Sources: Cratis/Screenplay
`Source/DotNET/Screenplay/Semantics/Execution/SemanticEvaluator.cs`,
`Documentation/screenplay/commands.md`, `concepts.md` in the same
folder.

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
Stage renders pure reducer transitions and opaque policy bodies, but refuses command and
concept code validations and rule predicates (STAGE-ESM-005): those are gap-fill code. Prefer a
declarative rule when one can say it. What binds and what the executable model rejects (Date and
DateTime comparisons, nested-path rules): `cratis-screenplay-toolchain` `references/executable-subset.md`.
