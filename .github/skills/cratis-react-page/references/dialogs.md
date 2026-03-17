# Dialogs — Reference

## useDialog

`useDialog` wires up a `CommandDialog` without needing to manage open/close state yourself.

```tsx
import { useDialog } from '@cratis/arc.react/dialogs';

const [dialogMediator, openDialog] = useDialog<TContext, TCommand>(
    async (context, command) => {
        // optional: called when user clicks Confirm, after command executes
    }
);
```

- `TContext` — data passed to `openDialog()` (typically a selected row)
- `TCommand` — the command proxy type

Call `openDialog(context?)` to show the dialog. Pass a row or other context when needed.

## useDialogContext

Inside a `CommandDialog`'s `children`, call `useDialogContext()` to read the context value passed by `openDialog`:

```tsx
const AddItemDialog = () => {
    const [item] = useDialogContext<ItemSummary>();
    const [command, setValues] = AddItem.use({ itemId: item?.id });

    return (
        <InputTextField label="Name" value={command.name} onChange={setValues('name')} />
    );
};
```

## CommandDialog

```tsx
import { CommandDialog } from '@cratis/components';

<CommandDialog
    dialogMediator={dialogMediator}
    title="Create Account"
    command={CreateAccount}
    initialValuesMapper={(ctx) => ({ ownerId: ctx.id })}
>
    <InputTextField id="name" label="Account Name" />
    <NumberField id="initialBalance" label="Initial Balance" />
</CommandDialog>
```

### Props

| Prop | Type | Description |
| --- | --- | --- |
| `dialogMediator` | `IDialogMediator` | From `useDialog` |
| `title` | `string` | Dialog header text |
| `command` | Command class | Proxy-generated command class |
| `initialValuesMapper` | `(ctx) => Partial<Command>` | Map context to initial command values |
| `children` | `ReactNode` | `CommandForm` field components |
| `confirmLabel` | `string` | Confirm button text (default: "Save") |
| `cancelLabel` | `string` | Cancel button text (default: "Cancel") |

## CommandForm field components

All field components accept `id` matching the command property name. They read and write via the command proxy automatically when used inside `CommandDialog`.

```tsx
import { InputTextField, NumberField, CheckboxField, DateField, DropdownField, TextAreaField } from '@cratis/components';

<InputTextField id="name" label="Display Name" />
<NumberField id="amount" label="Amount" />
<CheckboxField id="isActive" label="Active" />
<DateField id="dueDate" label="Due Date" />
<DropdownField id="category" label="Category" options={[...]} />
<TextAreaField id="notes" label="Notes" />
```

## Edit dialog pattern

```tsx
const [editDialog, openEdit] = useDialog<AccountSummary, EditAccount>(
    async (row, command) => { /* post-confirm */ }
);

// Trigger from row selection:
<DataPage onRowSelected={openEdit} ... />

<CommandDialog
    dialogMediator={editDialog}
    title="Edit Account"
    command={EditAccount}
    initialValuesMapper={(row) => ({ accountId: row.id, name: row.name })}
>
    <InputTextField id="name" label="Account Name" />
</CommandDialog>
```
