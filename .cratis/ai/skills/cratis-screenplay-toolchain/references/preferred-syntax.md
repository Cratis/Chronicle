<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/preferred-syntax.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Preferred Screenplay syntax

Use these forms when writing or reviewing `.play`. Acceptance of an older
spelling is not a recommendation to teach it. Information diagnostics do not
fail `--warnaserror`; the corpus verification gate rejects the nine style codes
below even when a fence declares `expect`, unless an exact, reasoned migration
exception exists in the verifier's preferred-syntax allowlist. That allowlist also
covers deliberate legacy explanations in prose; stale entries fail.

## Forms

| Construct | Preferred form | Accepted older form or redundancy | Diagnostic |
| --- | --- | --- | --- |
| Optional type | `T optional`, `T[] optional` | `T?`, `T[]?` | PLAY0479 (information) |
| Compliance | Header `concept X : String pii` or `secret`; body settings `pii reason`, `pii special health`, `secret scope namespace` | `@pii`, `@sensitive`, `sensitive`; `personal` is an accepted alias without a diagnostic | PLAY0565 (information); repeated markers warn PLAY0653 |
| Inline code | A construct header such as `validate`, followed by an indented language-tagged fence; descriptions use a tagged `text` fence, event documentation `markdown` | `validate csharp`, a language line above a bare fence, or a bare description fence | PLAY0397 (warning) |
| Event identity | Omit `id` for a new event; preserve `id "OldName"` for a renamed stored contract | `id` equal to the current event name | PLAY0471 (information) |
| Source/stream identity | Omit `id` unless preserving a previous stored name | A pin equal to the current name | PLAY0507 (information) |
| Production route | Inherit the command's route | Repeating the same `stream` / `streamId` on a production | PLAY0664 (warning) |
| Imports | Import only declarations not already declared by the application | Importing the application's own names | PLAY0290 (warning) |
| Interaction guard | Block-form `when` alternatives | Opaque interaction `where` on click, double click or select | PLAY0564 (warning or information) |
| Projection transition cardinality | Source binding emits `One` | Constructed/deserialized ESM v1–v3 `ZeroOrOne` / `Many` | PLAY0445 (warning; ESM-only, not a source spelling) |

`query Q => observable?` is not a legacy optional type suffix and has no keyword
replacement. Reaction/Translate `where` is current syntax, not an interaction
legacy guard. Keep a real rename pin; do not remove it to resemble a new event.

## Command productions

**Cratis convention:** prefer inline `produces event <Name>` when one command
owns the event. Declare payload types and mappings together; omit `for` to use
the command identifier and never copy that identifier into the event payload.
For an event shared by commands or reactions, keep a standalone `event` and use
plain `produces <Name>` with explicit `for <identifier>` on commands. Reactions
cannot declare inline events; omission there follows the triggering event's
source, and clock/application triggers require a destination.

Plain command production omission is accepted *legacy omission behavior*, not
identifier inference: it can use allocation or a sibling's resolved destination.
PLAY0478 is advice, not a forbidden style code or permission to retarget a stored
contract automatically. PLAY0469 concerns payload design and contract impact;
it is not a syntax-style ban either. Neither are PLAY0516, PLAY0553 or PLAY0520.
Read command-surface [inline events and destinations](../../cratis-screenplay-command-surface/references/inline-events.md)
for mixed destinations, extraction and generation evolution.

**Stage caveat:** rendered Arc commands ignore production `for` (Stage#187).
An authored destination is not proof of rendered routing; verify the target
implementation separately.

## Navigation `by` and form `columns`

Both are current, not legacy. Screen
navigation passes the target's key with `by <parameter>` (`on row-click navigate
to InvoiceDetail by invoiceId`) and further values with `parameter <name> from
<binding>`; navigation inside an interaction block passes values with `with <name>
from <binding>`. A form's `columns auto|manual` chooses which command properties
it shows and in which order; `layout`/`place` positions them. Use whichever the
case needs; neither has a style diagnostic.

## Verified product sources

Screenplay documentation pages: `types.md`, `concepts.md`, `ast-compatibility.md`, `commands.md`,
`events.md`, `event-sources.md`, `reactions.md`, `interactions.md`, `folders.md`, `glossary.md`,
`forms.md` and `diagnostics.md`. Printer: the Screenplay printer's syntax text and its expression and
command printing.
The published contract is the selector's authority, not this hand-written table.
