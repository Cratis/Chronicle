````instructions
---
applyTo: "**/*.tsx"
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
- Use `onBeforeExecute` only for values that cannot come from form fields (for example generated IDs).

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

## When Using `Dialog`

Use this for dialogs that collect data and return it without executing a command (e.g. confirmation prompts, pure data-entry dialogs). `Dialog` defaults to OK + Cancel buttons. Use `isValid` to control confirm button state, `okLabel`/`cancelLabel` to customise button text.

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

## Customising Built-in Buttons

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

````
