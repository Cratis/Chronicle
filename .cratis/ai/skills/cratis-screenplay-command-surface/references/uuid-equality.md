<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/uuid-equality.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Uuid equality in command conditions

## Contents

- Complete model
- Operands and literals
- Refusals and target boundary

Requires Screenplay 4.205.0 (ESM v28; `screenplay --version`); older binders refuse Uuid comparisons with PLAY0268. Upgrade the compiler without weakening the rule.

A command `require` or `produces when` condition compares Uuid values with `==` and `!=`.
Use it for ownership and self-approval rules: the caller is the owner, the reviewer is not
the author. The model below needs `command reads protected` only for the protected read
path in `require`; a comparison between command properties needs no preamble.

## Complete model

```screenplay
domain Documents
command reads protected
concept DocumentId : Uuid
concept PersonId : Uuid
module Library
  feature Ownership
    slice StateView CurrentDocument
      event DocumentCreated
        owner PersonId
      event DocumentRemoved
      readmodel Document
        id DocumentId key
        owner PersonId
      projection CurrentProjection => Document
        from DocumentCreated
          owner = owner
        remove with DocumentRemoved
    slice StateChange CreateDocument
      command CreateDocument
        id DocumentId identifier
        owner PersonId
        produces DocumentCreated
          for id
          owner = owner
    slice StateChange RenameDocument
      command RenameDocument
        id DocumentId identifier
        caller PersonId
        title String
        reads Document by id
        validate
          require caller == Document.owner
            message "Only the owner can rename a document"
        produces DocumentRenamed
          for id
          title = title
      event DocumentRenamed
        title String
      specification OwnerRenames
        given DocumentCreated
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          owner = "0b6c1f0e-7d3a-4a52-9a3e-6f1d2c3b4a59"
        when RenameDocument
          id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          caller = "0b6c1f0e-7d3a-4a52-9a3e-6f1d2c3b4a59"
          title = "Plan"
        then DocumentRenamed
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          title = "Plan"
      specification StrangerCannotRename
        given DocumentCreated
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          owner = "0b6c1f0e-7d3a-4a52-9a3e-6f1d2c3b4a59"
        when RenameDocument
          id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          caller = "9d2b0c1e-1a2b-4c3d-8e4f-5a6b7c8d9e0f"
          title = "Plan"
        then error "Only the owner can rename a document"
    slice StateChange ReviewDocument
      command ReviewDocument
        id DocumentId identifier
        author PersonId
        reviewer PersonId
        produces DocumentReviewed
          for id
        produces when reviewer != author
          ReviewIndependent
            for id
      event DocumentReviewed
      event ReviewIndependent
      specification IndependentReview
        when ReviewDocument
          id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          author = "0b6c1f0e-7d3a-4a52-9a3e-6f1d2c3b4a59"
          reviewer = "9d2b0c1e-1a2b-4c3d-8e4f-5a6b7c8d9e0f"
        then DocumentReviewed
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        then ReviewIndependent
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
      specification SelfReview
        when ReviewDocument
          id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          author = "0b6c1f0e-7d3a-4a52-9a3e-6f1d2c3b4a59"
          reviewer = "0b6c1f0e-7d3a-4a52-9a3e-6f1d2c3b4a59"
        then DocumentReviewed
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

## Operands and literals

- An operand is a required scalar command property of type `Uuid`, a Uuid concept or
  identity, a protected read path (`Document.owner`, in `require` only) or a literal.
- When both sides are concepts or identities they must be the same concept. A bare `Uuid`
  compares with any Uuid concept. Comparing two different concepts (`id == Document.owner`
  above) is PLAY0268: it keeps an authorization rule from comparing unrelated identities.
  Convert the model rather than loosening the rule: give both sides the same concept.
- A literal is a lowercase hyphenated Uuid such as `0b6c1f0e-7d3a-4a52-9a3e-6f1d2c3b4a59`.
  Uppercase or unhyphenated text is PLAY0273.
- A comparison selects ESM v28. The same model without a Uuid comparison keeps its earlier version.

## Refusals and target boundary

PLAY0268 still refuses ordering (`<`, `>`), optional operands, Uuid validation rules on
a command property (`caller == "…"` under `validate`), Uuid rules on a concept, read paths
in `produces when`, collection operands, and text property, `$context` or reaction `where` operands; a
text literal such as `"x"` reports PLAY0273. Compare the Uuid
in a `require` instead of a property rule.

A renderer or CLI that does not admit v28 refuses it (Stage with STAGE-ESM-016, a CLI
with CLI-RENDER-004, or PLAY0268 when its bundled compiler predates Uuid equality); the
standalone compiler and `screenplay test` are unaffected. Keep the condition and hand-write its enforcement through render-and-gap-fill.
Specify the accepting and refusing caller, as the model does. `screenplay <root> --warnaserror`
must exit zero and `screenplay test <root>` must pass.
