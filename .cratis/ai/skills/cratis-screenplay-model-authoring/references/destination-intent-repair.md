<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/destination-intent-repair.md -->
# Establishing production destinations (PLAY0470)

## Contents

- Detect and discover
- Request
- Rules
- Review and apply
- Refusals and failures

Requires Screenplay 4.216.0 (`screenplay --version`); older servers offer no PLAY0470 repair, so
the author edits each `for` by hand.

`PLAY0470` is an error when a command targets another event source and a production omits `for`,
or when omitted inline and plain productions have different defaults. The repair needs the author's
explicit choice of every destination; the server never infers one. It is never in the no-input
`actions` list of `repair-capabilities`.

## Detect and discover

1. Feature-detect an entry in `repair-capabilities.intentActions` with `diagnosticCode: "PLAY0470"`.
   It names the tool (`propose-repair`), the intent schema (`destinationIntent`, version 1), the
   discovery view (`repair-intents`) and the report view (`production-destinations`). Absent entry:
   the server cannot repair `PLAY0470`; edit through a reviewed `propose-ast` instead.
2. `read-workspace` with `view: "repair-intents"` and `expectedRevision`. The response carries
   `expressionSchema` (`syntax-schema:ExpressionSyntax`) and a `page`. Each item has `command`,
   `diagnostics`, `subject` (the production occurrence handle), `event`, `condition`, `existingFor`,
   `authoredRoute`, `effectiveRoute` and `route` (`null` when ambiguous).
3. Page with `offset` (default 0) and `limit` (1 to 200, default 50) until `nextOffset` is `null`.
   Pass `expectedCatalogRevision` on continuation pages. A row too large for a page refuses with
   `LimitExceeded` and is never truncated.

## Request

Handles are `{revision, documentId, path}` objects: reuse discovered ones exactly. Every value in
angle brackets below is a placeholder, not literal JSON.

```json
{
  "expectedRevision": "<workspace revision>",
  "expectedCatalogRevision": "<catalog revision>",
  "diagnosticCode": "PLAY0470",
  "subject": "<command handle>",
  "formatting": "CanonicalizeTouchedDocuments",
  "destinationIntent": {
    "version": 1,
    "productions": [
      {
        "subject": "<production occurrence handle>",
        "for": "<typed expression object from syntax-schema>",
        "route": { "kind": "Legacy" }
      }
    ]
  }
}
```

A named route is `{ "kind": "Named", "source": <declaration handle>, "stream": <declaration handle> }`;
both handles are required, and `Legacy` takes neither.

## Rules

- Give exactly one selection per event-production occurrence, including repeated events, conditional
  productions and productions that already state `for`. An incomplete, duplicate, stale or foreign
  selection refuses the whole request.
- `for` is a typed expression object obtained from `syntax-schema`; never invent its shape and never
  guess an identifier. Choose each destination with the author.
- Confirm the route per selection as `Legacy` or `Named` with the exact source and stream handles.
- `formatting: "CanonicalizeTouchedDocuments"` is required. The server forces `Executable`
  validation, so send no `validation` argument. Send no `pinRepairEvidence` and no
  `expectedRepairEvidenceRevision`.
- Every missing `for` changes together in one transaction. A repair never retargets an explicit
  destination or route; already-explicit selections must match what is authored.
- The whole workspace must bind. There is no fix-all, no inferred destination and no applyable subset.
- The repair claims neither preservation nor an executable baseline, including for other commands.
  Binding proves neither business correctness nor target execution support.

## Review and apply

`propose-repair` retains a proposal and writes nothing. Both report views page like discovery
(`offset`, `limit` 1 to 200); read each until `page.nextOffset` is `null`, because a first page can
hide routing rows or new identities. Before `apply`:

1. `read-proposal` with `proposalId` and `view: "production-destinations"`: the `summary`
   (`changedCount`, `productionCount`, `commandRoute`, `existingIdentitiesPreserved`) and one row per
   production with `original`, `selectedFor`, `routeConfirmation`, `omission`, `classification`
   (`MadeExplicit` or `ExplicitUnchanged`) and `effectiveClassification`.
2. `view: "production-destination-identities"`: newly enrolled identity strings; existing identities
   must survive unchanged.
3. The `diagnostics` and `executable-diagnostics` views and the exact `before` and `after` bytes.
4. Show the author the review, then call `apply` with `proposalId`, `expectedRevision` and
   `expectedCatalogRevision`. Apply only the proposal that was reviewed.

Both report views refuse on a proposal that has no production destination report.

## Refusals and failures

- Refusals include an incomplete, duplicate, stale or foreign selection, an ambiguous or mismatched
  route, an attempt to retarget an explicit destination, an unsupported expression, incomplete
  imports or source, comment or identity loss, an unrelated executable blocker and missing
  formatting consent.
- Failure kinds: `StaleRevision` (rediscover), `FormattingConsentRequired`, `ProposalRejected`
  (read the conflicts and diagnostics), `RepairEvidenceWriteConflict` and `RepairEvidenceDrift`
  (obtain and review a fresh proposal).
- `RolledBack` is a failure with verified restoration. `RecoveryRequired` and `PendingOperation`
  mean a journal needs attention: inspect `workspace-state` and make recovery its own explicit
  request. Never retry automatically after an unknown outcome.
