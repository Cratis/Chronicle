# ObserverDetails

Composes the detail panel shown next to the observers table when a row is selected. Renders two tabs: a flat key/value **Summary** of the observer and an **Event Types** list.

## Component Hierarchy

```
ObserverDetails             ← composition root, owns the TabView
├── ObserverSummary         ← labelled key/value pairs in a DataTable
└── ObserverEventTypes      ← event types DataTable (id, generation)
```

## Architecture Decisions

- **Single-row props.** Every component receives the same `ObserverInformation` and reads only the fields it needs. The selection lives in `ObserversViewModel` — none of these components own state.
- **Sub-component per tab.** Splitting the two tabs into their own files keeps each focused on one rendering concern and keeps the composition root readable.

## State Management

- No local state.
- Parent (`Observers`) owns row selection; `ObserverDetails` receives the resolved observer as a prop.

## CSS Conventions

- Each component has its own CSS file (`*.css`) co-located in this folder.
- BEM-like class names prefixed with the component name (`observer-details__tab-content`, `observer-summary__label`, etc).
- Colors and surfaces come from PrimeReact CSS variables (`var(--text-color-secondary)`).

## How to Extend

- **Add a new tab:** create a new sub-component (e.g. `ObserverFailedPartitions.tsx`) with its own CSS, then add a `TabPanel` referencing it from `ObserverDetails.tsx`.
- **Change the summary fields:** edit the `summaryRows` array in `ObserverSummary.tsx`. Translations live under `eventStore.namespaces.observers.details.summary` in `translation.json`.
