<!-- cratis-ai-managed: skills/cratis-components-data-surfaces/references/query-table-updates.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Query tables: server state, selection and pagination

Requires Components 4.35.1 for all table features described here, including serverQuery, filteredEmptyMessage, pageLinks and refreshed selection; check `yarn why @cratis/components`, or retain the older table API and host-owned filtering, pagination and selection reconciliation.

## Server sorting and filtering are opt-in

`DataTableForQuery`, `DataTableForObservableQuery` and `DataPage` accept
`serverQuery`. Without it, sorting and filtering affect only the
loaded page, not the complete result set.

- Supply `toArguments` to translate search text and column filters to query
  arguments. The backend must apply those arguments itself.
- Sorting uses Arc `sortBy`/`sortDirection`; `sortFields` maps column names to
  server fields when necessary.
- Return `IQueryable` or use MongoDB `Observe` so sorting precedes paging.
- State changes return to page one; search is debounced by 300 ms by default.
  Rows are rendered as the server returns them, not filtered a second time.

Do not present a page-local search as a complete-dataset search.
`filteredEmptyMessage` replaces `emptyMessage` when a search/filter has no rows.
Provider messages distinguish `filteredEmpty` (this page) from
`filteredEmptyServer` (server result). `onGlobalFilterChange` reports search text
as the user types; it does not implement backend search.

## Query state and selection

With Arc and Arc React 22.122 or later, changing bound query arguments shows
loading instead of the previous arguments' rows, failure or access-denied state.
Returning to cached arguments avoids a loading flash. A completed
unsuccessful result is a failure, not an empty success; unauthorized stays access
denied. The query-bound chat sidebar also distinguishes these outcomes.

Bound tables and `DataPage` reconcile selection against refreshed
rows by `dataKey`, clearing it when the selected row disappears. A controlled
selection is reported through `onSelectionChange`, never forcibly overridden.
Use stable keys and exercise argument changes and removed selections in specs.

## Pagination and localization

`pageLinks` is off by default. Set it to `true` or `{ siblings, boundaries }` on
`TablePaginator`, a bound table or `DataPage` for numbered links with ellipses.
The current button has `aria-current="page"`; the old page-count info is hidden.
The `pages`, `page`, `ellipsis` parts support styling; `page(n)` and `pages`
messages name the controls. Provider paginator messages or
`ariaLabels.range(start, end, total)` localize the range text too.

For a custom pager, `paginatorPages` and `PaginatorPageItem` export the same
windowing logic. Do not hard-code English accessible names in a localized host.
