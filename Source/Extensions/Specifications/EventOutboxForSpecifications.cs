// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Json;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Specifications.Types;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents an implementation of <see cref="IEventLog"/> for specifications.
/// </summary>
public class EventOutboxForSpecifications : IEventOutbox
{
    readonly EventSequenceForSpecifications _sequence;

    /// <summary>
    /// Gets the appended events.
    /// </summary>
    public IEnumerable<AppendedEventForSpecifications> AppendedEvents => _sequence.AppendedEvents;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventOutboxForSpecifications"/> class.
    /// </summary>
    public EventOutboxForSpecifications()
    {
        var schemaGenerator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()));

        var typeFormats = new TypeFormats();
        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
        _sequence = new(expandoObjectConverter, schemaGenerator);
    }

    /// <inheritdoc/>
    public Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = default) => _sequence.Append(eventSourceId, @event);
}
