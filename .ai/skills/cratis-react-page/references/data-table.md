# Data Tables — Reference

Use standalone data table components when you need a table without the built-in `DataPage` full-page chrome (e.g., embedded inside another panel or card).

## DataTableForQuery

```tsx
import { DataTableForQuery } from '@cratis/components';
import { AllAccounts } from './queries/AllAccounts';

<DataTableForQuery
    query={AllAccounts}
    columns={[
        { header: 'Name', field: 'name' },
        { header: 'Balance', field: 'balance' },
    ]}
    onRowSelected={(row) => setSelected(row)}
/>
```

## DataTableForObservableQuery

```tsx
import { DataTableForObservableQuery } from '@cratis/components';
import { AllAccountsLive } from './queries/AllAccountsLive';

<DataTableForObservableQuery
    query={AllAccountsLive}
    columns={...}
    onRowSelected={(row) => setSelected(row)}
/>
```

## Shared props

| Prop | Type | Description |
| --- | --- | --- |
| `query` / `query` | Query class | Proxy query (use the appropriate component for type) |
| `columns` | `Column<T>[]` | Column definitions (same shape as DataPage) |
| `onRowSelected` | `(row: T) => void` | Row click callback |
| `selectedRow` | `T \| undefined` | Externally controlled selected row |
| `noDataMessage` | `string` | Message when no rows are returned |
| `queryArgs` | `object` | Arguments forwarded to the query |

## Column definition

```ts
type Column<T> = {
    header: string;
    field: keyof T | ((row: T) => string);
    width?: number | string;
};
```

## When to use each component

| Situation | Component |
| --- | --- |
| Full page with toolbar | `DataPage` |
| Embedded table, standard query | `DataTableForQuery` |
| Embedded table, real-time push | `DataTableForObservableQuery` |
| Inline data (no query) | Custom table (out of scope) |
