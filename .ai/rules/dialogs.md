---
applyTo: "**/*.tsx"
profile: application
paths:
  - "**/*.tsx"
---

# Using Dialogs

The Cratis dialog wrappers handle command execution, validation timing, loading states, and footer buttons consistently. Using PrimeReact's raw `Dialog` bypasses all of this and leads to inconsistent UX.

## Choose the Correct Dialog Type

- If confirm executes a command, use `CommandDialog` from `@cratis/components/CommandDialog`.
- If no command is executed on confirm, use `Dialog` from `@cratis/components/Dialogs`.
- **Never** import `Dialog` from `primereact/dialog` directly.

## When Using `CommandDialog`

- Pass the command constructor to `command={}`. `CommandDialog` handles instantiation, execution, and confirm/cancel buttons.
- Use command form fields (`InputTextField`, `TextAreaField`, etc. from `@cratis/components/CommandForm`) for user-input values.
- `CommandDialog` automatically disables confirm while the command executes.
- Any value that must be present for the form to be considered valid (i.e. passes `validateRequiredProperties`) must be supplied via `initialValues`, **not** via `onBeforeExecute`.
  - `onBeforeExecute` fires only at execution time — the command is already validated before it runs, so values set there never influence `isValid` and the OK/Submit button will remain permanently disabled.
  - Use `initialValues` for injected context values (e.g. a parent entity id passed as a prop).
  - Use `onBeforeExecute` only for transformations that should not affect form validity (e.g. generated IDs).

```tsx
import { DialogProps, DialogResult } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { RegisterProject } from './Registration';
import { Guid } from '@cratis/fundamentals';

export const AddProject = ({ closeDialog }: DialogProps) => {
    return (
        <CommandDialog<RegisterProject>
            command={RegisterProject}
            title="Add Project"
            okLabel="Add"
            cancelLabel="Cancel"
            onBeforeExecute={(values) => {
                values.projectId = Guid.create();  // generated, not user input
                return values;
            }}>
            <InputTextField<RegisterProject>
                value={instance => instance.name}
                title="Project name"
                placeholder="My Project"
            />
        </CommandDialog>
    );
};
```

To await the result from the parent:

```tsx
const [AddProjectDialog, showAddProjectDialog] = useDialog(AddProject);

const [result] = await showAddProjectDialog();
if (result === DialogResult.Ok) {
    // Dialog confirmed and command executed successfully
}
```

### `onSuccess` vs `onConfirm`

- **`onSuccess(response)`** fires only after the command succeeds and receives the typed command response — use it for `closeDialog(DialogResult.Ok, response)`, refreshing a query, or a toast.
- **`onConfirm()`** receives **no** command result. It is a close gate after successful execution: return `true` to let the wrapper close, `false`/`undefined` to keep it open. **Do not use `onConfirm` as a command-result handler.**

### `onBeforeExecute` is a transformer

It receives the current command values and **must return them** (mutated or not). **Returning `void` executes the command with `undefined` values.** It runs only on submit — never use it to seed *required* values (validation runs against pre-transform state, so a value seeded here never makes the form valid and the submit button stays permanently disabled). Seed required values via `initialValues`; reserve `onBeforeExecute` for transforms that don't affect validity (e.g. a generated id).

### CommandForm fields

Use built-in `CommandForm` fields (from `@cratis/components/CommandForm`) for every user-input value — a raw PrimeReact control inside a command dialog bypasses `CommandFormFieldWrapper`, so validation never re-runs and the submit button stays **permanently disabled**. Catalog: `InputTextField`, `NumberField`, `DropdownField`, `CheckboxField`, `TextAreaField`, `CalendarField`, `RadioButtonField`, `RadioGroupField`, `ChipsField`, `MultiSelectField`, `ColorPickerField`, `SliderField`.

- The `value={c => c.name}` **accessor lambda doubles as the binding and type-checked field selection** — renaming a command property surfaces a compile error at every binding.
- **`RadioGroupField<T>`** renders a whole group from data (`options`/`optionLabel`/`optionValue`, `layout='horizontal'|'vertical'`); **`RadioButtonField<T>`** is one component per option (each takes a `buttonValue`). Both infer the value type from the accessor — no `as string` casts.
- **`asCommandFormField(Component, opts)`** (`asCommandFormField`, `WrappedFieldProps` from `@cratis/arc.react/commands`) wraps a custom input so it participates in `CommandForm` like a built-in. `WrappedFieldProps<T>` gives `{ value, onChange, invalid, required, errors }` (`errors` is `string[]` → `errors.join(', ')`); options are `{ defaultValue, extractValue: e => ... }`.
- **`useCommandInstance(Command)`** (`@cratis/arc.react/commands`) returns the live reactive instance the form is bound to — **read** it to drive dependent fields (e.g. read `command.country` to choose a `DropdownField`'s options); never mutate (mutations go through field bindings).

### Opening dialogs — `useDialog` / `useDialogContext`

`useDialog<TResponse, TInput>(Component)` returns `[Wrapper, showFn]`: render `<Wrapper />` in JSX and call `showFn(input)` to open it; it resolves to `[DialogResult, TResponse?]` when the dialog closes. For a new dialog, prefer reading input as **plain typed props** (`<Name>Input`) and obtaining `closeDialog` from **`useDialogContext<TResponse>()`** — rather than declaring a props interface that extends `DialogProps` to thread both input and `closeDialog`. (Existing dialogs that destructure `closeDialog` from `DialogProps` remain valid.) Signal the outcome with `closeDialog(DialogResult.Ok | Cancelled, response?)`.

### Multi-step wizards — `StepperCommandDialog`

For a command split across named steps use `StepperCommandDialog` (`@cratis/components/CommandDialog`) — see the **stepper-command-dialog** skill. ⚠️ It **cannot handle conditional steps**, because `React.Children.count` counts falsy children. When steps are conditional, a step needs non-CommandForm inputs, or cross-step state is complex, fall back to a manual `Dialog` + PrimeReact `Stepper`.

## When Using `Dialog`

Use this for dialogs that collect data and return it without executing a command (e.g. confirmation prompts, pure data-entry dialogs). `Dialog` defaults to OK + Cancel buttons. Use `isValid` to control confirm button state, `okLabel`/`cancelLabel` to customize button text.

```tsx
import { useState } from 'react';
import { DialogProps, DialogResult } from '@cratis/arc.react/dialogs';
import { Dialog } from '@cratis/components/Dialogs';
import { InputText } from 'primereact/inputtext';

export const AddProject = ({ closeDialog }: DialogProps<{ name: string }>) => {
    const [name, setName] = useState('');
    const isValid = name.trim().length > 0;

    return (
        <Dialog
            title="Add Project"
            width='32rem'
            isValid={isValid}
            onConfirm={() => closeDialog(DialogResult.Ok, { name })}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
        >
            <InputText
                value={name}
                onChange={event => setName(event.target.value)}
                autoFocus
            />
        </Dialog>
    );
};
```

## Prefer `DialogButtons` over Custom Button JSX

Use the built-in `DialogButtons` enum instead of rendering manual `<Button>` elements in the `buttons` prop:

```tsx
import { DialogButtons, DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
```

### Available Button Sets

| `buttons` value | Shows |
|---|---|
| `DialogButtons.OkCancel` | Ok + Cancel |
| `DialogButtons.YesNo` | Yes + No |
| `DialogButtons.YesNoCancel` | Yes + No + Cancel |
| `DialogButtons.Ok` | Ok only |
| `null` | No buttons (content-only dialog) |

## Customizing Built-in Buttons

Use `okLabel`/`cancelLabel` to rename the buttons, and `isValid` to disable the confirm button:

```tsx
<Dialog
    title="Import Orders"
    visible={true}
    buttons={DialogButtons.OkCancel}
    okLabel="Upload"
    isValid={!!file && !isUploading}
    onConfirm={handleUpload}
    onCancel={() => closeDialog(DialogResult.Cancelled)}
>
```

## Validation Guard — Keep Dialog Open on Failure

When `onConfirm` needs to keep the dialog open (e.g. a command fails), annotate the handler as `Promise<boolean>` and return `false` to block the close. Return `true` to let the Dialog close itself:

```tsx
const handleConfirm = async (): Promise<boolean> => {
    const result = await myCommand.execute();
    if (!result.isSuccess) return false; // dialog stays open
    return true; // dialog closes
};
```

> **TypeScript note:** Always annotate the function as `Promise<boolean>`. Without it TypeScript infers `Promise<false | void>` which does not satisfy the `ConfirmCallback` type.

## Passing Result Data on Confirm

When the dialog must return data to its caller (e.g. a postal code lookup result), use `onClose` and call `closeDialog` manually, returning `false` to prevent the Dialog from calling it a second time:

```tsx
<Dialog
    title="Confirm Location"
    visible={true}
    buttons={DialogButtons.OkCancel}
    isValid={isValid}
    onClose={(result) => {
        if (result === DialogResult.Ok) {
            closeDialog(DialogResult.Ok, { postalCode, city, latitude, longitude } as MyResult);
            return false; // prevent Dialog from calling closeDialog(Ok) again
        }
        // Cancelled: return undefined so the Dialog calls closeDialog(Cancelled)
    }}
>
```

## Content-only Dialogs (No Action Buttons)

Use `buttons={null}` for dialogs that contain their own internal actions (e.g. a menu + data table) and don't need a confirm/cancel footer:

```tsx
<Dialog
    title="Hubs"
    visible={true}
    width="50vw"
    buttons={null}
    onCancel={() => closeDialog(DialogResult.Cancelled)}
>
    <Menubar model={menuItems} />
    <Listing configurationId={configurationId} />
</Dialog>
```

## Props Reference

| Prop | Type | Notes |
|---|---|---|
| `title` | `string` | Header text (replaces PrimeReact `header`) |
| `visible` | `boolean` | Controls visibility |
| `buttons` | `DialogButtons \| ReactNode \| null` | Prefer `DialogButtons` enum; `null` for no footer |
| `isValid` | `boolean` | Disables the confirm button when `false` |
| `okLabel` | `string` | Override the Ok/Confirm button label |
| `cancelLabel` | `string` | Override the Cancel button label |
| `onConfirm` | `() => boolean \| void \| Promise<boolean> \| Promise<void>` | Called when Ok is clicked; return `false` to keep dialog open, `true` to close |
| `onCancel` | `() => void \| Promise<void>` | Called when Cancel is clicked |
| `onClose` | `(result: DialogResult) => boolean \| void \| Promise<...>` | Combined handler for both Ok and Cancel |
| `width` | `string` | Dialog width (e.g. `'50vw'`) — replaces PrimeReact `style={{ width }}` |
| `resizable` | `boolean` | Default `false` |

PrimeReact-specific props (`style`, `contentStyle`, `modal`, `dismissableMask`, `draggable`, `footer`, `onHide`) are **not** available — do not use them.
