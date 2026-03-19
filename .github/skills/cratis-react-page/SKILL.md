---
name: cratis-react-page
description: Step-by-step guidance for building a React page in a Cratis Arc application — listing data with DataPage, toolbar actions with CommandDialog, row selection, detail panels, observable queries, and MVVM. Use whenever building or modifying a React page that lists or displays data, adding a table or grid, wiring up Add/Edit/Delete actions, using DataPage, DataTableForQuery, CommandDialog, or any @cratis/components. Also trigger when connecting a React component to a proxy-generated query or observable query.
---

## Workflow

### Step 1 — Prerequisites

- Backend query and command endpoints must already exist (see `cratis-readmodel` and `cratis-command` skills)
- Run `dotnet build` on the backend to regenerate proxies
- All proxy `.ts` files in `<CratisProxiesOutputPath>` must be committed/saved before importing

Imports you will need:

```tsx
import { DataPage, MenuItem, MenuItems, Columns } from '@cratis/components';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { useDialog, useDialogContext, DialogResult } from '@cratis/arc.react/dialogs';
import { Column } from 'primereact/column';
```

---

### Step 2 — Basic DataPage setup

`DataPage` combines a page header, data table, and optional detail panel. Define columns using `<DataPage.Columns>` with PrimeReact `<Column>` elements as children. The `title` and `emptyMessage` props are required.

```tsx
import { DataPage } from '@cratis/components';
import { Column } from 'primereact/column';
import { AllAccounts } from './queries/AllAccounts';

export const AccountsPage = () => {
    return (
        <DataPage
            title="Accounts"
            query={AllAccounts}
            emptyMessage="No accounts found."
        >
            <DataPage.Columns>
                <Column field="name" header="Name" />
                <Column field="balance" header="Balance" />
            </DataPage.Columns>
        </DataPage>
    );
};
```

---

### Step 3 — Add menu actions

Wrap `<MenuItem>` elements in `<DataPage.MenuItems>` to populate the toolbar above the table.

```tsx
import { DataPage, MenuItem } from '@cratis/components';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { useDialog, useDialogContext, DialogResult } from '@cratis/arc.react/dialogs';
import { Column } from 'primereact/column';
import { CreateAccount } from './commands/CreateAccount';
import { AllAccounts } from './queries/AllAccounts';

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
        </CommandDialog>
    );
};

export const AccountsPage = () => {
    const [CreateAccountDialogWrapper, showCreate] = useDialog(CreateAccountDialog);

    return (
        <>
            <DataPage
                title="Accounts"
                query={AllAccounts}
                emptyMessage="No accounts found."
            >
                <DataPage.MenuItems>
                    <MenuItem label="Create Account" icon="pi pi-plus" command={showCreate} />
                </DataPage.MenuItems>
                <DataPage.Columns>
                    <Column field="name" header="Name" />
                    <Column field="balance" header="Balance" />
                </DataPage.Columns>
            </DataPage>
            <CreateAccountDialogWrapper />
        </>
    );
};
```

See `references/dialogs.md` for `useDialog`/`useDialogContext` API detail.

---

### Step 4 — Row selection and edit dialog

Use `onSelectionChange` to track the selected row and pass it to an edit dialog. Use `disableOnUnselected` on the edit/delete menu items to disable them when nothing is selected.

```tsx
import { useState } from 'react';
import { DataPage, MenuItem } from '@cratis/components';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { useDialog, useDialogContext, DialogResult } from '@cratis/arc.react/dialogs';
import { DataTableSelectionSingleChangeEvent } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { EditAccount } from './commands/EditAccount';
import { AllAccounts } from './queries/AllAccounts';

const EditAccountDialog = ({ account }: { account: AccountSummary }) => {
    const { closeDialog } = useDialogContext();
    return (
        <CommandDialog<EditAccount>
            command={EditAccount}
            title="Edit Account"
            initialValues={{ accountId: account.id }}
            currentValues={{ name: account.name }}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
        >
            <InputTextField<EditAccount> value={c => c.name} label="Account Name" />
        </CommandDialog>
    );
};

export const AccountsPage = () => {
    const [selectedAccount, setSelectedAccount] = useState<AccountSummary | undefined>();
    const [EditAccountDialogWrapper, showEdit] = useDialog(EditAccountDialog);

    const handleEdit = () => {
        if (selectedAccount) showEdit({ account: selectedAccount });
    };

    return (
        <>
            <DataPage
                title="Accounts"
                query={AllAccounts}
                emptyMessage="No accounts found."
                selection={selectedAccount}
                onSelectionChange={(e: DataTableSelectionSingleChangeEvent<AccountSummary>) => setSelectedAccount(e.value)}
            >
                <DataPage.MenuItems>
                    <MenuItem label="Edit" icon="pi pi-pencil" disableOnUnselected command={handleEdit} />
                </DataPage.MenuItems>
                <DataPage.Columns>
                    <Column field="name" header="Name" />
                </DataPage.Columns>
            </DataPage>
            <EditAccountDialogWrapper />
        </>
    );
};
```

---

### Step 5 — Choose observable vs standard query

Use an **observable query** when the data updates in real time without a user-triggered refresh. Pass it as the `query` prop — `DataPage` detects the query type automatically.

```tsx
import { AllAccountsLive } from './queries/AllAccountsLive';

<DataPage
    title="Accounts"
    query={AllAccountsLive}
    emptyMessage="No accounts found."
>
    <DataPage.Columns>
        <Column field="name" header="Name" />
    </DataPage.Columns>
</DataPage>
```

Observable query results push updates automatically. Use a standard (`IQueryFor`) query for data that only changes when the user takes an explicit action.

---

### Step 6 — Detail panel (optional)

Provide a `detailsComponent` to render a side panel when a row is selected. It receives `item` (the selected row) and an optional `onRefresh` callback.

```tsx
const AccountDetail = ({ item }: { item: AccountSummary }) => (
    <div className="p-4">
        <h2>{item.name}</h2>
        <p>Balance: {item.balance}</p>
    </div>
);

<DataPage
    title="Accounts"
    query={AllAccounts}
    emptyMessage="No accounts found."
    detailsComponent={AccountDetail}
>
    <DataPage.Columns>
        <Column field="name" header="Name" />
    </DataPage.Columns>
</DataPage>
```

The detail panel is hidden when no row is selected.

---

### Step 7 — MVVM view model (optional, for complex pages)

For pages with complex state or coordination logic, wrap the page in a view model:

```tsx
import { withViewModel } from '@cratis/arc.react.mvvm';
import { injectable } from 'tsyringe';

@injectable()
class AccountsViewModel {
    selectedAccount?: AccountSummary;
}

export const AccountsPage = withViewModel(AccountsViewModel, ({ viewModel }) => (
    <DataPage
        title="Accounts"
        query={AllAccounts}
        emptyMessage="No accounts found."
    >
        <DataPage.Columns>
            <Column field="name" header="Name" />
        </DataPage.Columns>
    </DataPage>
));
```

See `references/mvvm.md` for full MVVM guidance.

---

## Quick decision guide

| Need | Use |
| --- | --- |
| Read-only list | `DataPage` with standard `query` |
| Real-time updates | `DataPage` with observable `query` |
| Add / create action | `DataPage.MenuItems` + `MenuItem` + `CommandDialog` + `useDialog` |
| Edit selected row | `disableOnUnselected` + `onSelectionChange` + `CommandDialog` |
| Side detail panel | `detailsComponent` prop on `DataPage` |
| Complex page logic | `withViewModel` MVVM wrapper |

## Reference files

- `references/data-page.md` — DataPage props, MenuItems, Columns, detailsComponent
- `references/data-table.md` — Standalone DataTableForQuery / DataTableForObservableQuery
- `references/dialogs.md` — `useDialog`, `useDialogContext`, `CommandDialog` full API
- `references/mvvm.md` — `withViewModel`, `@injectable`, `IHandleProps`, reactive props
