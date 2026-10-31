// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Releases stored constraint inputs with the same subject and schema as reindexing.
/// </summary>
/// <param name="eventStore">The event store.</param>
/// <param name="namespace">The namespace.</param>
/// <param name="eventTypes">The schemas to read.</param>
/// <param name="metadata">The compliance metadata manager.</param>
/// <param name="converter">The content converter.</param>
public class StoredConstraintEventContent(EventStoreName eventStore, EventStoreNamespaceName @namespace, IEventTypesStorage eventTypes, IJsonSchemaMetadataManager metadata, IExpandoObjectConverter converter)
{
    readonly Dictionary<EventType, EventTypeSchema> _schemas = [];

    /// <summary>
    /// Identifies erased subject inputs that cannot produce a readable claim.
    /// </summary>
    /// <param name="definition">The unique declaration.</param>
    /// <param name="context">The released event context.</param>
    /// <returns>Whether reindexing and replay should skip the claim.</returns>
    public static bool IsErased(UniqueConstraintDefinition definition, ConstraintValidationContext context) =>
        definition.SupportsEventType(context.EventTypeId) && !definition.RemovedWith.Contains(context.EventTypeId) &&
        definition.GetPropertiesAndValues(context).Any(property => string.IsNullOrEmpty(property.Value));

    /// <summary>
    /// Releases personal data before constraint hashing.
    /// </summary>
    /// <param name="event">The stored event.</param>
    /// <param name="cancellationToken">The read deadline.</param>
    /// <returns>The released content, or the original content when no metadata applies.</returns>
    public async Task<ExpandoObject> Release(AppendedEvent @event, CancellationToken cancellationToken = default)
    {
        var schema = await SchemaFor(@event, cancellationToken);
        if (!schema.Schema.HasSchemaMetadata()) return @event.Content;
        var identifier = @event.Context.Subject.IsSet ? @event.Context.Subject.Value : @event.Context.EventSourceId.Value;
        var json = converter.ToJsonObject(@event.Content, schema.Schema);
        var released = await metadata.Release(eventStore, @namespace, schema.Schema, identifier, json).WaitAsync(cancellationToken);
        return converter.ToExpandoObject(released, schema.Schema);
    }

    /// <summary>
    /// Gets the top-level properties of a stored event that carry personal data according to its schema generation.
    /// </summary>
    /// <param name="event">The stored event.</param>
    /// <param name="cancellationToken">The read deadline.</param>
    /// <returns>The property names.</returns>
    public async Task<IReadOnlyCollection<string>> PersonalPropertiesOf(AppendedEvent @event, CancellationToken cancellationToken = default) =>
        ConstraintPersonalData.PropertiesOf((await SchemaFor(@event, cancellationToken)).Schema);

    async Task<EventTypeSchema> SchemaFor(AppendedEvent @event, CancellationToken cancellationToken)
    {
        if (!_schemas.TryGetValue(@event.Context.EventType, out var schema))
        {
            schema = await eventTypes.GetFor(@event.Context.EventType.Id, @event.Context.EventType.Generation).WaitAsync(cancellationToken);
            _schemas[@event.Context.EventType] = schema;
        }

        return schema;
    }
}
