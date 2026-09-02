// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModelExplorer;

/// <summary>
/// Represents a snapshot of a read model.
/// </summary>
/// <param name="Occurred">When the snapshot was taken.</param>
/// <param name="CorrelationId">The correlation the events behind the snapshot were appended under.</param>
/// <param name="Instance">The JSON representation of the read model as it stood at the snapshot.</param>
/// <param name="Events">The events that led to the snapshot.</param>
[ReadModel]
[BelongsTo(WellKnownServices.ReadModelExplorer)]
public record ReadModelSnapshot(DateTimeOffset Occurred, Guid CorrelationId, string Instance, IEnumerable<Event> Events)
{
    /// <summary>
    /// Gets all snapshots a read model instance passed through.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to resolve the read model and its projection with.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read events and definitions from.</param>
    /// <param name="eventCompliance">The <see cref="IEventCompliance"/> to release PII content with.</param>
    /// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> to render the state with.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> content is serialized with.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The event store namespace.</param>
    /// <param name="readModel">The read model identifier.</param>
    /// <param name="readModelKey">The read model key.</param>
    /// <param name="eventSequenceId">The event sequence the read model observes, defaulting to the event log.</param>
    /// <param name="grouping">How the events are grouped into snapshots, defaulting to by correlation.</param>
    /// <returns>The snapshots, oldest first.</returns>
    /// <remarks>
    /// The grouping is taken as a string rather than the enum it names, because an enum query parameter
    /// is dropped by the proxy generator and would silently never reach here.
    /// </remarks>
    public static Task<IEnumerable<ReadModelSnapshot>> AllSnapshotsForReadModel(
        IGrainFactory grainFactory,
        IStorage storage,
        IEventCompliance eventCompliance,
        IExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        string readModel,
        string readModelKey,
        string eventSequenceId = "event-log",
        string grouping = nameof(ReadModelSnapshotGrouping.Correlation)) =>
        ReadModelSnapshotReader.Read(
            new ReadModelIdentifier(readModel),
            eventStore,
            @namespace,
            new EventSequenceId(eventSequenceId),
            readModelKey,
            ParseGrouping(grouping),
            grainFactory,
            storage,
            eventCompliance,
            expandoObjectConverter,
            jsonSerializerOptions);

    static ReadModelSnapshotGrouping ParseGrouping(string grouping) =>
        Enum.TryParse<ReadModelSnapshotGrouping>(grouping, ignoreCase: true, out var parsed)
            ? parsed
            : ReadModelSnapshotGrouping.Correlation;
}
