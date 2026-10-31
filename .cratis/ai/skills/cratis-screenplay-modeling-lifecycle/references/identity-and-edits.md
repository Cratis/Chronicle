<!-- cratis-ai-managed: skills/cratis-screenplay-modeling-lifecycle/references/identity-and-edits.md -->
# Identity ownership and the one edit strategy (single source)

Every other skill and reference points here instead of restating it.

## One edit strategy
1. **Discover** the capabilities available in this session once (MCP reachable, tool versions).
2. **Prefer typed, identity-preserving operations** (MCP proposals: review the diff, then apply).
3. **Bounded text edits** are fine only for edits that leave the identity catalog's addresses
   unchanged (see *Classify by catalog address* below). Without `.screenplay/identities.json` (a
   fresh, never-applied model) plain text edits are fine for everything except persisted names.
   Validate per coherent batch.
4. **Never use a text edit to get around an MCP refusal.** A refusal means look, then ask or
   return an edit request.
5. Let tools resolve what they can (revisions, identities, renames); never compute them by hand.

## Who is the identity owner
The main or orchestrating session when one exists. A top-level or unattended agent, or one
whose brief explicitly asks for the rename or move, **is** the owner for that change. Ownership
is separate from authoring authority: an authoring agent without ownership still makes the
address-preserving edits its brief asks for and returns edit requests only for identity-affecting
changes. The Screenplay Reviewer and Renderer never edit `.play`; they always return edit requests.

## Classify by catalog address
Classify an edit by whether it changes the **catalog addresses**, not by artifact category.
`SemanticAddress` assigns identities to modules, features, slices,
concepts, composite types, properties, commands, event contracts, read models, projections,
queries, query arguments, specifications, triggers, reactions and captures;
`ScreenplayWorkspace` materializes new assignments when it is created from source; and
`McpState.Open` refuses a persisted workspace whose recompiled catalog differs from the stored
one (`IdentityReconciliationRequired`). So a text edit that adds or removes an addressed element
can make a persisted workspace impossible to reopen.

**With `.screenplay/identities.json` present**, go through MCP (the owning session) for every
addition, removal or rename of an addressed element: declarations (new slice, command, event,
read model, projection, ...), properties of commands, events, read models, composite types and
triggers, queries, query arguments and specifications; also moving declarations between files and
contract evolution of a persisted event (new generation, property added, removed or retyped).

See "Document mappings count too" in SKILL.md.

**Bounded text edits** are for edits that preserve every address: descriptions, the bodies of
rules and expressions, mappings between members that already exist, and other changes that
add, remove or rename no addressed element. Anything doubtful goes through MCP.

**Without `identities.json`** the model has no persisted catalog: plain text edits are fine;
persisted event names still need `id` pins (below) and ask when unsure whether events are stored.

## What the owner does
0. **A request or brief that names a rename, move or removal IS the approval** (see
   `rules/capability-is-not-authority.md`). Do not ask for per-apply or repeated approval, also
   when unattended. Unnamed identity effects, or a wider scope than named, still need a question.
1. Use the MCP rename proposal where available (it keeps `id` pins and identities, and refuses on ambiguity).
2. Otherwise a text rename. Without `.screenplay/identities.json`: keep an existing effective `id`
   pin, add `id "<OldName>"` to every renamed persisted event only when absent, drop it when
   returning to the pinned name. **With `identities.json`, `id` pins alone are not enough**: the
   identity-preserving catalog transition must go through MCP (the request already approves it).
3. One identity note per renamed or moved element in the report (old to new, pin, impact).
4. Renaming a constraint resets its uniqueness index (history does not carry over): ask first
   unless the request names that constraint rename; then carry it out and report the reset.
5. Approval already given for the named effect and target stands; ask again only when the target
   or consequence expands.
6. The owner returns an edit request **only** when it cannot apply a requested identity change
   (an identities file exists and no MCP is available). The report's first line then says the
   requested change is NOT done. Never leave a requested change silently unapplied.

A subagent without MCP returns the catalog-changing request (any addition, removal or rename of
an addressed element, not only renames) to the owning session even when the domain change itself
is authorized.

## Edit requests
The normal path for any non-owner that finds a model defect, identity-affecting or not. Template:
`handoff-template.md` section 3. Proposal ids are connection-local and vanish on reopen or apply;
never pass them. The owner re-proposes on its own connection, shows the diff, checks existing
existing approval (a request that named the change is that approval), then applies.

## Open question
How the first `.screenplay/identities.json` is created for a model that was only ever text-edited
is not documented beyond "after the first apply". Ask before relying on a first apply to
initialise identities, use `id` pins for persisted events, and record it in STATE.md when it applies.

## Document mappings

**Document mappings count too.** With `identities.json` present, creating, deleting, renaming or
moving a mapped `.play` file changes no catalog address but still breaks reopening:
`McpState.Open` throws `IdentityMappingConflict` ("mapped .play files are missing,
renamed, or accompanied by unmapped files ... use an MCP proposal to move/add/delete documents
explicitly"). Do these through MCP document operations (the `screenplay` server's `propose-ast` tool), never a file move or rename
in the shell or editor.
