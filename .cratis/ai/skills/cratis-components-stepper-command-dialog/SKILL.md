---
name: cratis-components-stepper-command-dialog
description: Build a multi-step wizard dialog for a single Cratis Arc command with StepperCommandDialog and StepperPanel from Cratis Components — named steps, per-step validation, linear navigation, vertical or horizontal orientation, and pre-populated edit wizards. Use when one command gathers information across several stages or a form has too many fields to show at once. Do not use for an ordinary single-page command dialog or for a page shell.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-components-stepper-command-dialog/SKILL.md -->

# Cratis Components stepper command dialogs

`StepperCommandDialog` splits **one** command's form across named steps. The
user moves with Previous and Next; Submit appears on the last step once every
field across every step is valid.

## Verified product sources

| Package | Version | Verified from |
| --- | --- | --- |
| `@cratis/components` | `3.0.0` | its package manifest and `CommandDialog` sources |
| `@cratis/arc.react` | `>=20.3.1 <23` | peer range declared by `@cratis/components@3.0.0` |
| `primereact` | `^11.0.0` | peer of `@cratis/components@3.0.0` |

## Choose it over `CommandDialog` when

- the form has too many fields to show at once;
- the fields group into logical stages ("Contact → Details → Budget");
- guided linear input with per-step feedback is what the task deserves.

A four-field form is usually still an ordinary `CommandDialog`.

## Step 1 — One command for the whole wizard

Every step contributes properties to the same command instance. Define the
command on the backend and run a Debug build so the TypeScript proxy exists
before importing it.

## Step 2 — Build the dialog

```tsx
import { StepperCommandDialog, StepperPanel } from '@cratis/components/CommandDialog';
import { InputTextField, NumberField, TextAreaField } from '@cratis/components/CommandForm';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { CreateProject } from '../api/Projects/CreateProject';

export const CreateProjectDialog = () => {
    const { closeDialog } = useDialogContext();

    return (
        <StepperCommandDialog<CreateProject>
            command={CreateProject}
            title='Create new project'
            okLabel='Create'
            onSuccess={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <StepperPanel header='Contact info'>
                <InputTextField<CreateProject> value={c => c.email} title='Contact email' type='email' />
            </StepperPanel>
            <StepperPanel header='Project details'>
                <InputTextField<CreateProject> value={c => c.name} title='Project name' />
                <TextAreaField<CreateProject> value={c => c.description} title='Description' rows={4} />
            </StepperPanel>
            <StepperPanel header='Budget'>
                <NumberField<CreateProject> value={c => c.budget} title='Budget' />
            </StepperPanel>
        </StepperCommandDialog>
    );
};
```

Rules:

- Every `StepperPanel` needs a `header` — it is the step label.
- All `CommandForm` fields inside any panel bind to the **same** command
  instance through their `value` accessor.
- Each command property belongs on exactly one step.
- Next is disabled while the current step has validation errors.
- Submit appears on the last step only when every field across every step is
  valid.
- `StepperCommandDialog` reads the dialog context when it is hosted by
  `useDialog`, and works standalone when it is not.

## Step 3 — Open it from the page

```tsx
import { useDialog } from '@cratis/arc.react/dialogs';

const [CreateProjectWrapper, showCreateProject] = useDialog(CreateProjectDialog);

<MenuItem label='New project' icon={() => <i className='pi pi-plus' />} command={() => showCreateProject()} />
<CreateProjectWrapper />
```

## Validation timing

Pass `validateOnInit` to run validation on mount so a wizard opened with
pre-populated invalid values shows its errors immediately instead of after the
first keystroke. `validateOn` (`'blur' | 'change' | 'both'`) chooses when
subsequent validation runs.

## Pre-populating an edit wizard

`initialValues` is the synchronous baseline and also the change-tracking
baseline — use it for values that must be present but are not user-entered.
`currentValues` is the reactive overlay for values that arrive late, such as
from a query.

```tsx
<StepperCommandDialog
    command={UpdateProject}
    title='Update project'
    initialValues={{ projectId: project.id }}
    currentValues={{ name: project.name, description: project.description }}>
```

Never seed a value required for validity in `onBeforeExecute` — validation runs
against the pre-transform values, so the Submit button would stay permanently
disabled.

## Orientation and labels

```tsx
<StepperCommandDialog
    command={RegisterEmployee}
    title='Register employee'
    orientation='vertical'
    okLabel='Register'
    nextLabel='Continue'
    previousLabel='Back'>
```

## Props

Dialog-level props:

| Prop | Type | Default |
| --- | --- | --- |
| `command` | the generated command class | **required** |
| `title` | `string` | **required** |
| `children` | `StepperPanel` elements | — |
| `visible` | `boolean` | `true` |
| `width` | `string` | `'600px'` |
| `okLabel` | `string` | `'Submit'` |
| `nextLabel` | `string` | `'Next'` |
| `previousLabel` | `string` | `'Previous'` |
| `showCancel` | `boolean` | `false` |
| `cancelLabel` | `string` | `'Cancel'` |
| `isValid` | `boolean` | extra validity gate on top of form validity |
| `onConfirm` / `onCancel` / `onClose` | close gates | — |
| `onBeforeExecute` | `(values) => values` | transformer; must return the values |
| `style` / `contentStyle` | `CSSProperties` | — |
| `resizable` | `boolean` | `false` — accepted but has no effect |

Stepper-level props (inherited from the stepper customization surface):

| Prop | Type | Default |
| --- | --- | --- |
| `orientation` | `'horizontal' \| 'vertical'` | `'horizontal'` |
| `headerPosition` | `'top' \| 'bottom'` | `'top'` |
| `linear` | `boolean` | `true` |
| `showNavigation` | `boolean` | `true` |
| `showSubmit` | `boolean` | `true` |
| `start` / `end` | `React.ReactNode` | extra content beside the step headers |
| `onChangeStep` | `(event: { index: number }) => void` | — |

Command-form props (`initialValues`, `currentValues`, `validateOn`,
`validateOnInit`, `onSuccess`, `onValidationFailure`, `onFailed`,
`autoServerValidate`, …) all apply as well.

### Two pass-through targets

The inherited `pt`, `ptOptions`, and `unstyled` target the **inner stepper**.
Use `dialogPt`, `dialogPtOptions`, `dialogUnstyled`, and `dialogClassName` to
reach the **outer dialog**. Getting these the wrong way round is the usual cause
of a pass-through that appears to do nothing.

## Dismissal while the command runs

The dialog withdraws its close control, Escape, and backdrop dismissal while the
command is executing, so a half-submitted wizard cannot be abandoned mid-flight.
Do not add your own Cancel button to the footer — set `showCancel` if the wizard
needs one.

## Related components

`CommandStepper` (same subpath) is the stepper without the dialog chrome, for
embedding a wizard directly in a page. It takes the same `StepperPanel`
children.

## Common mistakes

| Mistake | Fix |
| --- | --- |
| A hand-rolled Cancel button in the footer | Set `showCancel`, or rely on the header close control |
| One step per field | Group related fields; aim for two to five per step |
| The same property bound on two steps | Each property appears on exactly one step |
| A `StepperPanel` without `header` | `header` is the navigation label |
| Several panels wrapped in one fragment | A fragment counts as **one** step — give each step its own `StepperPanel` child |
| A raw PrimeReact control for a command value | Use a `CommandForm` field, or validation never re-runs |
| Seeding a required value in `onBeforeExecute` | Use `initialValues` |
| `pt` applied expecting it to reach the dialog | `pt` targets the stepper; use `dialogPt` |

## Verify

- Imports come from `@cratis/components/CommandDialog` and
  `@cratis/components/CommandForm`, not the root barrel.
- Every `StepperPanel` has a `header` and is a direct child.
- Every command property appears on exactly one step.
- Required values come from `initialValues`.
- `onBeforeExecute`, if present, returns the values.
- Pass-through props target the intended element.
- Lint and the TypeScript build pass.
