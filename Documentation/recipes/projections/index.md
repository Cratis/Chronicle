# Projections

Projections in Cratis allow you to create read models from events stored in the event log. They provide different levels of complexity from simple auto-mapping to sophisticated hierarchical models with joins.

## Projection recipes

| Recipe | Description |
| ------ | ----------- |
| [Simple projection](simple-projection.md) | Basic projection using AutoMap() |
| [Projection with properties](projection-with-properties.md) | Explicit property mapping and transformations |
| [Projection with children](projection-with-children.md) | Hierarchical data models with child collections |
| [Projection with joins](projection-with-joins.md) | Cross-stream projections using joins |
| [Projection functions](projection-functions.md) | Mathematical operations and calculations |
| [Projection composite keys](projection-composite-keys.md) | Multi-property key identification |
| [Projection with event context](projection-event-context.md) | Using event metadata in projections |
| [Projection with FromEvery](projection-from-every.md) | Setting properties for all events in a projection |
| [Projection with initial values](projection-initial-values.md) | Default values for read model properties |
| [Projection with RemoveWithJoin](projection-remove-with-join.md) | Cross-stream child removal |
| [Passive projection](projection-passive.md) | In-memory projections for on-demand lookups |
| [Projection with FromEventSequence](projection-from-event-sequence.md) | Sourcing events from specific event sequences |
| [Projection with NotRewindable](projection-not-rewindable.md) | Forward-only projections that cannot be replayed |

## Key concepts

### Auto-mapping vs explicit mapping

- **Auto-mapping**: Automatically maps properties with matching names between events and read models
- **Explicit mapping**: Gives you full control over property transformations and mappings

### Event handling

- Projections can handle multiple event types
- Each event type can have its own property mappings
- Properties are updated incrementally as events are processed

### Keys and identification

- Event source ID is used as the default key for read models
- Child collections use custom identifiers for individual items
- Joins use keys to link data from different streams

### Performance

- Projections are automatically maintained as events are appended
- The system handles indexing and query optimization
- Consider join complexity and update frequency when designing projections
