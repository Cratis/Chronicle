// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The exception that is thrown when <see cref="ReadModelScenario{TReadModel}.Instance"/> is accessed but
/// the seeded events materialized more than one read model instance, making a single result ambiguous.
/// </summary>
/// <remarks>
/// Assert against a specific instance with <see cref="ReadModelScenario{TReadModel}.InstanceForEventSourceId(EventSourceId)"/>,
/// or enumerate <see cref="ReadModelScenario{TReadModel}.Instances"/>, instead of <see cref="ReadModelScenario{TReadModel}.Instance"/>.
/// </remarks>
/// <param name="readModelType">The read model type under test.</param>
/// <param name="eventSourceIds">The event source ids of the materialized instances.</param>
public class MultipleInstancesMaterialized(Type readModelType, IEnumerable<EventSourceId> eventSourceIds)
    : Exception(BuildMessage(readModelType, [.. eventSourceIds]))
{
    static string BuildMessage(Type readModelType, IReadOnlyCollection<EventSourceId> eventSourceIds) =>
        $"Events materialized {eventSourceIds.Count} instances of '{readModelType.Name}' ({string.Join(", ", eventSourceIds)}). " +
        "'Instance' is ambiguous — use InstanceForEventSourceId(id) or Instances to select the intended one.";
}
