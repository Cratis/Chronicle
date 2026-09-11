<!-- cratis-ai-managed: skills/cratis-arc-react-page/references/data-tables.md -->
# Data tables reference

Use a standalone table when you need query-backed rows **without** the
`DataPage` chrome — embedded in a panel, a card, or a dialog. Import from
`@cratis/components/DataTables`.

```tsx
import {
    Column,
    DataTableForObservableQuery,
    DataTableForQuery,
} from '@cratis/components/DataTables';
import type { DataTableSelectionChangeEvent } from '@cratis/components/DataTables';
```

## Pick the component by query kind

| Situation | Component |
| --- | --- |
| Full list page with menubar and details | `DataPage` |
| Embedded table, snapshot query | `DataTableForQuery` |
| Embedded table, observable query | `DataTableForObservableQuery` |

Unlike `DataPage`, these do **not** auto-detect — pass a snapshot query to
`DataTableForQuery` and an observable query to `DataTableForObservableQuery`.

## Shared props

Both components take the same prop names:

| Prop | Type | Notes |
| --- | --- | --- |
| `query` | `Constructor<TQuery>` | required |
| `emptyMessage` | `string` | required |
| `children` | `ReactNode` | the `Column` declarations |
| `queryArguments` | `TArguments` | forwarded to the query |
| `dataKey` | `string \| undefined` | row identity |
| `selection` | `TDataType \| undefined \| null` | controlled selection |
| `onSelectionChange` | `(event: DataTableSelectionChangeEvent<TDataType>) => void` | `event.value` is the row or `null` |
| `globalFilterFields` | `string[] \| undefined` | enables the search box |
| `defaultFilters` | `DataTableFilterMeta` | initial filter state |
| `className` | `string` | |
| `pt` / `ptOptions` / `unstyled` | PrimeReact pass-through | |
| `paginatorClassName` | `string` | |
| `paginatorAriaLabels` | paginator label overrides | localize the paginator |
| `clientFiltering` | `boolean` | **deprecated and a no-op** |

```tsx
<DataTableForQuery<AllAccounts, AccountSummary, object>
    query={AllAccounts}
    emptyMessage='No accounts found'
    dataKey='id'
    globalFilterFields={['name', 'ownerName']}
    selection={selected}
    onSelectionChange={event => setSelected(event.value)}>
    <Column field='name' header='Name' sortable filter filterPlaceholder='Filter by name' />
    <Column field='ownerName' header='Owner' />
</DataTableForQuery>
```

## Paging

Both tables page server-side through the Arc paging hooks with a fixed page
size of 20 rows starting at page 0. The paginator renders only when the result
reports more than one page. There is no page-size prop on these components —
when you need caller-controlled paging, call the query's paging hook yourself
(see [queries-and-commands.md](queries-and-commands.md)) and render the rows
with your own layout.

## Selection event

```ts
interface DataTableSelectionChangeEvent<TData> {
    value: TData | null;
    originalEvent?: SyntheticEvent;
}
```

Add a selection column with `<Column selectionMode='single' headerStyle={{ width: '3rem' }} />`
when you want an explicit selector rather than row click selection.

## Column filtering

Set `filter` on a column to get a per-column filter menu. `dataType` picks the
match modes (`'text'`, `'numeric'`, `'date'`, `'boolean'`; default `'text'`),
`showFilterMatchModes={false}` hides the mode picker, and `filterElement`
replaces the editor entirely:

```tsx
<Column
    field='rating'
    header='Rating'
    filter
    filterElement={options => (
        <RatingEditor value={options.value} onChange={value => options.onChange(value)} onApply={options.onApply} />
    )} />
```

`ColumnFilterElementOptions` gives `field`, `value`, `matchMode`, `onChange`,
`onApply`, and `onClear`.

Filter menu strings are localizable per column with `filterLabels`
(`filterTriggerAriaLabel`, `clear`, `apply`, `true`, `false`) — never ship a
hard-coded English accessible name.

## Filter state shape

```ts
type DataTableFilterConstraint = { value: unknown; matchMode?: DataTableFilterMatchMode };
type DataTableFilterMeta = Record<string, DataTableFilterConstraint>;
```

`DataTableFilterMatchMode` is a const object, not an enum:
`StartsWith`, `Contains`, `NotContains`, `EndsWith`, `Equals`, `NotEquals`,
`In`, `Between`, `LessThan`, `LessThanOrEqual`, `GreaterThan`,
`GreaterThanOrEqual`, `DateIs`, `DateIsNot`, `DateBefore`, `DateAfter`.

## Custom match modes

Register a matcher to add a domain-specific mode, and unregister it when the
owner unmounts:

```ts
import { registerDataTableFilterMatcher } from '@cratis/components/DataTables';

const registration = registerDataTableFilterMatcher(
    'startsWithAccountPrefix',
    (value, filter) => String(value).startsWith(String(filter)));

// later
registration.unregister();
```

Registration rejects names that collide with a built-in mode, names that are
not identifier-like, and prototype-polluting names. Re-registering the same
name with a *different* function throws — register once per matcher.
