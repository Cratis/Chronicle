// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Json;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Releases stored content before converting the selected v2 constraint components.
/// </summary>
/// <param name="eventTypeSchemas">The schemas for stored event generations.</param>
/// <param name="metadataManager">The status-aware protected-value release boundary.</param>
/// <param name="componentsBuilder">The ordered, schema-aware component builder.</param>
internal class UniqueConstraintRelease(
    IEventTypesStorage eventTypeSchemas,
    IJsonSchemaMetadataManager metadataManager,
    UniqueConstraintComponentsBuilder componentsBuilder)
{
    /// <summary>
    /// Resolves a stored event's actual schema and releases protected values before extraction.
    /// Release failures, including unrecorded missing keys, propagate to the caller.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The event store namespace.</param>
    /// <param name="definition">The constraint's ordered event properties.</param>
    /// <param name="event">The stored event.</param>
    /// <returns>Released components, or an explicit skip result for an affected erased value.</returns>
    internal async Task<ReleasedUniqueConstraintComponents> Create(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        UniqueConstraintEventDefinition definition,
        AppendedEvent @event)
    {
        var schema = (await eventTypeSchemas.GetFor(@event.Context.EventType.Id, @event.Context.EventType.Generation)).Schema;
        var content = JsonSerializer.SerializeToNode(@event.Content, Globals.JsonSerializerOptions)!.AsObject();
        var identifier = @event.Context.Subject.IsSet ? @event.Context.Subject.Value : @event.Context.EventSourceId.Value;
        var released = await metadataManager.ReleaseWithStatus(eventStore, @namespace, schema, identifier, content);
        if (ErasedComponentPaths.Affects(definition, released.Statuses))
        {
            return new([], UniqueConstraintReleaseOutcome.NoClaimErased);
        }
        return new(componentsBuilder.Build(definition, released.Content, schema), UniqueConstraintReleaseOutcome.Claim);
    }
}
