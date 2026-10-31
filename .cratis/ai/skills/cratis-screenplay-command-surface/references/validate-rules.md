<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/validate-rules.md -->
# Validation rule forms

## Contents

- Rule vocabulary
- `require` message
- Date and DateTime comparisons
- `not empty` means non-blank text
- Rules whose logic is code

## Rule vocabulary

| Rule | Example |
| --- | --- |
| `not empty` | `name not empty` |
| `max <n>` / `min <n>` | `reason max 500` (length on text), `quantity min 1` (value on numbers) |
| `> <v>` / `>= <v>` / `< <v>` / `<= <v>` | `quantity > 0`, `discountPct <= 100` |
| `== <v>` / `!= <v>` | `currency == "NOK"`, `status != draft` |
| `length == <n>` | `currency length == 3` |
| `matches email` | the only named pattern; any other name is `PLAY0366` |
| `matches "<regex>"` | Portable linear dialect; any substring unless anchored `^…$`; invalid syntax PLAY0367, refused constructs PLAY0699 |
| `contains "<text>"` / `not contains "<text>"` | Ordinal substring presence/absence on scalar non-enum text |
| `starts with "<text>"` / `ends with "<text>"` | Ordinal prefix/suffix; quoted literal operands, not conditions |
| `decimals <= <n>` | `price decimals <= 2`; a `Decimal` property or concept only, `<n>` a whole number 0 to 28, trailing zeros do not count; ESM v29 (decision 0098) |
| `all > <v>` / `all >= <v>` | `lines.quantity all > 0` |
| `rule <Name>` | `orgNumber rule BeAValidOrganizationNumber` |
| `checksum luhn|mod11|mod97` | String concept `validate checksum luhn`; validates supplied/stored values too |

Requires Screenplay 4.172.0 or later (`screenplay --version`) for checksum rules
(ESM v21); older compilers refuse them with PLAY0268. A pattern's `checksum`
modifier generates check digits but does not add validation. Check digits catch
typos, not forgery. For generated-concept proof statuses, deterministic regex
limits and PLAY0688 repairs, read [generated rules](generated-values-and-responses.md#generated-concept-rules-esm-v20).

## `require` message

The `message` of a whole-command `require` goes on the next, indented line, not on the `require` line:

```screenplay excerpt
validate
  require to >= from
    message "The end must not be before the start"
```

## Date and DateTime comparisons

Requires Screenplay 4.199.0 (ESM v26; `screenplay --version`); older binders refuse
with PLAY0268. Command/property rules admit `==`, `!=`, `>`, `>=`, `<`, `<=` against
a same-primitive date literal only, for example `to >= "2026-01-01T00:00:00Z"` on
DateTime. Property-to-property comparisons instead use `require to >= from`.
Date rules on concepts remain refused; an absent optional value passes rules
except `not empty`. Read [date comparisons](date-comparisons.md) for UTC ticks,
input offsets, refused positions and the complete retention-window specifications.

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
implementation. Attached command handlers refuse PLAY0800 and require constrained
implementation attachments; `file` constraints retain PLAY0268 (only `unique`
constraints bind). Requires Screenplay 4.210.0 (`screenplay --version`) for the
handler's PLAY0800; older binders use PLAY0268. Pending command handlers beside unconditional
`produces` instead bind as described commands; see [described handlers](described-handlers.md).
Literal text rules, portable patterns and Studio desugaring: [text and match rules](text-and-match-rules.md).
Stage renders pure reducer transitions and opaque policy bodies, but refuses command and
concept code validations and rule predicates (STAGE-ESM-005): those are gap-fill code. Prefer a
declarative rule when one can say it. What binds and what the executable model rejects
(date comparisons outside command positions, nested-path rules): `cratis-screenplay-toolchain` `references/executable-subset.md`.
