# Clean up stored schema annotations

Chronicle tolerates annotation differences when registering an event type. Registration does not remove
annotations already stored by older writers. Cleanup is an explicit administrative operation, not a startup
migration, and does not rewrite event content. Some .NET 11 clients in the 19.155.x series stored
`deprecated` and `contentEncoding` annotations. Upgrade the kernel before upgrading clients that emit
different annotations, then inspect existing schemas before deciding whether cleanup is necessary.

## Inspect before changing

Use the EventTypes service's `GetEventTypesWithAnnotations` query with the `EventStore` name. It returns
only affected generations, with `EventTypeId`, `Generation`, and the distinct `Keywords` found. The query
is read-only and inspects every stored generation, not just the latest.

## Normalize selected schemas

Use the EventTypes service's `NormalizeEventTypeSchemas` command with `EventStore` and optionally
`EventTypeIds`. A missing or empty identifier list selects all event types in that store. Use a nonempty
list for a scoped cleanup. The command follows the event-store validation restrictions of event type
creation; the system store is not a valid normalization target.

For example, from a Chronicle checkout, use `grpcurl` with the generated descriptor set to inspect the
`orders` store and normalize only `OrderPlaced`. Supply a valid access token through your approved
credential mechanism as `CHRONICLE_ACCESS_TOKEN`; do not paste it into shell history. These commands
target a local development kernel with a self-signed certificate. For a deployed kernel, use its trusted
TLS endpoint and omit `-insecure`.

```sh
grpcurl -insecure -protoset Source/Kernel/Protobuf/chronicle.desc \
  -H "Authorization: Bearer ${CHRONICLE_ACCESS_TOKEN}" \
  -d '{"EventStore":"orders"}' localhost:35000 \
  Cratis.Chronicle.Contracts.EventTypes.EventTypes/GetEventTypesWithAnnotations

grpcurl -insecure -protoset Source/Kernel/Protobuf/chronicle.desc \
  -H "Authorization: Bearer ${CHRONICLE_ACCESS_TOKEN}" \
  -d '{"EventStore":"orders","EventTypeIds":["OrderPlaced"]}' localhost:35000 \
  Cratis.Chronicle.Contracts.EventTypes.EventTypes/NormalizeEventTypeSchemas
```

The query uses the same store-read permissions as other event-type inspection. Normalization uses the
same store-management permissions as event-type creation. Inspect `IsAuthorized`, validation results,
and exception messages before treating either call as successful. No dedicated CLI command or
Workbench cleanup view is provided by this change.

The command response's `Response.Changed` collection lists only generations actually rewritten and the keywords
removed. Run the query again to confirm the result. Repeating the command without intervening changes
returns an empty `Changed` collection and does not evict caches again.

Cleanup removes `deprecated`, `contentEncoding`, `examples`, and `$comment` only from schema declarations.
It retains `title` and `description`, validation constraints, and arbitrary JSON values inside `default`,
`const`, and `enum`. Property names that happen to match annotation keywords are retained.

Existing generations are replaced in place. The command does not create types or generations, change
ownership, visibility, origin, migrations, or migration versions, or append events. Changed types are
invalidated in storage and across silo caches. A failure is propagated; earlier replacements may already
have completed, so inspect and retry after resolving the failure. Coordinate cleanup with schema edits
and registrations to avoid competing updates.
