<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/operations.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Systems and operation intent

## Contents

- Declare and produce
- Phases and ordered work
- Specification intent

## Declare and produce

A top-level `system` names an external business system. A slice-owned `operation`
declares exactly one `uses <System>`, typed inputs, optional description and
execute/compensate phases. Operations and their specification steps are
**authoring-only** at Screenplay 4.125.0: compile acceptance is not execution;
binding refuses the whole model with PLAY0268. Source: released `operations.md`.

`produces operation <Name>` declares an inline slice-owned operation with typed
input mappings. Plain `produces <Name>` references an existing event or operation;
it never creates one from a typo. Event and operation names share resolution:
collisions/ambiguity refuse, not event-first guessing. Standalone operation
references can be qualified across slices; that does not broaden event-production
grammar. Operations are produced by commands, not reactions, and take no `for`,
tags or event routing metadata. Inputs cannot be identifier/generated properties.

## Phases and ordered work

Keep mixed event/operation productions in their authored sequence. The intended
future transaction contract enrolls events first, executes operations in authored
order before commit, and compensates according to commit disposition. It does
not promise reverse compensation order, rollback or post-commit delivery.

Each phase accepts a description and one direct file/tagged body or an
`implementation` wrapper with ordered nonblank hints and optional source.
Direct/wrapped sources cannot mix; duplicate phases/payloads refuse.
Description-only/hints-only phases are pending intent, not executable code.
Typed `execute Type` remains an input; bare `execute` opens a phase. Escape an
input named uses as `@uses`, and event-metadata input names where needed.
No automatic operation extraction ships: moving an inline declaration manually
must preserve its production sequence, mappings, comments and attachment paths.

## Specification intent

This complete authoring-only model compiles with `--warnaserror` at 4.125.0.
`screenplay test` intentionally returns exit 3/unbound with PLAY0268; it proves
no failure/compensation behavior today:

```screenplay test=unbound
system Accounting
concept ProjectId : Uuid
module Projects
  feature Registration
    slice StateChange Register
      operation OpenCostCenter
        uses Accounting
        projectId ProjectId
        execute
          description "Open a provisional cost center"
        compensate
          description "Close it if registration does not commit"
      command RegisterProject
        projectId ProjectId identifier
        produces OpenCostCenter
          projectId = projectId
      specification Compensating
        given operation OpenCostCenter fails
        when RegisterProject
          projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        then operation OpenCostCenter
          projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        then compensated OpenCostCenter
```

`given operation ... fails` and `then compensated ...` are leaves. A requested
`then operation` can state a partial input subset, with unique fields and
compatible concrete values. References resolve against declared operations;
compensation intent needs a declared compensate phase. Provider execution is a
separate future admission, never implied by a source fixture.
