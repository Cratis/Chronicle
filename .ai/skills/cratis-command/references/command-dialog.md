# CommandDialog — Reference

`CommandDialog` from `@cratis/components/CommandDialog` is a modal dialog that executes a command when the user confirms.

## How it works

- It creates a command instance internally from the `command` constructor you pass
- It renders your `CommandForm` fields as children, bound to that instance
- When the user clicks OK, it calls `command.execute()`
- `onConfirm` is called **only if** execution succeeds
- `onCancel` / dismiss closes without executing

---

## Dialog pattern with `useDialog` and `DialogProps`

```tsx
import { DialogProps, DialogResult, useDialog } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField, NumberField } from '@cratis/components/CommandForm';
import { CreateProduct } from '../api/Products/CreateProduct';

// 1. Define the dialog component
export const CreateProductDialog = ({ closeDialog }: DialogProps) => {
    return (
        <CommandDialog<CreateProduct>
            command={CreateProduct}
            title="Create product"
            okLabel="Create"
        >
            <InputTextField<CreateProduct> value={c => c.name} label="Name" />
            <NumberField<CreateProduct> value={c => c.price} label="Price" />
        </CommandDialog>
    );
};

// 2. Use it from the parent
const [CreateProductDialogWrapper, showCreateProduct] = useDialog(CreateProductDialog);

const handleCreate = async () => {
    const [result] = await showCreateProduct();
    if (result === DialogResult.Ok) {
        refreshProducts();
    }
};

// 3. Render the wrapper
return (
    <>
        <button onClick={handleCreate}>Create product</button>
        <CreateProductDialogWrapper />
    </>
);
```

`DialogProps` provides `closeDialog` as a prop to the dialog component. `CommandDialog` automatically executes the command on confirm and closes the dialog.

---

## Edit dialog (pre-populate with existing values)

```tsx
interface EditProductDialogProps extends DialogProps {
    product: Product;
}

export const EditProductDialog = ({ closeDialog, product }: EditProductDialogProps) => {
    return (
        <CommandDialog<UpdateProduct>
            command={UpdateProduct}
            title="Edit product"
            currentValues={{ name: product.name, price: product.price }}
            initialValues={{ productId: product.id }}
        >
            <InputTextField<UpdateProduct> value={c => c.name} label="Name" />
            <NumberField<UpdateProduct> value={c => c.price} label="Price" />
        </CommandDialog>
    );
};

// Pass product to the dialog
const [EditDialogWrapper, showEdit] = useDialog(EditProductDialog);
await showEdit({ product: selectedProduct });
```

- `initialValues` sets the baseline for change tracking (values that are not "changes")
- `currentValues` populates the initial field display

---

## CommandForm field components

All fields come from `@cratis/components/CommandForm`. Always pass the command type as the generic parameter so the `value` accessor is fully typed.

```tsx
import {
    InputTextField,    // text input
    NumberField,       // number input
    CheckboxField,     // boolean toggle
    DateField,         // date picker
    DropdownField,     // select from options list
    TextAreaField,     // multi-line text
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

## CommandDialog props

| Prop | Required | Purpose |
| ---- | -------- | ------- |
| `command` | ✓ | Command constructor |
| `title` | ✓ | Dialog header text |
| `initialValues` | | Values set as the change-tracking baseline |
| `currentValues` | | Values to pre-populate the fields |
| `onConfirm` | | Called after successful execution |
| `onCancel` | | Called when cancelled/dismissed |
| `okLabel` | | Confirm button text (default: "Ok") |
| `cancelLabel` | | Cancel button text (default: "Cancel") |
| `isValid` | | Extra validity gate combining with field validation |
| `onBeforeExecute` | | Transform command values just before `.execute()` |
| `onFieldChange` | | Callback on every field change |
| `buttons` | | Override button set (`DialogButtons` enum or custom node) |
