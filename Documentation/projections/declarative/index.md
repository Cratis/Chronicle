# Projections

Declarative Projections in Cratis allow you to create read models from events stored in the event log. They provide different levels of complexity from simple auto-mapping to sophisticated hierarchical models with joins.

## Projection recipes

| Recipe | Description |
| ------ | ----------- |
| [Simple projection](simple-projection.md) | Basic projection using AutoMap() |
| [AutoMap](auto-map.md) | Automatic property mapping at different levels |
| [Passive](passive.md) | In-memory projections for on-demand lookups |
| [Set properties](set-properties.md) | Explicit property mapping and transformations |
| [Children](children.md) | Hierarchical data models with child collections |
| [Joins](joins.md) | Cross-stream projections using joins |
| [Functions](functions.md) | Mathematical operations and calculations |
| [Composite keys](composite-keys.md) | Multi-property key identification |
| [Event context](event-context.md) | Using event metadata in projections |
| [FromEvery](from-every.md) | Setting properties for all events in a projection |
| [Initial values](initial-values.md) | Default values for read model properties |
| [RemoveWithJoin](remove-with-join.md) | Cross-stream child removal |
| [FromEventSequence](from-event-sequence.md) | Sourcing events from specific event sequences |
| [NotRewindable](not-rewindable.md) | Forward-only projections that cannot be replayed |

## Key concepts

### Auto-mapping vs explicit mapping

- **Auto-mapping**: Automatically maps properties with matching names between events and read models
  - Use `.AutoMap()` in fluent projections (`IProjectionFor<T>`)
  - Use `[FromEvent<TEvent>]` in model-bound projections for the same functionality
- **Explicit mapping**: Gives you full control over property transformations and mappings
  - Use `.Set()`, `.Add()`, etc. in fluent projections
  - Use `[SetFrom<TEvent>]`, `[AddFrom<TEvent>]`, etc. in model-bound projections

### Event handling

- Projections can handle multiple event types
- Each event type can have its own property mappings
- Properties are updated incrementally as events are processed

### Keys and identification

- **EventSourceId** is used as the default key for both read models and parent identification in child collections
- **Child identifiers**: Use `.IdentifiedBy()` to specify how child items are uniquely identified within collections
- **Parent key resolution**:
  - By default, the EventSourceId is used to identify the parent when adding children
  - Use `.UsingParentKey(e => e.Property)` when the parent identifier is in the event content
  - Use `.UsingParentKeyFromContext(ctx => ctx.EventSourceId)` to explicitly document default behavior
- **Child key specification**: Use `.UsingKey(e => e.Property)` to extract the child identifier from event content
- **Joins**: Use keys to link data from different event streams using `.On(m => m.Property)`

### Performance

- Projections are automatically maintained as events are appended
- The system handles indexing and query optimization
- Consider join complexity and update frequency when designing projections
