<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/enum-numbers.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Explicit enum member numbers

Authoring shipped in Screenplay 4.137.0; non-positional executable numbers shipped
in 4.139.0 as ESM v12 (decision 0065). The model owns member identity, not the renderer.

```screenplay
domain EnumNumbers

concept InvoiceId : Uuid
concept InvoiceStatus : Enum
  draft = 1
  sent = 4
  cancelled = -1

module Invoicing
  feature Issuing
    slice StateChange SendingInvoice
      command SendInvoice
        invoiceId InvoiceId identifier
        status InvoiceStatus
        produces event InvoiceSent
          status InvoiceStatus = status
      specification Sending
        when SendInvoice
          invoiceId = "6f9619ff-8b86-d011-b42d-00c04fc964ff"
          status = "sent"
        then InvoiceSent
          for "6f9619ff-8b86-d011-b42d-00c04fc964ff"
          status = "sent"
```

## Source rules

- Number **all members or none**; there is no implicit continuation after a pin.
- Numbers are unique whole int32 values, from -2147483648 through 2147483647.
  Gaps and negative numbers are allowed; aliases and flags semantics are not.
- Write decimal digits with an optional minus sign: no leading zeros, fraction,
  exponent or plus sign. `-0` prints canonically as `0`.
- Escape a keyword member as usual, for example `@validate = 7`.
- Unnumbered enums retain zero-based positional meaning. Descriptions and
  validation blocks still work. Specifications, fixtures, validation operands
  and conditions use **member names**, never their numbers.

PLAY0667 reports mixed numbering, PLAY0668 duplicate numbers, PLAY0669 an
out-of-int32 value and PLAY0670 malformed number syntax. None has an automatic
repair: correcting a stored identity needs an author decision.

## When to pin

Pin enums used in persisted events or a stable public contract, or when retaining
numbers extracted from an existing system. Before reordering, inserting or removing
members, establish the **stored** numbers first; numbering an already reordered list
would preserve the wrong meaning. Never renumber a published member or reuse its
number for another meaning. Pinning does not prove old readers accept a removed member.

A short-lived enum used only by command inputs or views may remain positional.
Ordinary compilation does not demand pins. Opt into `--check enum-pinning`
(or `PLAY0671` or `all`) for an Information hint on wholly unnumbered enums used
by event properties, including through composite types. MCP `diagnostics` uses
`checks: "enum-pinning"`. The hint suggests `propose-number-enum-members`, which
pins current positions; it is a refactoring, not a correctness repair. Read
model-authoring `references/mcp-tools.md` for the revision-checked proposal loop.

## Executable contract and consumer boundary

ESM v12 adds an optional canonical **last** `numbers` array parallel to `values`.
For the concept above, `values` is `["draft", "sent", "cancelled"]` and `numbers`
is `[1, 4, -1]`. Non-positional numbers select v12. Pins equal to `0..n-1` are
retained in source/AST but omitted from ESM, preserving the previous executable
version, bytes and revision; unnumbered enums also preserve their old bytes.

Strict reading rejects `numbers` in older versions, without enum values, with a
length mismatch, duplicates, non-int32 entries or a positional array (non-canonical).
Programmatic validation enforces the same contract. Specifications still compare
names; numbers are neither alternative fixture values nor flags.

Consumers must **explicitly admit v12**. CLI 3.57.0 (Screenplay 4.147.1 / Stage
4.89.1) admits through v15 and renders explicit numbers as `Member = N` (sparse
and negative, `-0` as 0; unnumbered enums keep declaration order). Stage first
admitted v12 in 4.76.0. Do not remove enum pins to fit an older consumer;
report its admission gap. See toolchain `references/versions.md`.

Sources checked at `v4.139.0`: `Documentation/screenplay/concepts.md`,
`interoperability.md`, `diagnostics.md`, `mcp/authoring-tools.md`, decision 0065,
`Semantics/Versions.cs`, `SemanticModelValidator.EnumMemberNumbers.cs` and the
source-backed `EnumMemberNumbersCorpus.V12` (under `Source/DotNET/`). Consumer
pins: CLI `v3.57.0:Directory.Packages.props:8-10` and
`Source/Cli/Commands/Render/RenderedSemanticVersions.cs:34`; Stage `v4.89.1`,
`Source/Rendering.Cratis/Semantics/SemanticVersionFeatures.cs:100`,
`SemanticEnumMembers.cs:15-26`, `SemanticCommonArtifactRenderer.cs:56-65`.
