# DataPage — Reference

`DataPage` (from `@cratis/components`) is the standard full-page layout providing a menubar, data table, and optional detail panel in one component.

## Import

```tsx
import { DataPage, MenuItemGroup, MenuItem, Column } from '@cratis/components';
```

## Core props

| Prop | Type | Description |
| --- | --- | --- |
| `query` | Query class | Proxy-generated query class (standard) |
| `observableQuery` | Observable query class | Proxy-generated observable query class (real-time) |
| `columns` | `Column<T>[]` | Column definitions (see below) |
| `menuItems` | `ReactNode` | Toolbar content (usually `<MenuItemGroup>`) |
| `detailPanel` | `(row: T) => ReactNode` | Renders to the right when a row is selected |
| `onRowSelected` | `(row: T) => void` | Callback when user clicks a row |
| `noDataMessage` | `string` | Message when the query returns no rows |
| `queryArgs` | `object` | Arguments forwarded to the query proxy |

Use either `query` or `observableQuery` — not both.

## Column definition

```tsx
type Column<T> = {
    header: string;
    field: keyof T | ((row: T) => string);
    width?: number | string;
    sortable?: boolean;
};
```

Example with custom renderer:

```tsx
columns={[
    { header: 'Name', field: 'name' },
    { header: 'Balance', field: (row) => row.balance.toFixed(2) },
]}
```

## MenuItemGroup / MenuItem

```tsx
<MenuItemGroup>
    <MenuItem label="Add" onClick={openCreate} />
    <MenuItem label="Delete" onClick={openDelete} disabled={!selectedRow} />
</MenuItemGroup>
```

Multiple `MenuItemGroup` children create visual separators between groups.

## Detail panel

The detail panel receives the currently selected row. It is hidden when no row is selected.

```tsx
<DataPage
    query={AllAccounts}
    columns={...}
    detailPanel={(account) => (
        <AccountDetail accountId={account.id} />
    )}
/>
```

## Full example

```tsx
import { useDialog } from '@cratis/arc.react/dialogs';
import { CreateAccountDialog } from './CreateAccountDialog';

export const AccountsPage = () => {
    const [CreateAccountWrapper, showCreateAccount] = useDialog(CreateAccountDialog);

    return (
        <>
            <DataPage
                query={AllAccounts}
                columns={[
                    { header: 'Account', field: 'name' },
                    { header: 'Owner', field: 'ownerName' },
                    { header: 'Balance', field: (r) => `$${r.balance.toFixed(2)}` },
                ]}
                menuItems={
                    <MenuItemGroup>
                        <MenuItem label="Create Account" onClick={() => showCreateAccount()} />
                    </MenuItemGroup>
                }
                detailPanel={(row) => <AccountDetail account={row} />}
                noDataMessage="No accounts found."
            />
            <CreateAccountWrapper />
        </>
    );
};
```

`CreateAccountDialog` is a separate component that receives `closeDialog` via `DialogProps` and renders a `CommandDialog`. See `dialogs.md` for the full dialog pattern.
