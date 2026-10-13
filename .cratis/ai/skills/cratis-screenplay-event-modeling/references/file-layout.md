<!-- cratis-ai-managed: skills/cratis-screenplay-event-modeling/references/file-layout.md -->
## Choose a file layout

Treat the model folder as one application and choose the coarsest layout
that keeps both the source and its diffs readable:

- Keep one `application.play` while it stays readable top to bottom.
- Use one file per module when modules are simple.
- Use one file per feature when features have sub-features.
- Use one file per slice when the model is large enough that a change must be
  reviewable in isolation.

Reviewability, not an arbitrary line count, triggers the next split. In a large
model, one slice per file makes a scripted edit's blast radius visible in the
diff instead of hiding cross-reference defects in a single enormous file.

At the most granular layout, folders mirror the language, one folder per level,
and every level above the slices is a **barrel file** that declares its scope
and imports what is beneath it:

- `application.play` at the root holds `domain` and imports the rest:
  `import "Shared/*.play"` for the files of `concept`, `type`, `policy`,
  `persona`, `authentication` and `seed`, then one import per module file.
- `<Module>/<Module>.play` declares the module - `description`, `authorize`,
  templates, forms - and its features. Either declare each feature inline and
  import its folder (`feature Orders` / `import "Orders/*.play"`), or import one
  `<Feature>/<Feature>.play` per feature.
- `<Feature>/<Feature>.play`, when a feature has its own file, declares the
  feature and imports its slice files (`import "*.play"`) and any nested
  feature's file.
- A slice file holds just its `slice`, at the top level. The import that brings
  it in places it in its feature, so it does **not** restate `module` or
  `feature` - the barrel above it already says where it belongs.

The root then reads as a table of contents, and every file is reachable from
it: `screenplay application.play` compiles exactly what the imports reach, and
a file no import reaches is not part of the application. Do not hand-write the
older merge-only layout, where barrels import nothing and every slice file
restates `module` and `feature` so the folder merges into one model. It compiles
as a folder, but the root says nothing about what the application holds and
every slice file repeats where it lives. The MCP `expand-layout` tool still
writes that layout; compose its output with imports when you take it over.

**Compose focused files with imports** (v4.48.0, checked at tag `v4.48.0`,
commit `3baf4a4`). `import "<path or glob>"` imports `.play` files relative to
the importing file - `**` crosses folders, `*` stays in one, `..` climbs. Where
the import is written decides where the files belong: at the top level it brings
in whole documents; inside a `module` or `feature` it places each imported file
there, so a file holds only its part of the story - a slice file is just the
`slice`. A root file that imports everything says what the application is made
of:

Layout sketches (file contents, not standalone documents):

```text
domain Acme.Commerce

import "Shared/*.play"
import "Ordering/Ordering.play"
```

```text
module Ordering
  description "Orders, from basket to doorstep"
  feature Orders
    import "Orders/*.play"
```

Every file is imported once. When a root glob and a module file both match a
file, the deepest placement wins; two placements where neither lies inside the
other are an error (`PLAY0457`), and so is a file that declares a module other
than the one it is placed in (`PLAY0459`). A placed file's top level holds only
what its scope can hold (`PLAY0460`) - a `screen template` belongs in the
module file, not in a slice file placed in a feature. `import Customers.CustomerRegistered` without quotes still
names a contract from another bounded context.

⚠️ **Compile the folder, or its root file, as one application.**
`screenplay <root file>` compiles what the root imports. `screenplay <folder>` merges every
`.play` beneath the root *before* resolving, so an event declared in one file and
produced in another resolves. Compiling files individually reports unknown types,
events and policies (`PLAY0165`, `PLAY0166`, `PLAY0167`) that are not missing.
Duplicates *across* files are real errors naming both ends (`PLAY0172`, `PLAY0173`).

Round-tripping a folder does not preserve the order of **modules, features and
slices**: they come back sorted by name, and members of one module or feature
that come from different files print in canonical kind order. Order *within* one
file is kept, and some order carries meaning: `authorize` gates and policy
operands evaluate left to right, and specification events compare in authored
order unless `then events in any order` is stated. Never encode meaning in the
order of modules, features or slices.
