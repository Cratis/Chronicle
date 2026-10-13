<!-- cratis-ai-managed: skills/cratis-screenplay-read-surface/references/guarded-actions.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Guarded screen actions

For a gesture choosing a whole action list, rather than a labeled action choosing
one command, use [guarded interactions](../../cratis-screenplay-ui-composition/references/guarded-interactions.md).
They share item conditions/subject rules but run once without a click-time refetch.

## Contents

- State one user decision
- Select and bind
- Guard boundaries

## State one user decision

A label-headed `action "<text>"` (or `$strings.<key>`) can choose a command
from the displayed item's state. The label replaces a plain action's `label`
child. This complete model compiled without warnings on Screenplay 4.125.0;
it is authoring evidence, not a rendered interaction test:

```screenplay
module Tasks
  feature Work
    slice StateChange CloseTask
      command CloseTask
        taskId String identifier
        produces event TaskClosed
    slice StateView Details
      readmodel TaskDetails
        taskId String
        status String
      query TaskById => TaskDetails optional
        by taskId String
      screen Details
        data TaskDetails via query TaskById by taskId
        action "Close"
          when item.status == "open" execute CloseTask
            with taskId from item.taskId
          otherwise hidden
```

## Select and bind

`item` is the nearest container's data item, or selected element for collection
data. Sibling data in the action's container takes precedence over outer data
regardless of text order; ambiguous/missing data warns rather than guessing.
Without an item (loading/no selection), always hide the action, even if it has an
execute fallback.

Check alternatives in authored order whenever data changes; **first match wins**.
At least one when is required. Last optional `otherwise execute <Command>` is
a fallback; `otherwise hidden` or omission hides unmatched actions. Intentional
overlap is valid; only provable shadowing warns. Inputs resolve from explicit
`with <property> from <binding>`, matching subject fields, declared command form,
then renderer input. Terminal collection fields may fill collection inputs;
traversing through a collection is not supported.

A single `navigate to` runs after the chosen command succeeds, not to open that
command's input screen. Open a form/dialog first when it needs separate input.
A click runs the choice shown; if a click-time check changes it, refresh instead
of executing the newly selected command.

## Guard boundaries

Conditions compare item paths to literals, not other paths, route/screen state,
`$context` or `$env`. Equality/inequality, numeric ordering, string contains/starts
with and grouped and/or are supported; and binds tighter than or. Collections
cannot be condition operands. Missing/null fields compare false, including !=
to a non-null literal; == null matches an explicit null, not a missing member.
Type mismatches compare false. Editors validate syntax/shape, not runtime choices.

The guard controls an offer, never server permission. The selected command still
runs authorization, validation and constraints; denial never falls through to a
second command. Screens remain outside portable ESM execution. A downstream
renderer must explicitly admit guarded selection or report unsupported behavior,
not approximate it as a plain action. Checked at historical CLI 3.41.0: its bundled
screen renderer reported unsupported guarded actions with STAGE-SCENE-ACTION-001; source compilation alone
cannot establish usable controls. Since Stage 4.65 a guard comparing `item.<field>` with a literal is
rendered and evaluated at runtime (public CLI 3.53.1 with `cratis/stage:4.73.1`, Cratis/Screenplay#671);
STAGE-SCENE-ACTION-001 remains for a guard Stage cannot evaluate.

Authority: Screenplay `v4.125.0:Documentation/screenplay/screens.md`;
CLI `v3.41.0:Documentation/reference/screenplay.md` for renderer disposition.
