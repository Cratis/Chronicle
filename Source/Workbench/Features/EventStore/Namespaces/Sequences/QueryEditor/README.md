# Query editor

The event sequence workspace. A user builds queries over an event sequence, keeps several open at
once, and finds them again later — their own, or ones somebody shared with everyone.

## Component hierarchy

```
Sequences                     page: query hierarchy | open query tabs, split by Allotment
├── QueryHierarchy            the tree of scopes, folders and saved queries
│   └── QueryTreeNode         one row, with its inline actions and in-place rename
├── QueryTabHeader            the name on a tab, renamed in place by double-clicking it
└── QueryEditor               one open query (one tab)
    ├── Menubar               the toolbar every list page uses — Save first, then Run and the rest
    ├── Dropdown              which event sequence the query runs against
    ├── QueryFilterBar        the filter dropdown, built on @cratis/components/Filter
    │   └── OccurredRangeFilter   time range picked over a server-counted histogram
    ├── EventsTable           the matching events, paged and ordered by the backend
    └── EventDetails          the selected event, in a resizable pane
```

## Architecture decisions

**Plain React, no MVVM.** Everything stateful lives either in a hook (`useOpenQueries`,
`useQueryHierarchyActions`) or in a pure module (`SequenceQueryState`, `OpenQuery`, `queryFilters`,
`toQueryArguments`, `histogramResolution`, `buildQueryTree`, `folderNaming`). The pure modules hold
the logic worth specifying and are covered by the `for_*` folders here; the components are wiring.

**One state shape, persisted as-is.** `SequenceQueryState` mirrors the saved query, so saving is a
straight projection rather than a reconciliation between an "editing" and a "saved" copy.

**Saving is deliberate.** An open query keeps the state it was last saved as (`OpenQuery.saved`)
alongside the state being edited, and `hasUnsavedChanges` compares the two. The first save asks
where the query should live — its name, whether it is visible to only the user or to everyone, and
which folder it goes in; every save after that writes it back where it already is.

**The name is edited where it is shown.** Double-clicking the node in the hierarchy or the name on
the tab renames it, so no query has to give up room to a name field. Renaming from the tree writes
back immediately, because the tree only knows queries that exist; renaming on the tab is an edit
like any other and waits for Save.

**Filters go straight into query state.** There is no separate applied-filters copy to keep in step;
the state the user edits *is* the state that gets persisted.

**Results refresh when the user is done choosing, not on every keystroke.** Sorting a column,
changing the event sequence, and closing the filter panel after changing something all re-run the
query immediately — those are decisions, not typing. Editing a filter *inside* the panel does not,
which is what Run is for. The results table is keyed on a run counter so running remounts it with
the arguments captured at that moment.

**Sorting is Arc's, and server-side.** The order travels on the query itself (`Sorting` on the
client, `QueryContext.Sorting` on the server) rather than as arguments of our own, and is applied in
storage — so paging through a sorted query walks the whole matching set rather than re-sorting each
page. `EventsTable` drives the PrimeReact table directly because the published `DataTableForQuery`
does not pass `onSort` through; fold it back once that component grows
`onSort`/`sortField`/`sortOrder`.

**Export is produced by the server.** The browser only ever holds one page, so it has nothing to
export from; `ExportEvents` returns everything the criteria matches, unpaged, and the browser saves
it as a file.

**What was open is picked up again.** The identifiers of the saved queries in the tabs live in local
storage per namespace (`rememberedQueries`); anything deleted in the meantime simply drops out on
the way back in. Unsaved tabs are deliberately not remembered — there is nothing on the server to
restore them from. The hierarchy's width is remembered the same way (`useHierarchyWidth`) and starts
at its narrowest, so the results get the room until the user says otherwise.

**Two things reach outside their own markup**, both because PrimeReact and the shared filter panel
own the elements involved. The tab's double-click-to-rename listens on the whole tab link rather
than the label, which only covers a sliver of it; and the filter panel, which portals to the
document body and positions itself from its trigger's *left* edge, is pulled back on screen by a
class on the body while it is open. Both are commented where they happen.

**The histogram is counted by the backend.** `SequenceHistogram` aggregates over the whole sequence,
not the loaded page, so the shape the user drags over is the real distribution. It is fetched twice:
once coarsely to learn the total span, then at the resolution that span deserves.

**The time range is excluded from its own histogram.** `toHistogramArguments` applies every filter
*except* the occurred bounds — narrowing the histogram by the range already picked would collapse it
to the selection and make the rest of the span unreachable.

**Folders are stored in their own right.** A folder that holds nothing has no query to be inferred
from, and creating one before deciding what goes in it is the normal order of doing things — so
`QueryFolder` is its own read model with its own save and delete commands. The tree still shows
folders implied by the paths queries carry, because a query can be filed into a folder name typed
straight into the save dialog. Renaming a folder writes back every stored folder *and* every query
at or below the old path; deleting one takes everything beneath it, which is why both deletions ask
first and the folder prompt says how many queries go with it.

## Reuse

The filter dropdown is `FilterPanel` from `@cratis/components/Filter` — the same component the
PivotViewer uses, marked with the same funnel (`FilterIcon`) and offering the same dimensions:
event type, event source, event source type, event stream type, correlation, tags and time. Only the
event type is a choice from a known set; the rest are values the server matches on and are rendered
through `FilterEditor` custom slots, because enumerating them would mean reading the whole sequence.

The toolbar is the PrimeReact `Menubar` that `DataPage` puts on every list page, and the results are
a `DataTableForQuery` with an `Allotment` details pane — `DataPage` itself renders a whole `Page`,
which a tab cannot.

> Once `@cratis/components` ships `numericRange.histogram` (pre-counted buckets on a range filter),
> `OccurredRangeFilter` can be dropped in favour of the library's own `RangeHistogramFilter`.

## CSS conventions

One `.css` file per component, named for it, with class names prefixed by the component
(`.query-editor__toolbar`, `.query-tree-node__row`). Colors come from PrimeReact tokens
(`var(--surface-*)`, `var(--primary-color)`, `var(--text-color)`) so themes and dark mode work.

## How to extend

- **A new filter dimension**: add a key and a group in `queryFilters.ts`, a field on
  `SequenceQueryState`, and map it in `toQueryArguments.ts` — then extend `SequenceQueryFilter` and
  `EventSequenceQueryCriteria` on the backend to match, including every storage provider. Add it to
  `areSequenceQueryStatesEqual` too, or edits to it will neither count as unsaved nor be saved.
- **A new column**: add a `<Column>` in `EventsTable.tsx`. To make it sortable, add the field to
  `EventSequenceQuerySortBy` and to the order switch in all three storage providers — sorting only
  works on values stored alongside the event itself.
- **Anything static on a `[ReadModel]`**: remember every static method is published as a query. A
  conversion helper belongs in `SequenceQueryConverters`, not on the read model, or it becomes an
  endpoint and a generated proxy of its own.
