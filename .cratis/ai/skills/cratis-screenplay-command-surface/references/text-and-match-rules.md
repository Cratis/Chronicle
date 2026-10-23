<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/text-and-match-rules.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Literal text rules and portable matching

## Contents

- Literal text rules
- Portable `matches`
- Studio replacements and public check APIs

## Literal text rules

Requires Screenplay 4.184.0 or later (`screenplay --version`) for literal text rules and pattern admission checks; older compilers do not admit these rule forms.

Prefer a literal rule over a regex when the requirement is plain text. Complete example:

```screenplay
concept InvoiceReference : String
  validate
    contains "-"
    not contains " "
    starts with "INV-"
    ends with "-NOK"
```

These concept/command-property rules select ESM v24. They compare UTF-16 code units
ordinally, without case folding or normalization. Use one non-enum text value and a
quoted literal operand, not a property, collection or regex. An absent optional
value passes; add `not empty` to require presence. An empty operand makes `contains`,
`starts with` and `ends with` pass, and `not contains` fail for present text.
Equality and inequality remain `== "x"` and `!= "x"`.

This is not condition grammar: `contains`/`starts with` in `require`, `produces when`
or reaction `where` remain PLAY0268. Those conditions admit scalar equality (including Uuid from v28) and
numeric ordering, not the property text rules.

Requires Stage 4.124.0 for v24 text-rule admission (`cratis --version` identifies the CLI bundle); older renderers refuse v24 with STAGE-ESM-016.

Stage admits v24 and executes these four rules on its Host and specification
executor, rendering ordinal UTF-16 comparisons without regex or a Screenplay
runtime dependency. The legacy EventModel syntax engine drops these rules; use
semantic admission, never infer support from that engine's output.

## Portable `matches`

Requires Screenplay 4.183.0 or later (`screenplay --version`); older compilers use platform matching and may accept nonportable constructs.

A quoted pattern is a flagless JavaScript expression restricted to a portable linear
dialect, not arbitrary ECMAScript or .NET regex. It searches any substring unless
anchored. Screenplay uses its own Thompson automaton, without timeout or host-engine
settings. Syntax remains `matches "^INV-[0-9]{6}$"` or `matches email`.

| Admitted | Meaning |
| --- | --- |
| Literals, `.`, `^`, `$`, alternation | `$` is strict end of input, not before a final newline |
| `(…)`, `(?:…)`, `(?<name>…)` | ASCII names `[A-Za-z_][A-Za-z0-9_]*`; duplicate names only in disjoint alternatives; names that can match together report PLAY0367 |
| Classes, negation, single-character ranges | Unions/complements use set semantics |
| `\d \D \w \W \s \S` | `\d` is ASCII digits, `\w` ASCII letters/digits/underscore |
| `\t \n \r \f \v`, `\0` not before a digit, `\xHH`, `\uHHHH` | UTF-16 code-unit escapes |
| Escaped syntax characters and `\/` | `\-` also inside a class |
| `* + ? {n} {n,} {n,m}` and lazy forms | No stacked quantifiers |

`\s` includes U+00A0, U+FEFF and Unicode line/paragraph separators; it excludes
U+0085. Dot excludes LF, CR, U+2028 and U+2029. `\w` excludes U+0130.
### Write unambiguous patterns (PLAY0803)

Requires Screenplay 4.218.2 or later (`screenplay --version`); older compilers do not warn.

Generated clients run the pattern in a browser's backtracking engine, so an exponentially
ambiguous pattern can hang a tab on a crafted value even though Screenplay's own automaton
stays linear. The warning `PLAY0803` (`AmbiguousMatchPattern`) names the ambiguous
subexpression and a simpler rewrite where one exists; C#, TypeScript, Monaco and VS Code
report it identically. Very large patterns are not analyzed, so keep them small anyway.

| Ambiguous | Write instead |
| --- | --- |
| `(a+)+$`, `(a*)*` | `a+`, `a*` (never quantify a group whose body already repeats) |
| `(a\|a)*` | `a*` (alternatives must not match the same text) |

Prefer a character class such as `[ab]*` over alternatives that overlap. Fix the pattern rather than suppressing the warning; validate with `--warnings-as-errors`.

`matches email` is conservative, not RFC 5322: one `@`, nonempty local part without
whitespace/`@`, and at least two nonempty domain parts without whitespace/`@`.
A no-break space therefore fails email validation. Other bare names report PLAY0366.

PLAY0367 means invalid syntax, including stacked quantifiers. PLAY0699 means a
construct outside the dialect: lookarounds, backreferences, word boundaries,
Unicode properties, code-point escapes, control/octal/identity escapes, .NET-only
anchors/options/class subtraction, empty classes, stray brackets, class escapes as
range endpoints, or non-ASCII group names. Use explicit classes, UTF-16/hex escapes,
`^`/`$`, or a literal text rule where equivalent. A length-only lookahead can become
an anchored counted repeat; there is no general lookaround replacement.

Limits are 1,000 decoded UTF-16 code units and 3,000 expanded nodes. Literals,
classes, anchors and each optional/loop/alternative choice point count one. Counted
repeats multiply their body by the upper bound (unbounded: lower bound plus one),
adding optional/loop choices. `x{1500}` counts 1,500; `a{0,1500}` and `(a*){1500}`
count 3,000. Exceeding either limit reports PLAY0699 before automaton construction.

Older serialized ESM retains authored pattern bytes and loads, but invalid/refused
patterns plan as UnsupportedValidation and generation checks report Unproven;
re-authoring reports PLAY0367/PLAY0699.
Browser matching remains advisory: JavaScript may backtrack even on admitted
`(a+)+$`. The server-side linear check is authoritative.

## Studio replacements and public check APIs

Only exact escaped-literal Studio shapes desugar with warning PLAY0787, not PLAY0699:

| Decoded pattern (`L` is an escaped literal) | Replacement |
| --- | --- |
| `^(?![\s\S]*L)` | `not contains "literal"` |
| `L(?![\s\S])` | `ends with "literal"` |
| `^L(?![\s\S])` | `== "literal"` |
| `^(?!L$)` or `^(?!L(?![\s\S]))` | `!= "literal"` |

Recognition requires a lossless escape/decode round trip: Studio v1 uses
.NET Regex.Escape; v2 escapes `\ ^ $ . | ? * + ( ) [ ] { } /`. Other lookarounds
remain refused. Plain `L`, `^L`, `^L$` and `L$` are unchanged. Syntax retains the
original text, while binding stores the ordinal rule. Old ESM evaluates these exact
patterns as replacements without changing bytes. The editor quick fix and MCP
`propose-repair` migrate to literal rules preserving message/severity; preview the
revision-bound canonical-formatting proposal before explicit apply.

C#: `Cratis.Screenplay.Semantics.PortableMatchPattern.Check(pattern)`.
TypeScript: import `checkPortableMatchPattern` from `@cratis/screenplay-compiler`.
Both return PortableMatchCheck: status, code, construct, UTF-16 offset in the decoded
pattern, suggestion, description and optional replacement `{ kind, value }`.
Statuses are Admitted, Deprecated, Refused, Invalid (lowercase strings in TypeScript).
Admitted has no code; Deprecated is PLAY0787, Refused PLAY0699, Invalid PLAY0367.
Replacement kinds are Equal, NotEqual, Contains, NotContains, StartsWith, EndsWith.
Checking never executes a pattern or constructs a platform regex.

Validate with `screenplay <root> --warnaserror`, then execute acceptance/refusal
examples through `screenplay test <root>`; source validation alone is not a rule test.
