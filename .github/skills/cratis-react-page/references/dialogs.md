# Dialogs — Reference

## useDialog

`useDialog` wires up a dialog component without needing to manage open/close state yourself.

```tsx
import { useDialog } from '@cratis/arc.react/dialogs';

const [DialogWrapper, showDialog] = useDialog<TResult>(DialogComponent);
```

- `TResult` — the value type returned when the dialog closes (e.g. `CommandResult<string>` or a data object)
- `DialogWrapper` — render this once in JSX; it mounts the dialog when `showDialog()` is called
- `showDialog()` — call to open the dialog; returns a `Promise<[DialogResult, TResult | undefined]>`

```tsx
const [result, value] = await showDialog();
if (result === DialogResult.Ok) {
    // use value
}
```

Pass data to the dialog by calling `showDialog` with a props object:

```tsx
await showDialog({ account: selectedAccount });
```

## useDialogContext

Inside a dialog component, call `useDialogContext<TResult>()` to get the `closeDialog` function:

```tsx
import { useDialogContext, DialogResult } from '@cratis/arc.react/dialogs';

const MyDialog = () => {
    const { closeDialog } = useDialogContext<MyResult>();

    return (
        <Dialog
            title="My Dialog"
            onConfirm={() => closeDialog(DialogResult.Ok, resultValue)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
        >
            {/* content */}
        </Dialog>
    );
};
```

## CommandDialog

`CommandDialog` (from `@cratis/components/CommandDialog`) executes a command when the user confirms. Use it inside a component that calls `useDialogContext` for the close function.

```tsx
import { useDialogContext, DialogResult } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField, NumberField } from '@cratis/components/CommandForm';
import { CreateAccount } from '../api/Accounts/CreateAccount';

const CreateAccountDialog = () => {
    const { closeDialog } = useDialogContext();
    return (
        <CommandDialog<CreateAccount>
            command={CreateAccount}
            title="Create Account"
            okLabel="Create"
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
        >
            <InputTextField<CreateAccount> value={c => c.name} label="Account Name" />
            <NumberField<CreateAccount> value={c => c.initialBalance} label="Initial Balance" />
        </CommandDialog>
    );
};

// Parent component
export const AccountsPage = () => {
    const [CreateDialogWrapper, showCreate] = useDialog(CreateAccountDialog);

    return (
        <>
            <button onClick={showCreate}>Create account</button>
            <CreateDialogWrapper />
        </>
    );
};
```

`CommandDialog` calls `onConfirm` only after a successful `command.execute()`, so you do not need to check `isSuccess` yourself.

## Dialog (data collection without a command)

`Dialog` (from `@cratis/components/Dialogs`) is for collecting data or confirming an action without executing a command.

```tsx
import { useState } from 'react';
import { useDialogContext, DialogResult } from '@cratis/arc.react/dialogs';
import { Dialog } from '@cratis/components/Dialogs';
import { InputText } from 'primereact/inputtext';

type CreateProjectResult = { name: string };

const AddProjectDialog = () => {
    const { closeDialog } = useDialogContext<CreateProjectResult>();
    const [name, setName] = useState('');
    const isValid = name.trim().length > 0;

    return (
        <Dialog
            title="Add Project"
            width="32rem"
            isValid={isValid}
            onConfirm={() => closeDialog(DialogResult.Ok, { name })}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
        >
            <InputText
                value={name}
                onChange={e => setName(e.target.value)}
                autoFocus
            />
        </Dialog>
    );
};
```

## CommandDialog props

| Prop | Required | Purpose |
| ---- | -------- | ------- |
| `command` | ✓ | Command constructor (proxy-generated class) |
| `title` | ✓ | Dialog header text |
| `initialValues` | | Values set as the change-tracking baseline (supply injected IDs here) |
| `currentValues` | | Values to pre-populate the fields (use for edit dialogs) |
| `onConfirm` | | Called after successful execution |
| `onCancel` | | Called when cancelled/dismissed |
| `okLabel` | | Confirm button text (default: `"Ok"`) |
| `cancelLabel` | | Cancel button text (default: `"Cancel"`) |
| `isValid` | | Extra validity gate combined with command form validity |
| `onBeforeExecute` | | Transform command values just before `.execute()` (do not use for required fields — it runs after validation) |
| `onFieldChange` | | Callback on every field change |
| `buttons` | | Override button set (`DialogButtons` enum or custom ReactNode) |

## CommandForm field components

All fields from `@cratis/components/CommandForm`. Pass the command type as a generic parameter so the `value` accessor is fully typed:

```tsx
import {
    InputTextField,
    NumberField,
    CheckboxField,
    DateField,
    DropdownField,
    TextAreaField,
} from '@cratis/components/CommandForm';

<InputTextField<MyCommand> value={c => c.title} label="Title" />
<NumberField<MyCommand> value={c => c.quantity} label="Qty" />
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

## Edit dialog pattern

Pre-populate fields with `currentValues` and set identity keys with `initialValues`:

```tsx
const EditAccountDialog = ({ account }: { account: AccountSummary }) => {
    const { closeDialog } = useDialogContext();
    return (
        <CommandDialog<UpdateAccount>
            command={UpdateAccount}
            title="Edit Account"
            initialValues={{ accountId: account.id }}
            currentValues={{ name: account.name }}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
        >
            <InputTextField<UpdateAccount> value={c => c.name} label="Account Name" />
        </CommandDialog>
    );
};

// Pass the selected account as props when opening
const [EditDialogWrapper, showEdit] = useDialog(EditAccountDialog);
await showEdit({ account: selectedAccount });
```

- `initialValues` — sets the change-tracking baseline; use for IDs that must be present for validity but are not user-entered
- `currentValues` — pre-populates field display values; use for editable fields that should show existing data
