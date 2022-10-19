# Modal Dialogs

> Part of the [@aksio/cratis-mui](https://www.npmjs.com/package/@aksio/cratis-mui) package.

The goal of the modal dialog encapsulation is:

- Provide a way for simple dialogs with a description and just ok/cancel or yes/no buttons
- Provide an encapsulation for dialogs with complex content and ok/cancel or yes/no buttons
- Encourage separation of dialogs into their own components
- Take out the verbosity of all the MUI artifacts for building up a dialog

## useModal hook

The `useModal` hook gives you a simple way of using the standardized modals.
It allows for both simple strings and more complex components to be used.
Using the hook allows you define the input type into the dialog when it is shown and
the result type populated by the component within the dialog when it is closed.

To know when the dialog is closed and get the values from the dialog, you pass it
a `onClose` function which takes two arguments:

| Argument | Description |
| -------- | ----------- |
| result | An enum of type `ModalResult` with values `Success`, `Failed`, `Dismissed`|
| output | Optional result in the type the dialog defines |

The `useModal` hook returns a tuple with two functions that can be called

```ts
[ShowModal, CloseModal]
```

The `ShowModal` function takes the input to the modal in the type specified by the modal
component.

## Simple string confirmation dialogs

For simple confirmation dialogs where one wants to ask a question and let the user confirm
by clicking ok/cancel or yes/no is a very common scenario in applications.

```tsx
import { useModal, ModalResult } from '@aksio/cratis-mui';

export const Transfer = () => {
    const [amount, setAmount] = useState(0);

    const [showConfirmationDialog] = useModal(
        `Transfer money?`,
        ModalButtons.OkCancel,
        `Are you sure you want to transfer '${amount})'?`,
        async (result) => {
            if (result == ModalResult.success) {
                /*
                    perform operation when successful
                */
            }
        });
    return (
        <>
            <TextField label='Amount' required defaultValue={amount} onChange={(e) => setAmount(e.currentTarget.value)} />
            <Button onClick={showConfirmationDialog} >Transfer</Button>
        </>
    );
};
```

> Note: Since the simple confirmation dialogs does not provide a component that has a well
> defined return type, the result in the `ModalClosed` callback will be `undefined`.

## Component based dialogs

The component based dialogs support putting in your own component as the content of the dialog.
Instead of passing a string to the `useModal` hook for the content, you can pass it a component.

Lets say you have a modal for editing a bank accounts details:

```tsx
export interface EditAccountInput {
    name: string;
    description: string;
}

export interface EditAccountOutput {
    name: string;
    description: string;
}

export const EditAccountDetails =  (props: IModalProps<EditAccountInput, EditAccountOutput>) => {
    const [name, setName] = useState(props.input.name);
    const [description, setDescription] = useState(props.input.description);

    props.onClose(result => {
        return {
            name,
            description
        }
    });

    return (
        <div>
            <TextField label='Name' required defaultValue={name} onChange={(e) => setId(e.currentTarget.value)} />
            <TextField label='Description' required defaultValue={name} onChange={(e) => setId(e.currentTarget.value)} />
        </div>
    );
};
```

> Note: The `IModalProps<>` given to the dialog component holds a method called `onClose()`. This is used
> to give the callback that is called when the dialog is about to close. This is typically where you can
> return the state from the dialog.

```tsx
import { useModal, ModalResult } from '@aksio/cratis-mui';
import { EditAccountDetails } from './EditAccountDetails';

type BankAccount = {
    name: string;
    description: string;
};


export const BankAccounts = () => {
    const [selectedBankCount, setSelectedBankAccount] = useState<BankAccount>();
    const [showEditAccount, closeEditAccount] = useModal(
        'Edit account',
        ModalButtons.OkCancel,
        EditAccountDetails,
        async (result, output) => {
            if( result == ModalResult.success ) {
                /*
                    perform an edit operation if successful

                    output will of type EditAccountOutput holding the values from the dialogs
                    onClose.
                */
            }
        }
    )

    const editAccount = () => {
        showEditAccount({ name: selectedBankAccount.name, description: selectedBankAccount.description });
    };

    return (
        <div>
            {/*
            ... a data grid / table with accounts and logic for selecting an account ...
            */}

            <Button onClick={editAccount}>Edit account</Button>
        </div>
    );
}
```

> Note: Since the `EditAccountDetails` component specifies an input type, we can't just call it without
> passing a value. So the buttons `onClick` calls a method that forwards values from the `selectedBankAccount`.
