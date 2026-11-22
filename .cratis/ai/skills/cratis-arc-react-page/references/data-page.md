<!-- cratis-ai-managed: skills/cratis-arc-react-page/references/data-page.md -->
# DataPage reference

## Contents

- Compound members
- `DataPageProps`
- Page landmarks
- Snapshot versus observable
- Selection reconciliation
- Search and server queries
- `MenuItemProps<TDataType>`
- `IDetailsComponentProps<TDataType>`
- `Column`
- Type name caution
- Layout
- Step 7 — Row selection and a details pane

`DataPage` is the standard list page: an action menubar, a data table, and an
optional details pane in one component. Import from
`@cratis/components/DataPage`.

```tsx
import { FaPlus } from 'react-icons/fa6';
import { Column, DataPage, MenuItem } from '@cratis/components/DataPage';
import type { IDetailsComponentProps } from '@cratis/components/DataPage';
```

## Compound members

Three static members exist:

- `DataPage.Columns` — wraps the `Column` elements.
- `DataPage.MenuItems` — wraps the `MenuItem` elements.
- `DataPage.MenuItem` — the same component as the named `MenuItem` export.

`Column` and `MenuItem` are named exports of the same subpath. There is no
`DataPage.Column`.

## `DataPageProps`

| Prop | Type | Notes |
| --- | --- | --- |
| `title` | `string` | required — menubar title |
| `landmark` | `boolean` | defaults to `true`; use `false` inside an existing main landmark |
| `query` | `Constructor<TQuery>` | required — a snapshot **or** observable query proxy |
| `emptyMessage` | `string` | required — only a real empty result |
| `loadingMessage` / `failureMessage` / `unauthorizedMessage` | `ReactNode` | query-state rows; defaults from `messages.dataTable.loading/failed/unauthorized` |
| `children` | `ReactNode` | required — `DataPage.Columns` and optionally `DataPage.MenuItems` |
| `queryArguments` | `TArguments` | forwarded to the query |
| `dataKey` | `string \| undefined` | row identity; pass whenever the read model has one |
| `selection` | `TDataType \| undefined \| null` | controlled single selection |
| `selectionMode` | `'single' \| 'multiple'` | default single |
| `selectedItems` / `onSelectedItemsChange` | `TDataType[]` / `(items: TDataType[]) => void` | multiple selection; omit `selectedItems` for internal state |
| `onSelectionChange` | `(event: DataTableSelectionChangeEvent<TDataType>) => void` | `event.value` is the row or `null`; accept it for controlled selection |
| `detailsComponent` | `React.FC<IDetailsComponentProps<TDataType>>` | rendered beside the table for the selected row |
| `globalFilterFields` | `string[] \| undefined` | enables the global search box over these fields |
| `filteredEmptyMessage` | `ReactNode` | separate copy for an active search/filter with no shown rows |
| `onGlobalFilterChange` | `(globalFilter: string) => void` | notifies every search-text change |
| `serverQuery` | `DataTableServerQuery<TArguments>` | caller maps search/filter/sort state to backend query arguments; see below |
| `defaultFilters` | `DataTableFilterMeta` | initial per-column filter state |
| `onRefresh` | `() => void` | forwarded unchanged to details; DataPage never invokes it or refetches |
| `actionsAriaLabel` | `string` | accessible name of the actions menubar, default `'Actions'` |
| `globalSearchPlaceholder` / `globalSearchAriaLabel` | `string` | search copy and accessible name |
| `paginatorClassName` / `paginatorAriaLabels` | class / label overrides | paginator presentation and localization |
| `tableClassName` | `string` | class on the inner table |
| `tablePt` | `DataTableParts` | per-part attributes on the inner data table (`tablePtOptions`/`tableUnstyled` are accepted no-ops) |
| `menubarClassName` | `string` | class on the action menubar |
| `menubarPt` | `ButtonParts` | per-part attributes on the action buttons (`menubarPtOptions`/`menubarUnstyled` are accepted no-ops) |
| `paginatorPt` | `TablePaginatorParts` | per-part attributes on the paginator |
| `clientFiltering` | `boolean` | **deprecated and a no-op** — do not use in new code |

## Page landmarks

`Page` from `@cratis/components/Common` renders its content as `<main>` by
default. `landmark={false}` switches that content region to `<div>`, for a page
already inside the application's main landmark. `DataPage` forwards its
`landmark` prop to Page. Choose the main owner and avoid nested main landmarks;
`showTitle` controls Page's heading separately, not its landmark.

## Snapshot versus observable

There is no `observableQuery` prop. `DataPage` inspects the query prototype at
render time: a snapshot query renders the snapshot table, anything else renders
the observable table. Pass an observable query proxy and the page pushes
updates automatically. There is no snapshot refresh handle on DataPage;
`onRefresh` is only a caller-owned callback forwarded to the details component.
Do not promise it will rerun the table's query.

Until the first result, query tables show a loading row, not an empty row. Error
and unauthorized results show a failure row; `empty-row` means a real empty
result. While refreshing, the table marks `aria-busy`. Style status rows with
`tablePt.loadingRow/loadingCell/failureRow/failureCell`.

## Selection reconciliation

On a ready query result, single selection follows the replacement row with the
same stringified `dataKey` value; if it disappears, `event.value` becomes `null`.
Without `dataKey`, matching is by reference. DataPage updates its internal
selection automatically; a controlled consumer must accept `event.value` in
`onSelectionChange`, otherwise its stale selection and details remain. The event
is `DataTableSelectionChangeEvent<TDataType>` (`value: TDataType | null`, optional
`originalEvent`). Do not assume the same reconciliation contract for multiple
selection.

## Search and server queries

Without `serverQuery`, filtering, global search and sorting operate only on the
loaded page, not the whole dataset. Default filtered-empty copy says that only
the loaded page was searched; `filteredEmptyMessage` overrides it.
`onGlobalFilterChange` reports each typed change.

`DataTableServerQuery<TArguments>` and `DataTableServerQueryState` are exported
from `@cratis/components/DataTables`. The state contains `globalFilter`, `filters`
and `sort`; the contract is:

```ts
interface DataTableServerQuery<TArguments> {
    mapQueryArguments: (state: DataTableServerQueryState, baseArguments: TArguments | undefined) => TArguments;
    debounceMs?: number; // defaults to 250
}
```

The caller must map that state into real backend arguments whose query implements
the search/filter/sort. With `serverQuery`, the table disables local filtering
and sorting and resets paging when effective server arguments change; it does
not invent backend search semantics. Global search text is debounced; column
filters and sorting apply immediately. The default filtered-empty copy becomes
neutral ("No rows match the search.").

## `MenuItemProps<TDataType>`

| Prop | Type | Notes |
| --- | --- | --- |
| `icon` | `React.ComponentType<{ className?: string }>` | a **component**, not an icon class string |
| `label` | `string` | menu label |
| `command` | `(selection: DataPageSelection<TDataType>) => void` | the action — **not** `onClick`; zero-argument callbacks remain valid |
| `disabled` | `boolean \| ((selection: DataPageSelection<TDataType>) => boolean)` | unconditional or selection-aware disabling |
| `disableOnUnselected` | `boolean` | disabled until a row is selected; combined with `disabled` |

`DataPageSelection<TDataType>` (exported from `@cratis/components/DataPage`) is a
readonly discriminated union: `{ selectionMode: 'single', selectedItem: TDataType | null | undefined }`
or `{ selectionMode: 'multiple', selectedItems: readonly TDataType[] }`. Both
callbacks receive the immutable, committed selection, not a click event. The
container and multiple-selection array are frozen, but row objects are not
deep-frozen. A disabled item cannot dispatch its command. JSX does not infer the row type from
DataPage's generic; supply it explicitly:

```tsx
<DataPage.MenuItem<AccountSummary>
    label='Edit'
    disabled={selection => selection.selectionMode !== 'single' || !selection.selectedItem}
    command={selection => {
        if (selection.selectionMode === 'single' && selection.selectedItem) {
            showEditAccount({ accountId: selection.selectedItem.id });
        }
    }} />
```

`MenuItem` renders nothing on its own; it is a marker that `DataPage.MenuItems`
reads. Icons are usually a small inline component:

```tsx
<MenuItem label='Add account' icon={FaPlus} command={() => showCreate()} />
```

## `IDetailsComponentProps<TDataType>`

```ts
interface IDetailsComponentProps<TDataType> {
    item: TDataType;
    onRefresh?: () => void;
}
```

```tsx
const AccountDetails = ({ item }: IDetailsComponentProps<AccountSummary>) => <div>{item.name}</div>;
```

## `Column`

`Column` is re-exported from `@cratis/components/DataPage` and defined in
`@cratis/components/DataTables`. It renders nothing itself — it is a
declaration the table reads.

| Prop | Type |
| --- | --- |
| `field` | `string` |
| `header` | `React.ReactNode` |
| `body` | `(rowData: TData) => React.ReactNode` |
| `sortable` | `boolean` |
| `filter` | `boolean` |
| `filterField` | `string` |
| `filterPlaceholder` | `string` |
| `dataType` | `'text' \| 'numeric' \| 'date' \| 'boolean'` |
| `showFilterMatchModes` | `boolean` |
| `filterElement` | `(options: ColumnFilterElementOptions) => ReactNode` |
| `filterOptions` | `{ label: string; value: unknown }[]` — list choices instead of a free-form input |
| `filterLabels` | `Partial<ColumnFilterMenuLabels>` |
| `filterPt` | `ColumnFilterMenuParts` — parts of the filter popup |
| `selectionMode` | `'single' \| 'multiple'` — selectors, including checkbox multi-selection |
| `style` / `className` | `React.CSSProperties` / `string` |
| `headerStyle` / `headerClassName` | `React.CSSProperties` / `string` |
| `bodyStyle` / `bodyClassName` | `React.CSSProperties` / `string` |

Use `body` for a computed or formatted cell:

```tsx
<Column field='balance' header='Balance' body={row => `$${row.balance.toFixed(2)}`} />
```

## Type name caution

`ColumnProps` from `@cratis/components/DataPage` is the **column declaration**
type re-exported from `DataTables`. `ColumnsProps` is also exported from the
DataPage subpath: it is the props type of the `DataPage.Columns` wrapper.

## Layout

The details pane renders in a resizable split. Give the page a bounded height —
the stories wrap `DataPage` in a container with an explicit height — or the
split has nothing to size against.

## Step 7 — Row selection and a details pane

`selection` and `onSelectionChange` are controlled. The change event carries
`value`, which is `null` when the selection is cleared.

```tsx
import type { DataTableSelectionChangeEvent } from '@cratis/components/DataTables';

const [selected, setSelected] = useState<AccountSummary | null>(null);

<DataPage
    title='Accounts'
    query={AllAccounts}
    emptyMessage='No accounts yet.'
    dataKey='id'
    selection={selected}
    onSelectionChange={(event: DataTableSelectionChangeEvent<AccountSummary>) => setSelected(event.value)}
    detailsComponent={AccountDetails}>
    <DataPage.Columns>
        <Column field='name' header='Name' />
    </DataPage.Columns>
</DataPage>
```

`detailsComponent` receives `IDetailsComponentProps<T>` — `{ item, onRefresh? }`
— and renders beside the table for the selected row.
