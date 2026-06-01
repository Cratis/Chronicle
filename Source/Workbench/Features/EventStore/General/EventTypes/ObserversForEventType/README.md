# ObserversForEventType

Lists every observer across all namespaces of the current event store that consumes the given event type. Mounted as a tab inside the event-type detail view.

## Component Hierarchy

```
ObserversForEventType             ← subscribes to the reactive query
└── ObserversForEventTypeTable    ← renders the resulting rows in a DataTable
```

## Architecture Decisions

- **Reactive query.** Uses `ObserveObserversForEventType` (an `ISubject<...>` backed observable query) so the table re-renders automatically when observers in any namespace change. No manual refetch required.
- **Data fetch separated from rendering.** `ObserversForEventType` owns the query subscription; `ObserversForEventTypeTable` is a pure renderer that accepts an already-fetched collection. This keeps the table easy to test in isolation.

## State Management

- No local state. The composition root reads the reactive query result via `.use()`.
- The route parameter `params.eventStore` provides the event store; `eventTypeId` comes from props.

## CSS Conventions

- BEM-like class names prefixed with the component name (`observers-for-event-type-table`).
- Colors and surfaces come from PrimeReact CSS variables; no hard-coded colors.

## How to Extend

- **Add a column:** edit `ObserversForEventTypeTable.tsx` and add a `<Column>`. Add translations under `eventStore.general.eventTypes.observers.columns` in `translation.json`.
- **Filter / sort defaults:** add a `filters` prop to `ObserversForEventTypeTable` or wrap the table in a higher-level component that prepares filter state.
