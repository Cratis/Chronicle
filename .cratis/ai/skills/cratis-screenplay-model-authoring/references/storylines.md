<!-- cratis-ai-managed: skills/cratis-screenplay-model-authoring/references/storylines.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Storylines

Read this when a journey that crosses several modules, features and slices must be named, reviewed or walked step by step.
Requires Screenplay 6.6.0 or later (`screenplay --version`).

A `storyline` is authoring metadata: the executable model, the semantic revision and every generated artifact are identical with or without it.

```text
storyline <Name> ["<title>"]
  [description "<text>"]
  <kind> <reference> ["<note>"]*
```

- `<Name>` is unique among the application's storylines (`PLAY0943`); the optional title is what a board shows.
- `<kind>` is `module`, `feature`, `slice`, `command`, `event`, `readmodel`, `query`, `screen` or `reaction`; line order is path order.
- `<reference>` is the element's name; qualify it (`Returns.Requests`, `Requests.RequestReturn`) when several elements of that kind match (`PLAY0198`, warning).
- A `screen` step names any screen: one declared in a slice, or a free screen declared at the top level, in a module or in a feature, whether or not a slice has adopted it yet ([free screens](free-screens-proposals.md)).
- A storyline is declared at the top level, in exactly one file of a folder model.

```screenplay expect PLAY0533
concept ItemId : Uuid

module Returns
  feature Requests
    slice StateChange RequestReturn
      command RequestReturn
        itemId ItemId
        produces ReturnRequested
          itemId = itemId

      event ReturnRequested
        itemId ItemId

    slice StateView PendingReturns
      readmodel PendingReturn
        itemId ItemId

      projection PendingReturn
        from ReturnRequested
          key itemId

      query AllPending => PendingReturn[]

storyline ReturnAnItem "A customer returns an item"
  description "From the request to the list the warehouse works from"
  feature Returns.Requests
  command RequestReturn "The customer asks to return the item"
  event ReturnRequested
  readmodel PendingReturn
  query AllPending "The warehouse sees the return"
```

## Diagnostics

| Code | Severity | Meaning |
| --- | --- | --- |
| `PLAY0940` | Error | Malformed `storyline` declaration |
| `PLAY0941` | Error | A step is not `<kind> <reference> ["<note>"]`, has an unknown kind, or has a line nested under it |
| `PLAY0942` | Warning | A step names no element; remove it or point it at its replacement |
| `PLAY0943` | Error | Two storylines carry the same name |

## Renames and tools

- Renaming or moving an element rewrites the steps that name it (`propose-rename`, `screenplay rename`).
- MCP `list-storylines` (optional `name`) lists each storyline's steps with kind, reference, note and the resolved element's kind, address, location and semantic id, or `missing` / `ambiguous`.
- Storylines are AST nodes (`StorylineSyntax`, `StorylineStepSyntax`): `read-ast` reads them; `propose-ast` and `propose-source` edit them. `search-declarations` and `declaration-details` use kind `Storyline`; `export-workspace` keeps them.
