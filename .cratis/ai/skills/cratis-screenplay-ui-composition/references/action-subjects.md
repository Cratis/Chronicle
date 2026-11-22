<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/action-subjects.md -->
# Action subjects: `for item` and `for screen`

## Contents

- Table narrowing
  - Repair repeated advisories one occurrence at a time

Requires Screenplay 4.248.0 or later (`screenplay --version`). Source: Screenplay
`Documentation/screenplay/action-subjects.md` (decision 0110).

A plain or toolbar `action` states whether it acts on one chosen record or on the screen:

- `for item`: the nearest single `data` record, or the selected row of a collection. When a screen
  declares several `data` and a `table` in scope shows exactly one collection, that collection is the
  subject (see [table narrowing](#table-narrowing)). Inputs bind
  from it: explicit `with <input> from item.<field>` first, then case-sensitive same-name
  fields, then one key field with the same declared concept.
- `for screen`: independent of selection; no row fills the inputs. Use it for actions that
  create something or that the user fills in through the form.

The clause follows the command name, before an inline `label`. Only action toolbar items
accept it. A hook (an outlet or toolbar `button` naming a State Change `slice`) takes the same
`for item` or `for screen` part on its line, and an actions outlet can set it for every button
(`outlet rowActions of actions for item`); a hook's `enabled when` that reads `item` while it
acts `for screen` is `PLAY0915` (Requires Screenplay 6.0 or later).

```screenplay
concept TaskId : Uuid
concept Title : String
module Work
  feature Tasks
    slice StateChange AddTask
      command AddTask
        taskId TaskId identifier
        title Title
        produces event TaskAdded
          title Title = title
    slice StateChange CompleteTask
      command CompleteTask
        taskId TaskId identifier
        note String
        produces event TaskCompleted
          note String = note
    slice StateView OpenTasks
      readmodel OpenTask
        taskId TaskId key
        title Title
      projection OpenTasks => OpenTask
        from TaskAdded
          taskId = $eventSourceId
          title = title
        remove with TaskCompleted
      query ListOpenTasks => OpenTask[]
      screen OpenTasksScreen
        title "Open tasks"
        data OpenTask[] via query ListOpenTasks
        action AddTask for screen
          label "Add task"
        action CompleteTask for item
          label "Complete"
          with taskId from item.taskId
        table OpenTask
          column title label "Task"
```

## Table narrowing

Requires Screenplay 6.18.0 or later (`screenplay --version`). A screen often shows a header
record next to a table of rows. An authored plain or toolbar `for item` action then acts on the
one collection `data` that a `table` in scope shows, so the header does not make it ambiguous:

```play
data Header via query HeaderOf
data Row[] via query Rows
table Row
action RetryItem for item
```

Unchanged and still ambiguous: a table over a record, two tables over two collections, and no
table. Guarded actions and inferred (omitted) subjects are unchanged. PLAY0346 names the candidate
`data`; use that list to pick the explicit fix: show one table, or move the action into a screen
section or slot with its own local `data`. The subject is a modeling statement, so do not
restructure a screen only to satisfy `for item`.

State the subject on every plain and toolbar action. Without it, the compiler infers `for item`
when there is exactly one collection `data`, one required non-generated identifier and one key
with the same concept, and `for screen` otherwise. Inference follows types, not intent: here
`AddTask` without a clause infers `for item`, because its `taskId` has the row key's concept,
and would bind the selected row's id to a new task.

| Code | Severity | Meaning | Fix |
| --- | --- | --- | --- |
| PLAY0817 | Error | Malformed, duplicate or misplaced subject clause | One clause after the command name |
| PLAY0818 | Information | Explains an omitted subject's inference | State `for item` or `for screen` |
| PLAY0819 | Warning | The inferred binding differs from the legacy case-insensitive row-name matching | Decide per action: `for screen`, or `for item` with explicit inputs; never bulk-accept |
| PLAY0820 | Warning | Several keys could bind one input | Write `with <input> from item.<field>` |
| PLAY0821 | Warning | A guarded `for screen` action has collection data and cannot choose one item | Use `for item` or single-record data |

A model written before 4.248.0 can report PLAY0818/PLAY0819 on every list-screen action. Review
each one against what the action means; a create action is `for screen` even when the row has a
matching identifier. Compiler inference is authoring evidence only: Scene owns the runtime
selection and form-input behavior.

### Repair repeated advisories one occurrence at a time

PLAY0818 and PLAY0819 repeat with identical text for every action on a list screen. Never
repair by message text, by code or in bulk. For each occurrence:

1. Read that diagnostic's own location and occurrence handle from `read-ast` or the repairs
   view. Handles are revision-bound: refetch them after every apply.
2. Decide what that action means: a create action is `for screen`; a row action is `for item`,
   with explicit `with <input> from item.<field>` when several keys could bind.
3. Propose one edit for that occurrence, apply it, and re-read.
4. Confirm only that occurrence's diagnostic cleared and the others remain until decided.
