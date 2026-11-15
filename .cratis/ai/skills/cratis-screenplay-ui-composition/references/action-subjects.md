<!-- cratis-ai-managed: skills/cratis-screenplay-ui-composition/references/action-subjects.md -->
# Action subjects: `for item` and `for screen`

Requires Screenplay 4.248.0 or later (`screenplay --version`). Source: Screenplay
`Documentation/screenplay/action-subjects.md` (decision 0110).

A plain or toolbar `action` states whether it acts on one chosen record or on the screen:

- `for item`: the nearest single `data` record, or the selected row of a collection. Inputs bind
  from it: explicit `with <input> from item.<field>` first, then case-sensitive same-name
  fields, then one key field with the same declared concept.
- `for screen`: independent of selection; no row fills the inputs. Use it for actions that
  create something or that the user fills in through the form.

The clause follows the command name, before an inline `label`. Only action toolbar items
accept it.

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
