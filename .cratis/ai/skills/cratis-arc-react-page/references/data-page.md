<!-- cratis-ai-managed: skills/cratis-arc-react-page/references/data-page.md -->
# DataPage reference

`DataPage` is the standard list page: an action menubar, a data table, and an
optional details pane in one component. Import from
`@cratis/components/DataPage`.

```tsx
import { Column, DataPage, MenuItem } from '@cratis/components/DataPage';
import type { IDetailsComponentProps } from '@cratis/components/DataPage';
```

## Compound members

Exactly two static members exist:

- `DataPage.Columns` — wraps the `Column` elements.
- `DataPage.MenuItems` — wraps the `MenuItem` elements.

`Column` and `MenuItem` are named exports of the same subpath. There is no
`DataPage.Column` and no `DataPage.MenuItem`.

## `DataPageProps`

| Prop | Type | Notes |
| --- | --- | --- |
| `title` | `string` | required — menubar title |
| `query` | `Constructor<TQuery>` | required — a snapshot **or** observable query proxy |
| `emptyMessage` | `string` | required — shown when the query returns no rows |
| `children` | `ReactNode` | required — `DataPage.Columns` and optionally `DataPage.MenuItems` |
| `queryArguments` | `TArguments` | forwarded to the query |
| `dataKey` | `string \| undefined` | row identity; pass whenever the read model has one |
| `selection` | `any \| undefined \| null` | controlled selection |
| `onSelectionChange` | `(event: DataTableSelectionChangeEvent<any>) => void` | `event.value` is the row or `null` |
| `detailsComponent` | `React.FC<IDetailsComponentProps<any>>` | rendered beside the table for the selected row |
| `globalFilterFields` | `string[] \| undefined` | enables the global search box over these fields |
| `defaultFilters` | `DataTableFilterMeta` | initial per-column filter state |
| `onRefresh` | `() => void` | invoked to re-fetch a snapshot query |
| `actionsAriaLabel` | `string` | accessible name of the actions menubar, default `'Actions'` |
| `tableClassName` | `string` | class on the inner table |
| `tablePt` / `tablePtOptions` / `tableUnstyled` | PrimeReact pass-through | target the inner data table |
| `menubarClassName` | `string` | class on the action menubar |
| `menubarPt` / `menubarPtOptions` / `menubarUnstyled` | PrimeReact pass-through | target the action buttons |
| `clientFiltering` | `boolean` | **deprecated and a no-op** — do not use in new code |

## Snapshot versus observable

There is no `observableQuery` prop. `DataPage` inspects the query prototype at
render time: a snapshot query renders the snapshot table, anything else renders
the observable table. Pass an observable query proxy and the page pushes
updates automatically; pass a snapshot query and re-fetch through `onRefresh`
after a command succeeds.

## `MenuItemProps`

| Prop | Type | Notes |
| --- | --- | --- |
| `icon` | `React.ComponentType<{ className?: string }>` | a **component**, not an icon class string |
| `label` | `string` | menu label |
| `command` | `() => void` | the action — **not** `onClick` |
| `disabled` | `boolean` | unconditionally disabled |
| `disableOnUnselected` | `boolean` | disabled until a row is selected |

`MenuItem` renders nothing on its own; it is a marker that `DataPage.MenuItems`
reads. Icons are usually a small inline component:

```tsx
<MenuItem label='Add account' icon={() => <i className='pi pi-plus' />} command={() => showCreate()} />
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
| `filterLabels` | `Partial<ColumnFilterMenuLabels>` |
| `selectionMode` | `'single' \| 'multiple'` |
| `style` / `className` | `React.CSSProperties` / `string` |
| `headerStyle` / `headerClassName` | `React.CSSProperties` / `string` |
| `bodyStyle` / `bodyClassName` | `React.CSSProperties` / `string` |

Use `body` for a computed or formatted cell:

```tsx
<Column field='balance' header='Balance' body={row => `$${row.balance.toFixed(2)}`} />
```

## Type name caution

`@cratis/components/DataPage` exports two different things called
`ColumnProps` — the props of the `DataPage.Columns` wrapper and the re-exported
column declaration props. Import `ColumnProps` from
`@cratis/components/DataTables` when you need the column type, so the reference
is unambiguous.

## Layout

The details pane renders in a resizable split. Give the page a bounded height —
the stories wrap `DataPage` in a container with an explicit height — or the
split has nothing to size against.
