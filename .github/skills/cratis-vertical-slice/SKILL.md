---
name: cratis-vertical-slice
description: Explains how vertical feature slices are structured in a Cratis Chronicle + Arc application — folder layout, what goes in the single backend .cs file, the four slice types (State Change/View/Automation/Translation), and how features compose slices. Use when asking how vertical slices work, where to put files, how to organize a feature folder, which slice type to choose, or how the workflow from C# to TypeScript proxies to React looks. For actively building a new slice end-to-end right now, use new-vertical-slice instead.
---

## Core principle

A vertical slice contains **everything for a single behavior**: the command or query, the events it produces, the projections that build read models, the React component, and the specs. Everything lives together because everything changes together.

One feature folder → many slices.   
One slice folder → one `.cs` file (all backend) + one `.tsx` file (frontend).

---

## Step 1 — Identify the feature and slice type

First, name the feature (a domain noun, pluralized) and identify the slice type:

| Slice type | What it does | Key artifacts |
| --- | --- | --- |
| **State Change** | Mutates system state | Command + events + validators/constraints |
| **State View** | Projects events into queryable data | Read model + projection + queries |
| **Automation** | Reacts to events, makes decisions | Reactor + optional local read models |
| **Translation** | Adapts events between slices | Reactor → triggers commands in own slice |

---

## Step 2 — Create the folder structure

```
Features/
└── <Feature>/                      ← feature root (pluralized domain noun)
    ├── <Feature>.tsx               ← composition page
    ├── <ConceptName>.cs            ← shared ConceptAs<T> types for the feature
    └── <SliceName>/                ← slice (action or view name)
        ├── <SliceName>.cs          ← ALL backend artifacts in ONE file
        ├── <Component>.tsx         ← React component
        └── when_<behavior>/        ← integration specs (state-change slices)
            └── and_<scenario>.cs
```

✅ Correct:
```
Features/Authors/
├── Authors.tsx
├── AuthorId.cs
├── AuthorName.cs
├── Registration/
│   ├── Registration.cs     ← command + event + constraint + validator
│   ├── AddAuthor.tsx
│   └── when_registering/
│       └── and_there_are_no_authors.cs
└── Listing/
    ├── Listing.cs          ← read model + projection + query
    └── Listing.tsx
```

❌ Wrong — never split by artifact type:
```
Features/Authors/
├── Commands/RegisterAuthor.cs
├── Handlers/RegisterAuthorHandler.cs
├── Events/AuthorRegistered.cs
```

**Namespace rule**: Drop the `.Features.` segment.  
`MyApp.Authors.Registration` — not `MyApp.Features.Authors.Registration`.

---

## Step 3 — Write the backend slice file

All backend artifacts for one slice go in a single `<SliceName>.cs` file. File header:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```

For a **State Change** slice, the file contains:
```csharp
[EventType]
public record AuthorRegistered(AuthorName Name);

public class UniqueAuthorName : IConstraint { ... }

public class RegisterAuthorValidator : CommandValidator<RegisterAuthor> { ... }

[Command]
public record RegisterAuthor(AuthorName Name)
{
    public (AuthorId, AuthorRegistered) Handle()
    {
        var authorId = AuthorId.New();
        return (authorId, new(Name));
    }
}
```

For a **State View** slice, the file contains:
```csharp
[ReadModel]
[FromEvent<Registration.AuthorRegistered>]
public record Author(
    [Key] AuthorId Id,
    AuthorName Name)
{
    public static ISubject<IEnumerable<Author>> AllAuthors(IMongoCollection<Author> collection) =>
        collection.Observe();
}
```

See `references/slice-anatomy.md` for all artifact patterns.

---

## Step 4 — Define domain concepts

For every identity or domain value, create a `ConceptAs<T>` type — one file per concept, in the feature folder (or `Features/` root if shared across features).

```csharp
public record AuthorId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly AuthorId NotSet = new(Guid.Empty);
    public static implicit operator Guid(AuthorId id) => id.Value;
    public static implicit operator AuthorId(Guid value) => new(value);
    public static implicit operator EventSourceId(AuthorId id) => new(id.Value.ToString());
    public static AuthorId New() => new(Guid.NewGuid());
}
```

See `references/concepts.md` for all concept patterns.

---

## Step 5 — Build to generate TypeScript proxies

```bash
dotnet build
```

This generates `.ts` proxy files in the configured `<CratisProxiesOutputPath>`. The frontend cannot be written until this succeeds — the proxies are the contract.

---

## Step 6 — Write the React component

```tsx
// Listing.tsx
import { AllAuthors } from '../proxies/Listing';   // auto-generated

export const Listing = () => {
    const [result] = AllAuthors.use();
    return (
        <DataTable value={result.data} ...>
            <Column field="name" header="Name" />
        </DataTable>
    );
};
```

---

## Step 7 — Compose the feature page

The feature's `<Feature>.tsx` assembles slices into a page:

```tsx
// Authors.tsx
import { AddAuthor } from './Registration/AddAuthor';
import { Listing } from './Listing/Listing';
import { useDialog } from '@cratis/arc.react/dialogs';

export const Authors = () => {
    const [AddAuthorDialog, showAddAuthorDialog] = useDialog(AddAuthor);
    const menuItems = [{ label: 'Add Author', command: () => showAddAuthorDialog() }];
    return (
        <Page title="Authors">
            <Menubar model={menuItems} />
            <Listing />
            <AddAuthorDialog />
        </Page>
    );
};
```

---

## Development workflow order

Work in this exact sequence — TypeScript proxies are generated from C# during `dotnet build`:

1. Implement the C# slice file (step 3)
2. Write integration specs for state-change slices
3. `dotnet build` — generates TypeScript proxies (step 5)
4. Implement React component(s) (step 6)
5. Register in the feature composition page (step 7)
6. Add/update routes if needed

---

## Reference files

- `references/slice-anatomy.md` — complete patterns for every artifact type
- `references/slice-types.md` — when to use each slice type with decision guide
- `references/concepts.md` — ConceptAs<T> patterns for all primitive backing types
