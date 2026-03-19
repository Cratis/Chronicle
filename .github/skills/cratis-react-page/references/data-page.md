# DataPage — Reference

`DataPage` (from `@cratis/components`) provides a complete page layout combining a page header, menubar, data table, and optional detail panel in one component.

## Import

```tsx
import { DataPage, MenuItem } from '@cratis/components';
import { Column } from 'primereact/column';
```

## Core usage pattern

`DataPage` uses a **composable children API**. Place `<DataPage.MenuItems>` and `<DataPage.Columns>` as children:

```tsx
<DataPage
    title="Accounts"
    query={AllAccounts}
    emptyMessage="No accounts found."
>
    <DataPage.MenuItems>
        <MenuItem label="Create" icon="pi pi-plus" command={handleCreate} />
        <MenuItem label="Edit" icon="pi pi-pencil" disableOnUnselected command={handleEdit} />
    </DataPage.MenuItems>
    <DataPage.Columns>
        <Column field="name" header="Name" />
        <Column field="balance" header="Balance" />
    </DataPage.Columns>
</DataPage>
```

## Props

| Prop | Type | Required | Description |
| --- | --- | --- | --- |
| `title` | `string` | ✓ | Page title shown in the header |
| `query` | Query constructor | ✓ | Proxy-generated query class (`IQueryFor` or `IObservableQueryFor`) |
| `emptyMessage` | `string` | ✓ | Message when no rows are returned |
| `queryArguments` | `object` | | Arguments forwarded to the query |
| `dataKey` | `string` | | Unique key field for data items |
| `selection` | `T` | | Externally controlled selected item |
| `onSelectionChange` | `(e: DataTableSelectionSingleChangeEvent<T>) => void` | | Callback when the user clicks a row |
| `globalFilterFields` | `string[]` | | Fields included in global search |
| `defaultFilters` | `DataTableFilterMeta` | | Initial filter state |
| `clientFiltering` | `boolean` | | Enable client-side filtering (default: `false`) |
| `detailsComponent` | `React.FC<IDetailsComponentProps<T>>` | | Component rendered in the side panel when a row is selected |
| `onRefresh` | `() => void` | | Forwarded to `detailsComponent` to signal a data refresh |

`DataPage` detects whether `query` is a standard (`IQueryFor`) or observable (`IObservableQueryFor`) query and renders the appropriate table component automatically.

## MenuItems — `<DataPage.MenuItems>`

Wrap `<MenuItem>` elements in `<DataPage.MenuItems>` to populate the action toolbar. `MenuItem` accepts all PrimeReact `MenuItem` props plus `disableOnUnselected`:

```tsx
<DataPage.MenuItems>
    {/* Always enabled */}
    <MenuItem label="Create" icon="pi pi-plus" command={handleCreate} />

    {/* Disabled when no row is selected */}
    <MenuItem label="Edit" icon="pi pi-pencil" disableOnUnselected command={handleEdit} />
    <MenuItem label="Delete" icon="pi pi-trash" disableOnUnselected command={handleDelete} />
</DataPage.MenuItems>
```

Icons accept PrimeIcons strings (`"pi pi-plus"`) or React icon components from `react-icons`.

## Columns — `<DataPage.Columns>`

Wrap PrimeReact `<Column>` elements in `<DataPage.Columns>`:

```tsx
import { Column } from 'primereact/column';

<DataPage.Columns>
    <Column field="name" header="Name" />
    <Column field="balance" header="Balance" body={(row) => `$${row.balance.toFixed(2)}`} />
    <Column field="createdAt" header="Created" sortable />
</DataPage.Columns>
```

All standard PrimeReact `Column` props are supported (`field`, `header`, `body`, `sortable`, `filter`, `style`, etc.).

## Detail panel — `detailsComponent`

Provide a component to render in a resizable side panel when a row is selected. The panel is hidden when no row is selected.

```tsx
interface AccountDetailProps {
    item: AccountSummary;
    onRefresh?: () => void;
}

const AccountDetail = ({ item }: AccountDetailProps) => (
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

The `detailsComponent` prop takes a React function component. It receives `item` (the selected data item) and an optional `onRefresh` callback. Use `onRefresh` to signal the parent page that data should be reloaded (e.g. after a mutation in the detail panel).

## Full example

```tsx
import { useState } from 'react';
import { DataPage, MenuItem } from '@cratis/components';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { useDialog, useDialogContext, DialogResult } from '@cratis/arc.react/dialogs';
import { DataTableSelectionSingleChangeEvent } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { AllAccounts } from './queries/AllAccounts';
import { CreateAccount } from './commands/CreateAccount';

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

const AccountDetail = ({ item }: { item: AccountSummary }) => (
    <div className="p-4">
        <h2>{item.name}</h2>
        <p>Balance: {item.balance}</p>
    </div>
);

export const AccountsPage = () => {
    const [selected, setSelected] = useState<AccountSummary | undefined>();
    const [CreateDialogWrapper, showCreate] = useDialog(CreateAccountDialog);

    return (
        <>
            <DataPage
                title="Accounts"
                query={AllAccounts}
                emptyMessage="No accounts found."
                selection={selected}
                onSelectionChange={(e: DataTableSelectionSingleChangeEvent<AccountSummary>) => setSelected(e.value)}
                detailsComponent={AccountDetail}
            >
                <DataPage.MenuItems>
                    <MenuItem label="Create Account" icon="pi pi-plus" command={showCreate} />
                </DataPage.MenuItems>
                <DataPage.Columns>
                    <Column field="name" header="Account" />
                    <Column field="ownerName" header="Owner" />
                    <Column field="balance" header="Balance" body={(r) => `$${r.balance.toFixed(2)}`} />
                </DataPage.Columns>
            </DataPage>
            <CreateDialogWrapper />
        </>
    );
};
```
