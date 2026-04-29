---
name: stepper-command-dialog
description: Step-by-step guidance for building a multi-step wizard dialog (StepperCommandDialog) in a Cratis Arc application. Use whenever a command requires gathering information across multiple steps, implementing a wizard flow, breaking a complex form into named stages, or using StepperCommandDialog, StepperPanel, validateOnInit, or wizard-style navigation.
---

# StepperCommandDialog — Wizard Dialogs

`StepperCommandDialog` organizes a single command form across multiple named steps. Users navigate with **Previous** and **Next** buttons; **Submit** only appears on the last step when every field across all steps is valid.

Use this instead of `CommandDialog` when:
- The form has too many fields to show at once
- Fields can be grouped into logical stages (e.g. "Contact Info → Project Details → Summary")
- You want guided, linear input with per-step validation feedback
- The operation feels like a wizard or an onboarding flow

---

## Step 1 — Define the command

A single command collects all fields across all steps. Each step contributes properties to the same command instance.

```tsx
// API/Projects/CreateProject.ts  (proxy-generated — run dotnet build first)
// C# side:
[Command]
public record CreateProject(string Name, string Email, string Description, decimal Budget)
{
    public ProjectCreated Handle() => new(Name, Email, Description, Budget);
}
```

---

## Step 2 — Build the dialog component

```tsx
import { StepperCommandDialog } from '@cratis/components/CommandDialog';
import { StepperPanel } from 'primereact/stepperpanel';
import { InputTextField, TextAreaField, NumberField } from '@cratis/components/CommandForm/fields';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { CreateProject } from '../api/Projects/CreateProject';

const CreateProjectDialog = () => {
    const { closeDialog } = useDialogContext();

    return (
        <StepperCommandDialog<CreateProject>
            command={CreateProject}
            title="Create New Project"
            okLabel="Create"
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
        >
            <StepperPanel header="Contact Info">
                <InputTextField<CreateProject>
                    value={c => c.email}
                    title="Contact Email"
                    placeholder="Enter contact email"
                    type="email"
                />
            </StepperPanel>
            <StepperPanel header="Project Details">
                <InputTextField<CreateProject>
                    value={c => c.name}
                    title="Project Name"
                    placeholder="Enter project name"
                />
                <TextAreaField<CreateProject>
                    value={c => c.description}
                    title="Description"
                    placeholder="Describe the project"
                    rows={4}
                />
            </StepperPanel>
            <StepperPanel header="Budget">
                <NumberField<CreateProject>
                    value={c => c.budget}
                    title="Budget"
                    placeholder="Enter budget"
                />
            </StepperPanel>
        </StepperCommandDialog>
    );
};
```

**Rules:**
- Each `StepperPanel` takes a `header` string — this is the step label shown in the wizard navigation bar
- All `CommandForm` fields inside any `StepperPanel` are bound to the **same** command instance
- Fields map to command properties via the `value={c => c.propertyName}` accessor
- The `Next` button is disabled while the current step has validation errors
- `Submit` only appears on the **last** step when all fields (across all steps) are valid

---

## Step 3 — Wire the dialog to a parent component

```tsx
import { useDialog } from '@cratis/arc.react/dialogs';

export const ProjectsPage = () => {
    const [CreateProjectDialogWrapper, showCreateProject] = useDialog(CreateProjectDialog);

    return (
        <>
            <button
                className="p-button p-component"
                onClick={() => showCreateProject()}
            >
                New Project
            </button>
            <CreateProjectDialogWrapper />
        </>
    );
};
```

---

## Navigation behavior

| Step | Footer buttons shown |
|------|---------------------|
| First step | **Next** |
| Middle step | **Previous**, **Next** |
| Last step (any step invalid) | **Previous** |
| Last step (all valid) | **Previous**, **Submit** |

Cancel is always available via the **×** button in the dialog header.

---

## Validation indicators

Step number circles in the wizard navigation bar change color to reflect validity:

- **Red circle** — the step contains at least one field with a validation error
- **Green circle** — the step has been visited (navigated through) and all its fields are valid
- **Default color** — the step has not been visited yet
- **Dimmed** — a step that is not the currently active step

To trigger validation immediately on open (before the user types anything), pass `validateOnInit`:

```tsx
<StepperCommandDialog
    command={CreateProject}
    validateOnInit
    ...
>
```

This is useful when the dialog opens with pre-populated values that may already be invalid.

---

## Customizing step labels

The `okLabel`, `nextLabel`, and `previousLabel` props override the default button text:

```tsx
<StepperCommandDialog
    command={CreateProject}
    title="Register Employee"
    okLabel="Register"
    nextLabel="Continue →"
    previousLabel="← Back"
    ...
>
```

---

## Pre-populating values (edit wizard)

Use `currentValues` for fields the user can change and `initialValues` for hidden/fixed fields (e.g. an ID):

```tsx
<StepperCommandDialog
    command={UpdateProject}
    currentValues={{ name: project.name, description: project.description }}
    initialValues={{ projectId: project.id }}
    ...
>
```

---

## Stepper orientation

For longer wizards, vertical orientation can be more readable:

```tsx
<StepperCommandDialog orientation="vertical" ...>
    <StepperPanel header="Step One"> ... </StepperPanel>
    <StepperPanel header="Step Two"> ... </StepperPanel>
</StepperCommandDialog>
```

---

## Props reference

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `command` | `Constructor<TCommand>` | — | **Required.** Command class constructor |
| `title` | `string` | — | **Required.** Dialog header title |
| `children` | `StepperPanel[]` | — | **Required.** Wizard steps |
| `okLabel` | `string` | `'Submit'` | Submit button label (last step) |
| `nextLabel` | `string` | `'Next'` | Next button label |
| `previousLabel` | `string` | `'Previous'` | Previous button label |
| `visible` | `boolean` | `true` | Controls dialog visibility |
| `width` | `string` | `'600px'` | Dialog width |
| `isValid` | `boolean` | — | Extra validity gate combined with form validity |
| `validateOnInit` | `boolean` | — | Run validation on mount to show errors immediately |
| `orientation` | `'horizontal' \| 'vertical'` | `'horizontal'` | Stepper layout direction |
| `linear` | `boolean` | `true` | Require steps to be completed in order |
| `initialValues` | `Partial<TCommand>` | — | Fixed initial values (not shown to user as editable) |
| `currentValues` | `Partial<TCommand>` | — | Pre-populated editable values |
| `onConfirm` | `() => void \| Promise<void>` | — | Called after successful execute |
| `onCancel` | `() => void \| Promise<void>` | — | Called when user dismisses dialog |
| `onBeforeExecute` | `(values: TCommand) => TCommand` | — | Transform values before execution |
| `pt` | `StepperProps['pt']` | — | PrimeReact PassThrough for deep DOM customization |

---

## Common mistakes

| Mistake | Fix |
|---------|-----|
| Putting a Cancel button in the footer | Don't — the × in the dialog header is the cancel action |
| One step per field | Group related fields; aim for 2–5 fields per step |
| Fields from different steps sharing the same `value` accessor | Each property should appear on exactly one step |
| Forgetting `header` on `StepperPanel` | Always set `header` — it is the navigation label |
| Using `CommandDialog` for a 4+ field form | Consider `StepperCommandDialog` to reduce cognitive load |
