# CommandDialog — Reference

`CommandDialog` from `@cratis/components/CommandDialog` is a modal dialog that executes a command when the user confirms.

## How it works

- It creates a command instance internally from the `command` constructor you pass.
- It renders your `CommandForm` fields as children, bound to that instance.
- When the user clicks OK, it calls `command.execute()`.
- `onConfirm` is called **only if** execution succeeds.
- `onCancel` / dismiss closes without executing.
- While execution is in progress, all buttons are disabled and the primary button shows a loading spinner.

---

## Dialog pattern with `useDialog` and `useDialogContext`

```tsx
import { useDialog, useDialogContext, DialogResult } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField, NumberField } from '@cratis/components/CommandForm';
import { CreateProduct } from '../api/Products/CreateProduct';

// 1. Define the dialog component
const CreateProductDialog = () => {
    const { closeDialog } = useDialogContext();
    return (
        <CommandDialog<CreateProduct>
            command={CreateProduct}
            title="Create product"
            okLabel="Create"
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
        >
            <InputTextField<CreateProduct> value={c => c.name} label="Name" />
            <NumberField<CreateProduct> value={c => c.price} label="Price" />
        </CommandDialog>
    );
};

// 2. Use it from the parent
export const ProductsPage = () => {
    const [CreateProductDialogWrapper, showCreateProduct] = useDialog(CreateProductDialog);

    const handleCreate = async () => {
        const [dialogResult] = await showCreateProduct();
        if (dialogResult === DialogResult.Ok) {
            refreshProducts();
        }
    };

    return (
        <>
            <button onClick={handleCreate}>Create product</button>
            <CreateProductDialogWrapper />
        </>
    );
};
```

---

## Returning a value from the dialog

When the command returns a response (e.g. a generated ID), capture it through `useDialogContext` typed to `CommandResult<TResponse>`:

```tsx
import { CommandResult } from '@cratis/arc/commands';

const CreateProductDialog = () => {
    const { closeDialog } = useDialogContext<CommandResult<string>>();
    return (
        <CommandDialog<CreateProduct>
            command={CreateProduct}
            title="Create product"
            onConfirm={async (result) => closeDialog(DialogResult.Ok, result as CommandResult<string>)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
        >
            <InputTextField<CreateProduct> value={c => c.name} label="Name" />
        </CommandDialog>
    );
};

// In the parent:
const [CreateDialogWrapper, showCreate] = useDialog<CommandResult<string>>(CreateProductDialog);
const [dialogResult, commandResult] = await showCreate();
if (dialogResult === DialogResult.Ok && commandResult?.isSuccess) {
    navigateTo(`/products/${commandResult.response}`);
}
```

---

## Edit dialog (pre-populate with existing values)

Use `initialValues` for identity keys (affects change-tracking baseline) and `currentValues` for editable fields:

```tsx
const EditProductDialog = ({ product }: { product: Product }) => {
    const { closeDialog } = useDialogContext();
    return (
        <CommandDialog<UpdateProduct>
            command={UpdateProduct}
            title="Edit product"
            initialValues={{ productId: product.id }}
            currentValues={{ name: product.name, price: product.price }}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
        >
            <InputTextField<UpdateProduct> value={c => c.name} label="Name" />
            <NumberField<UpdateProduct> value={c => c.price} label="Price" />
        </CommandDialog>
    );
};

// Pass props when opening:
const [EditDialogWrapper, showEdit] = useDialog(EditProductDialog);
await showEdit({ product: selectedProduct });
```

- `initialValues` — sets the change-tracking baseline; use for IDs that must be present for validity but are not user-entered
- `currentValues` — pre-populates field display values; use for fields that should show existing data

---

## CommandForm field components

All fields come from `@cratis/components/CommandForm`. Always pass the command type as the generic parameter so the `value` accessor is fully typed.

```tsx
import {
    InputTextField,    // single-line text input
    NumberField,       // numeric input
    CheckboxField,     // boolean toggle
    DateField,         // date picker (CalendarField)
    DropdownField,     // select from options list
    TextAreaField,     // multi-line text
    MultiSelectField,  // multi-value select
    SliderField,       // range slider
    ColorPickerField,  // color picker
    ChipsField,        // tag/chip input
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

The `value` prop takes a function `(commandInstance) => property`. This drives both reading the value and writing it back on change.

---

## CommandDialog props

| Prop | Required | Purpose |
| ---- | -------- | ------- |
| `command` | ✓ | Command constructor (proxy-generated class) |
| `title` | ✓ | Dialog header text |
| `initialValues` | | Values set as the change-tracking baseline (use for injected IDs) |
| `currentValues` | | Values to pre-populate the fields (use for edit dialogs) |
| `onConfirm` | | Called after successful execution; return `false` to keep the dialog open |
| `onCancel` | | Called when cancelled/dismissed |
| `onClose` | | Combined handler for both Ok and Cancel |
| `okLabel` | | Confirm button text (default: `"Ok"`) |
| `cancelLabel` | | Cancel button text (default: `"Cancel"`) |
| `isValid` | | Extra validity gate combined with command form validity |
| `onBeforeExecute` | | Transform command values just before `.execute()` (runs after validation — do not use for required fields) |
| `onFieldChange` | | Callback on every field change |
| `buttons` | | Override button set (`DialogButtons` enum or custom ReactNode) |
| `width` | | Dialog width (e.g. `'32rem'`) |
| `resizable` | | Whether the dialog can be resized |
