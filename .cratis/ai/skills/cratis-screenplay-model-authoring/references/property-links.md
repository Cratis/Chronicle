<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/property-links.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Inspect a property's chain before changing it

## Contents

- Address and query
- Read evidence, not just a count
- Propose a property change (4.142.0)
- Add with explicit sources and fixtures

Screenplay 4.138.0 released property-path evidence in `find-references` and
`dependencies`; this guidance was checked against 4.139.0. Discover installed
`tools/list`: CLI 3.53.1's 4.137.0 bundle predates it. These are read-only
inspection tools. Standalone 4.140.0 adds the separate `propose-property` rename/remove planner;
4.142.0 adds `add`. The CLI bundle has neither capability.

## Address and query

Use `kind: "Property"`, case-sensitive. Append the member to its owner's exact
logical address: `Owner.member`. For an event this selects the current generation;
`Owner.generationN.member` selects a historical shape, whose target details carry
`historical: true`. Properties are not returned in declaration listings: locate
the owner and inspect its properties first.

On the command-surface [enum example](../../cratis-screenplay-command-surface/references/enum-numbers.md),
first inspect the event with `find-references`:

```json
{
  "address": "Invoicing.Issuing.SendingInvoice.InvoiceSent",
  "kind": "Event"
}
```

At 4.139.0 this returns two references (`produces`, `thenEvent`), with top-level
`totalCount: 2`, `references` and empty `ambiguous`, `incomplete`, `wrongKind`
arrays. The event response has no `complete` field. Query its property separately:

```json
{
  "address": "Invoicing.Issuing.SendingInvoice.InvoiceSent.status",
  "kind": "Property"
}
```

Then query direct mapping counterparts with `dependencies`:

```json
{
  "address": "Invoicing.Issuing.SendingInvoice.InvoiceSent.status",
  "kind": "Property",
  "direction": "outgoing"
}
```

Both tools accept `offset`, `limit` and `expectedSourceRevision`; echo the first
response's `sourceRevision` on continuation pages. `dependencies` additionally
accepts `direction: "incoming"` (default), `descendants` and `document`. It follows
**direct mapping counterparts only**, not a transitive property chain or runtime
impact. Query each resolved counterpart separately when walking a longer chain.

## Read evidence, not just a count

`find-references` returns top-level `references`, `ambiguous`, `incomplete`,
`complete`, `totalCount`, `offset`, `nextOffset` and `coverage`. Ambiguous/incomplete
rows contain `reference` and `candidates`. `dependencies` instead has
`page.items`, each carrying `reference`, `resolution` and `targets`, with paging
inside `page`; `complete` and `coverage` stay at the top level.

Each property reference retains `owner`, `location`, `role`, `reasons` and
`otherEnd` (the link's resolved other end, or null). For outgoing dependencies,
navigate `targets`, not an assumed interpretation of `otherEnd`. Evidence roles are:

| Role | Meaning |
| --- | --- |
| `mapping-source`, `mapping-target` | Explicit production, capture append or projection mapping end |
| `automap` | Derived case-insensitive projection link |
| `condition` | Declarative condition, including query filters and policy readers |
| `key` | Lookup/read, projection/join/removal/parent, constraint or stream-id key |
| `screen-binding` | Screen/form field use |
| `fixture`, `assertion` | Specification/example assignment or expected value |
| `response` | Command response property source |
| `opaque` | Text names the property; code is not resolved |
| `unclassified` | Conservative backstop found a name occurrence outside classified links |

### Observed result on the compiled example (4.139.0)

The property `find-references` response has `success: true`, `complete: false`,
`totalCount: 3`, `nextOffset: null`: two resolved references (`mapping-target`
on the command's `status = status`, and `assertion` on `then InvoiceSent`),
and one `incomplete` entry. Its `reference.role` is `unclassified`, located at the
specification's command-input `status` assignment, with `reference.reasons`:

```json
["Property name occurs in referencing declaration text without a classified link."]
```

Outgoing `dependencies` also reports `success: true`, `complete: false` and
`page.totalCount: 1`. Its resolved `page.items[0]` has a `mapping-source`
reference; `targets[0].address` is
`Invoicing.Issuing.SendingInvoice.SendInvoice.status`, while the reference's
`otherEnd.address` is the queried event property. The property's completeness
covers all indexed evidence even when the chosen direction's page has only a
resolved link. The workspace had `sourceSuccess: true`, no source diagnostics
and `readiness.executableReady: true`: compilation success does not imply
complete property evidence.

**`complete: false` is never a successful empty list.** Inspect every ambiguous
or incomplete row and its `reasons`/candidates, even when resolved `references`
is empty. `success` describes compilation, not completeness. Opaque occurrences,
unresolved counterparts, nested composite paths, legacy command reads and external
source items can leave incomplete evidence. The backstop counts whole-word names
case-insensitively in relevant typed source and comments, including quoted text;
same-named properties in unrelated declarations do not affect completeness.
External code contents and registrations remain outside coverage even with
`complete: true`; authored links do not prove executable admission.

Before a property change: pin the revision, inspect incoming readers and outgoing
counterparts, page to completion, resolve uncertainties, then choose the supported
identity-safe edit route with the connection owner. Never infer safe deletion from
zero resolved hits or edit every same-named field. An event-property change still
needs a persisted-contract evolution decision; the catalog is not storage evidence.

Source/output authority: `v4.139.0:Documentation/screenplay/mcp/reference.md`
(property links), `Source/DotNET/Screenplay.Mcp/McpPropertyDependencies.cs`,
`McpToolSchemas.cs`, and decision 0044's released prerequisite index. The planner's release evidence is separate below.

## Propose a property change (4.142.0)

Use `propose-property`, not hand-composed `propose-ast` batches, for supported
add/rename/remove changes along a proven chain. For add, inspect existing
producer/consumer properties and mapping blocks first; the new property has no
reference address yet.

Read the target property handle with `read-ast`, then call `propose-property`
with `expectedRevision`, `expectedCatalogRevision`, `action: "rename"|"remove"`,
`target` and `expectedName`. Rename also needs `newName`. Optional `alsoRename`
or `alsoRemove` entries are `{ "target": <handle>, "expectedName": <name> }`.
Select only intended command, current-event and non-key read-model properties:
chain membership establishes eligibility, not permission to edit every field.
Off-chain selections refuse atomically (`HopNotOnChain`). Historical generations, inline events,
keys, opaque or incomplete links and unsupported spans refuse rather than guess.

Rename repairs proven references and fixtures. Renaming only a command input
keeps the event payload name; broken AutoMap links become explicit mappings.
Removal refuses while readers remain. Removed expected values disclose
`weakensAssertion: true`; when inherited `then` steps cannot be determined, removal
refuses `AmbiguousPropertyReference`, not a false assertion-preservation verdict.
Unrelated same-named fields appear in `property.notChanged`.

Read `property.needsDecision` for refusal reasons and located candidates;
`property.chainCandidates` lists unselected chain members, not automatic edits.
A refusal returns `success: false` and no writable partial candidate. Missing an
event mode returns `EventContractChangeRequiresDecision`; do not bypass it by
text editing or claiming the catalog proves non-persistence.

For any selected event property (including optional additions), choose exactly one mode: `newGeneration: true`
appends N+1 and preserves history; `eventNeverPersisted: true` edits current N in
place and requires actual knowledge that it was never stored. Neither flag selects
additional events. New generations require transformations before Chronicle deployment.

Default validation is `Authoring` (optional `Executable`), formatting
`PreserveTrivia`, reference policy always `Safe`. Structural changes require
explicit `CanonicalizeTouchedDocuments` consent. Inspect retained `read-proposal`
and `view: "semantic-diff"` before `apply`; only apply writes source and identities.
Review `property.generations`, always an array ordered by canonical event address,
with one entry per selected event: `event`, `from`, `to`, `mode`. In-place edits
have equal revisions. `[]` means no accepted planned event edits, not success;
there is no singular `property.generation`. `deployableWithoutTransformation`
and `esmVersionKnown` are independent of authoring acceptance.

## Add with explicit sources and fixtures

Read the declaration's original `read-ast` handle. Send `action: "add"`, both
revision guards, `owner`, `expectedOwnerName`, `name` (new unqualified member),
`type` (authored type text, not a concept declaration), and optional `optional`
(default false). Use the flag, not an optional suffix in `type`.

`hops` defaults to `[]`: each entry has an original `at` handle and exactly one
of `name` or `value`. `{at, name}` adds the same type at a connected declaration;
`{at, value}` supplies a value expression at a `produces`, `from` or capture
`append` block. Every selected event producer/read-model consumer needs an
explicit mapping decision or compatible same-named AutoMap source. Never infer
a business expression. Existing property names and off-chain hops refuse.
Add does not accept `target`, `expectedName`, `newName`, `alsoRename` or `alsoRemove`;
add-only fields are invalid for rename/remove.

For a required addition with existing fixtures/assertions, supply `fixtureValue`
as **literal source text**, for example `"\"normal\""` for a String value. The
planner adds it to existing specification steps, examples and query results;
`optional: true` adds no fixture values. A literal is not proof of behavior:
inspect the assertions and, on Screenplay **4.145.0 and later**, run
`run-specifications` with `proposalId` on the retained candidate before `apply`.
See [reference execution](mcp-tools.md#run-reference-specifications).

| Refusal | Response |
| --- | --- |
| `SourcesRequired` | Select missing producer/consumer mappings from the located candidates; do not guess sources |
| `FixtureValueRequired` | Supply the intended required literal fixture value; do not weaken assertions |
| `HopNotOnChain` | Remove or correct the off-chain selection; never edit same-named unrelated fields |
| `UnsupportedPropertySpan` | Preserve source and report unsupported nested feeds/expressions or shared spans; do not flatten the model to bypass refusal |

Use `CanonicalizeTouchedDocuments` for additions. Review `property.changed`
(`added`, `fixtureAdded`), decisions, chain candidates and generations, exact
bytes, dropped comments, identities and semantic diff before ordinary `apply`.
Even an optional event addition needs the same explicit evolution mode above.

Authority: Screenplay `v4.142.0:Source/DotNET/Screenplay.Mcp/McpWorkspaces.Property.cs`,
`Screenplay/Workspaces/WorkspaceProperty{Request,Plan,Safety,Report}.cs`,
`WorkspacePropertyAdd{Plan,Edits}.cs`, `WorkspacePropertyAssertionProvenance.cs`,
`WorkspaceEventGenerations.cs`, and `Documentation/screenplay/mcp/{edit,reference,authoring-tools}.md`;
the published contract's `mcpTools` entry for `propose-property`; decisions 0067 and 0044.
