<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/inline-events.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Inline events and destinations

Cratis prefers inline `produces event <Name>` for a generation-1 event owned by
one command. Keep a standalone `event` when several commands or reactions share
it, and use plain `produces <Name>` with explicit `for <identifier>` on commands.
Reactions cannot declare inline events. This is a corpus convention, not a
compiler requirement. Screenplay `Documentation/screenplay/commands.md`
documents the destination defaults and calls plain omission legacy omission behavior.

This complete example declares its payload and mappings together:

````screenplay
concept ProjectId : Uuid
concept ProjectName : String
module Projects
  feature Naming
    slice StateChange RenameProject
      command RenameProject
        projectId ProjectId identifier
        name ProjectName
        produces event ProjectRenamed
          description "A project received a new name"
          documentation
            ```markdown
            Existing links retain the project's identity.
            ```
          tag audit
          name ProjectName = name
````

The omitted `for` means the command's required scalar identifier **only for
inline productions**, when every production targets that same source. Once a
production targets another source, every production must state `for`. Mixing
omitted inline and plain destinations also fails (`PLAY0470`); there is no
verified MCP repair for that diagnostic. Explicit syntax does not make cross-source
execution supported.

Plain `produces X` references a declared event; omitting `for` does not infer
the command's identifier. In ESM v2+, it inherits a sibling production's resolved
destination through the command destination default. An allocated identity is
used only when no production resolves a destination. State `for` explicitly on
every production targeting the identifier. `PLAY0478` offers advice and a
reviewed repair, not permission to silently retarget an append. Supply an
allocated identity to the executable model when allocation is intentional.

Never copy the same-source command identifier into the inline payload: it travels
in event context. Extract an inline declaration when another producer needs it;
read-only consumers alone do not require extraction. Preserve a shipped contract
rather than mechanically removing an existing identity field.

**Stage caveat:** rendered Arc commands ignore production `for` (see Screenplay
`commands.md`). Verify target routing separately.

Keep conditional `produces when` events standalone: its next line must be a
plain event name, not `event <Name>` (Screenplay
`Source/DotNET/Screenplay/Parsing/ProducesParser.cs`). Also retain plain
production when a site-specific tag must not become an event-type tag.

Inline declarations are slice-owned contracts, usable by other consumers.
Their `tag` lines are event-type tags; plain production tags apply at that one
append site. Both inline and standalone events accept a quoted description or
text/Markdown description fence, and one nonempty fenced Markdown `documentation`.
New events omit `id`. Only a rename preserving an old stored name needs
`id "<old name>"`; it does not replace the catalog's `EventContractId`.

| Diagnostic | What to change |
| --- | --- |
| `PLAY0469` | Do not copy the same-source command identifier into payload. Inline copies warn; plain copies with explicit `for` are information. Review persistence before changing a contract. |
| `PLAY0471` / `PLAY0472` | Remove a redundant name-equal `id`; an id must be one nonempty quoted value. |
| `PLAY0473` / `PLAY0474` | Avoid declaration/import collisions; inline events belong only in commands, never reactions. |
| `PLAY0475` | Extract the inline event before adding generations. |
| `PLAY0476` | Inline `origin` and unescaped system-assigned production metadata are forbidden. |
| `PLAY0477` | Use one nonempty Markdown documentation fence. |

MCP can declare a missing produced event (`PLAY0166`), add explicit routing
(`PLAY0478`), remove a redundant pin (`PLAY0471`), or remove an inline identifier
copy (`PLAY0469`). The last **changes the event contract**, retires a property,
refuses affected consumers/opaque implementations and is not fix-all. It does
not establish that stored data is safe to migrate. Use
`cratis-screenplay-model-authoring` for discovery, preview, extraction and rename.

