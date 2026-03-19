# Dialogs — Reference

## Core pattern

Dialogs are **separate components** that receive `closeDialog` as a prop via `DialogProps`. The parent uses `useDialog(DialogComponent)` to get a wrapper and a `show` function.

```tsx
import { DialogProps } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { CreateAccount } from './commands/CreateAccount';

// 1. Define the dialog component
export const CreateAccountDialog = ({ closeDialog }: DialogProps) => {
    return (
        <CommandDialog<CreateAccount>
            command={CreateAccount}
            title="Create Account"
            okLabel="Create"
        >
            <InputTextField<CreateAccount> value={c => c.name} label="Account Name" />
        </CommandDialog>
    );
};
```

```tsx
// 2. Wire it up in the parent
import { useDialog } from '@cratis/arc.react/dialogs';
import { CreateAccountDialog } from './CreateAccountDialog';

export const AccountsPage = () => {
    const [CreateAccountWrapper, showCreateAccount] = useDialog(CreateAccountDialog);

    return (
        <>
            <DataPage ... />
            <CreateAccountWrapper />
        </>
    );
};
```

`showCreateAccount()` opens the dialog. `closeDialog` (injected into the dialog component by the framework) closes it.

---

## `useDialog`

```tsx
import { useDialog } from '@cratis/arc.react/dialogs';

const [DialogWrapper, showDialog] = useDialog(MyDialogComponent);
```

- `DialogWrapper` — render this once in the JSX tree; it controls visibility
- `showDialog(props?)` — call to open; returns a `Promise<[DialogResult, TResponse?]>`

```tsx
const [result, response] = await showDialog({ someInitialProp: value });
if (result === DialogResult.Ok) {
    // handle confirmed result
}
```

Pass props to `showDialog()` when the dialog needs context from the parent (e.g. a selected row to edit).

---

## Passing props to a dialog

Define the dialog's props interface extending `DialogProps`:

```tsx
interface EditAccountDialogProps extends DialogProps {
    accountId: string;
    name: string;
}

export const EditAccountDialog = ({ closeDialog, accountId, name }: EditAccountDialogProps) => {
    return (
        <CommandDialog<EditAccount>
            command={EditAccount}
            title="Edit Account"
            initialValues={{ accountId }}
            currentValues={{ name }}
        >
            <InputTextField<EditAccount> value={c => c.name} label="Account Name" />
        </CommandDialog>
    );
};
```

Then in the parent:

```tsx
const [EditAccountWrapper, showEditAccount] = useDialog(EditAccountDialog);

// Pass the selected row when opening
<DataPage onRowSelected={(row) => showEditAccount({ accountId: row.id, name: row.name })} ... />
<EditAccountWrapper />
```

---

## CommandDialog

Use for dialogs that execute a command on confirm. Import from `@cratis/components/CommandDialog`.

```tsx
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField, NumberField } from '@cratis/components/CommandForm';
```

**Key props:**

| Prop | Purpose |
| --- | --- |
| `command` | Command constructor (proxy-generated class) |
| `title` | Dialog header text |
| `okLabel` | Confirm button text (default: `"Ok"`) |
| `cancelLabel` | Cancel button text (default: `"Cancel"`) |
| `initialValues` | Values set as the change-tracking baseline (e.g. injected IDs) |
| `currentValues` | Values to pre-populate the fields for editing |
| `isValid` | Extra validity gate in addition to field-level validation |
| `onBeforeExecute` | Transform command values just before `.execute()` |

`CommandDialog` automatically disables the confirm button until all required fields are filled.

Use `initialValues` for values that must be present but not visible (e.g. a parent entity ID). Do **not** set them in `onBeforeExecute` — they won't be visible to form validation.

---

## CommandForm field components

All field components come from `@cratis/components/CommandForm`. Pass the command type as the generic parameter so `value` is fully typed.

```tsx
import {
    InputTextField,  // text input
    NumberField,     // number input
    CheckboxField,   // boolean toggle
    DateField,       // date picker
    DropdownField,   // select from options list
    TextAreaField,   // multi-line text
} from '@cratis/components/CommandForm';

<InputTextField<MyCommand> value={c => c.title} label="Title" />
<NumberField<MyCommand> value={c => c.quantity} label="Qty" min={1} />
<CheckboxField<MyCommand> value={c => c.isActive} label="Active" />
<DateField<MyCommand> value={c => c.dueDate} label="Due date" />
<DropdownField<MyCommand>
    value={c => c.status}
    label="Status"
    options={statusOptions}
    optionLabel="label"
    optionValue="value"
/>
<TextAreaField<MyCommand> value={c => c.notes} label="Notes" rows={3} />
```

The `value` prop takes a function `(commandInstance) => property`. This drives both reading the value and writing it back on change.

---

## Dialog (data-only, no command)

Use when the dialog collects data and returns it without executing a command.

```tsx
import { DialogProps, DialogResult } from '@cratis/arc.react/dialogs';
import { Dialog } from '@cratis/components/Dialogs';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';

export const RenameDialog = ({ closeDialog }: DialogProps<{ name: string }>) => {
    const [name, setName] = useState('');

    return (
        <Dialog
            title="Rename"
            isValid={name.trim().length > 0}
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

Never import `Dialog` from `primereact/dialog` — always use `@cratis/components/Dialogs`.
