---
name: cratis-react-page
description: Step-by-step guidance for building a React page in a Cratis Arc application — DataPage lists, CommandDialog toolbar actions, row selection, details components, observable queries, and MVVM. Use when building or modifying a page that lists/displays data, adding a table, wiring Add/Edit/Delete, or connecting a component to a proxy-generated query (standard or observable).
---

## Workflow

### Step 1 — Prerequisites

- Backend query and command endpoints must already exist (see `cratis-readmodel` and `cratis-command` skills).
- Run a Release `dotnet build` on the backend to regenerate proxies before importing them.

Import `DataPage` (and its `Column`/`MenuItem` helpers) from the **subpath**, not the root barrel:

```tsx
import { DataPage, MenuItem } from '@cratis/components/DataPage';
import { Column } from 'primereact/column';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { useDialog, DialogProps } from '@cratis/arc.react/dialogs';
```

---

### Step 2 — Basic DataPage setup

`DataPage` combines a toolbar/menu, a data table, and an optional details component. `title`, `query`, `emptyMessage`, and `children` are required; columns are declared compositionally inside `<DataPage.Columns>` using PrimeReact `<Column>`.

```tsx
import { DataPage } from '@cratis/components/DataPage';
import { Column } from 'primereact/column';
import { AllAccounts } from './AllAccounts';

export const AccountsPage = () => (
    <DataPage
        title="Accounts"
        query={AllAccounts}
        emptyMessage="No accounts yet.">
        <DataPage.Columns>
            <Column field="name" header="Name" />
            <Column field="balance" header="Balance" />
        </DataPage.Columns>
    </DataPage>
);
```

---

### Step 3 — Add menu actions

Toolbar actions go in `<DataPage.MenuItems>`. `MenuItem` is a PrimeReact menu item (use `command`, not `onClick`); the `disableOnUnselected` flag greys it out until a row is selected. Create a separate dialog component using `DialogProps`, then wire it up with `useDialog`.

**Dialog component (`CreateAccountDialog.tsx`):**

```tsx
import { DialogProps } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { CreateAccount } from './CreateAccount';

export const CreateAccountDialog = ({ closeDialog }: DialogProps) => (
    <CommandDialog<CreateAccount> command={CreateAccount} title="Create Account" okLabel="Create">
        <InputTextField<CreateAccount> value={c => c.name} title="Account Name" />
    </CommandDialog>
);
```

**Page component:**

```tsx
import { DataPage, MenuItem } from '@cratis/components/DataPage';
import { Column } from 'primereact/column';
import { useDialog } from '@cratis/arc.react/dialogs';
import { CreateAccountDialog } from './CreateAccountDialog';

export const AccountsPage = () => {
    const [CreateAccountWrapper, showCreateAccount] = useDialog(CreateAccountDialog);

    return (
        <>
            <DataPage title="Accounts" query={AllAccounts} emptyMessage="No accounts yet.">
                <DataPage.Columns>
                    <Column field="name" header="Name" />
                </DataPage.Columns>
                <DataPage.MenuItems>
                    <MenuItem label="Add Account" command={() => showCreateAccount()} />
                </DataPage.MenuItems>
            </DataPage>
            <CreateAccountWrapper />
        </>
    );
};
```

See [dialogs.md](../../rules/dialogs.md) and the `stepper-command-dialog` skill for the full dialog patterns.

---

### Step 4 — Row selection and edit dialog

Track selection with `selection` + `onSelectionChange`, and supply the row data as props to the edit dialog.

**Edit dialog (`EditAccountDialog.tsx`):**

```tsx
import { DialogProps } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { EditAccount } from './EditAccount';

interface EditAccountDialogProps extends DialogProps {
    accountId: string;
    name: string;
}

export const EditAccountDialog = ({ accountId, name }: EditAccountDialogProps) => (
    <CommandDialog<EditAccount>
        command={EditAccount}
        title="Edit Account"
        okLabel="Save"
        initialValues={{ accountId }}
        currentValues={{ name }}>
        <InputTextField<EditAccount> value={c => c.name} title="Account Name" />
    </CommandDialog>
);
```

**Page wiring:**

```tsx
const [selected, setSelected] = useState<AccountSummary | undefined>();
const [EditAccountWrapper, showEditAccount] = useDialog(EditAccountDialog);

<DataPage
    title="Accounts"
    query={AllAccounts}
    emptyMessage="No accounts yet."
    selection={selected}
    onSelectionChange={(e) => {
        setSelected(e.value);
        if (e.value) showEditAccount({ accountId: e.value.id, name: e.value.name });
    }}>
    <DataPage.Columns>
        <Column field="name" header="Name" />
    </DataPage.Columns>
</DataPage>
<EditAccountWrapper />
```

- `initialValues` sets the change-tracking baseline (e.g. IDs that must be present but aren't user-entered).
- `currentValues` pre-populates the visible field values.

---

### Step 5 — Observable vs standard query

The **same `query` prop** accepts a standard query (`IQueryFor`) or an observable query (`IObservableQueryFor`) — there is no separate `observableQuery` prop. Pass the observable query proxy and `DataPage` subscribes to live updates automatically:

```tsx
<DataPage title="Accounts" query={ObserveAllAccounts} emptyMessage="No accounts yet.">
    <DataPage.Columns>
        <Column field="name" header="Name" />
    </DataPage.Columns>
</DataPage>
```

Observable results push updates automatically; for snapshot data that changes only on user action, pass the standard query and call `onRefresh` after a command succeeds.

---

### Step 6 — Details component (optional)

`detailsComponent` renders detail for the selected row. It receives `{ item, onRefresh }`:

```tsx
import { IDetailsComponentProps } from '@cratis/components/DataPage';

const AccountDetail = ({ item }: IDetailsComponentProps<AccountSummary>) => (
    <div>{item.name}</div>
);

<DataPage title="Accounts" query={AllAccounts} emptyMessage="No accounts yet." detailsComponent={AccountDetail}>
    <DataPage.Columns>
        <Column field="name" header="Name" />
    </DataPage.Columns>
</DataPage>
```

---

### Step 7 — MVVM view model (for complex pages)

For pages with complex state or coordination logic, wrap the page in a view model (see [react.md](../../rules/react.md)):

```tsx
import { withViewModel } from '@cratis/arc.react.mvvm';
import { injectable } from 'tsyringe';

@injectable()
class AccountsViewModel {
    selectedAccount?: AccountSummary;
    select(account: AccountSummary) { this.selectedAccount = account; }
}

export const AccountsPage = withViewModel(AccountsViewModel, ({ viewModel }) => (
    <DataPage title="Accounts" query={AllAccounts} emptyMessage="No accounts yet."
        selection={viewModel.selectedAccount}
        onSelectionChange={(e) => viewModel.select(e.value)}>
        <DataPage.Columns>
            <Column field="name" header="Name" />
        </DataPage.Columns>
    </DataPage>
));
```

Read `viewModel.property` inside JSX (never destructure observables at the top of the body). See [react.md](../../rules/react.md) for the full MVVM rules.

---

## Quick decision guide

| Need | Use |
|---|---|
| Read-only list | `DataPage` with a standard `query` |
| Real-time updates | `DataPage` with an observable query passed to the same `query` prop |
| Add / create action | `<DataPage.MenuItems>` + `MenuItem` + `CommandDialog` + `useDialog` |
| Edit selected row | `selection` + `onSelectionChange` + `CommandDialog` + `currentValues`/`initialValues` |
| Detail for selected row | `detailsComponent` prop |
| Complex page logic | `withViewModel` MVVM wrapper |

## Key DataPage props

| Prop | Purpose |
|---|---|
| `title` (required) | toolbar title |
| `query` (required) | the query proxy — standard or observable |
| `emptyMessage` (required) | shown when there are no rows |
| `children` (required) | `<DataPage.Columns>` + optional `<DataPage.MenuItems>` |
| `queryArguments` | arguments passed to the query |
| `selection` / `onSelectionChange` | controlled single-row selection |
| `detailsComponent` | `React.FC<IDetailsComponentProps<T>>` rendered for the selected row |
| `globalFilterFields` / `defaultFilters` / `clientFiltering` | filtering |
| `onRefresh` | invoked to re-fetch a standard query |
| `tablePt` / `menubarPt` / `*Unstyled` | PrimeReact pass-through styling |
