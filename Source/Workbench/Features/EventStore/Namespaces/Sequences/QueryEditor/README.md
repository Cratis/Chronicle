# Query editor

The event sequence workspace. A user builds queries over an event sequence, keeps several open at
once, and finds them again later — their own, or ones somebody shared with everyone.

## Component hierarchy

```
Sequences                     page: saved-query list + open query tabs
├── SavedQueries              the queries the user can reopen or delete
└── QueryEditor               one open query (one tab)
    ├── QueryFilterBar        the filter dropdown, built on @cratis/components/Filter
    │   └── OccurredRangeFilter   time range picked over a server-counted histogram
    └── DataPage              the matching events, paged and ordered by the backend
```

## Architecture decisions

**Plain React, no MVVM.** Everything stateful lives either in a hook (`useOpenQueries`,
`useQueryAutoSave`) or in a pure module (`SequenceQueryState`, `queryFilters`, `toQueryArguments`,
`histogramResolution`). The pure modules hold the logic worth specifying and are covered by the
`for_*` folders here; the components are wiring.

**One state shape, persisted as-is.** `SequenceQueryState` mirrors the saved query, so saving is a
straight projection rather than a reconciliation between an "editing" and a "saved" copy.

**Filters go straight into query state.** There is no separate applied-filters copy to keep in step;
the state the user edits *is* the state that gets persisted.

**Results refresh on demand, filters save continuously.** Editing schedules a save (debounced, see
`useQueryAutoSave`) but does not re-run the query — a query over a large sequence is worth running
deliberately, which is what the Run action is for. The results table is keyed on a run counter so
running remounts it with the arguments captured at that moment.

**The histogram is counted by the backend.** `SequenceHistogram` aggregates over the whole sequence,
not the loaded page, so the shape the user drags over is the real distribution. It is fetched twice:
once coarsely to learn the total span, then at the resolution that span deserves.

**The time range is excluded from its own histogram.** `toHistogramArguments` applies every filter
*except* the occurred bounds — narrowing the histogram by the range already picked would collapse it
to the selection and make the rest of the span unreachable.

## Reuse

The filter dropdown is `FilterPanel` from `@cratis/components/Filter` — the same component the
PivotViewer uses. Event types are a normal searchable multi-select group; the event source and time
range are `FilterEditor` custom slots, because neither is a choice from a fixed set of options.

> Once `@cratis/components` ships `numericRange.histogram` (pre-counted buckets on a range filter),
> `OccurredRangeFilter` can be dropped in favour of the library's own `RangeHistogramFilter`.

## CSS conventions

One `.css` file per component, named for it, with class names prefixed by the component
(`.query-editor__toolbar`, `.saved-queries__item`). Colors come from PrimeReact tokens
(`var(--surface-*)`, `var(--primary-color)`, `var(--text-color)`) so themes and dark mode work.

## How to extend

- **A new filter dimension**: add a key and a group in `queryFilters.ts`, a field on
  `SequenceQueryState`, and map it in `toQueryArguments.ts` — then extend the backend criteria to
  match. Add it to `areSequenceQueryStatesEqual` too, or edits to it will not be saved.
- **A new column**: add a `<Column>` in `QueryEditor.tsx`. Server-side sorting is by sequence
  number only; sorting on anything else would need backend support.
