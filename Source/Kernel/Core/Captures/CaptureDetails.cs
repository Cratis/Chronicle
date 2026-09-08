// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents the read model for a capture, providing query access to the captures an event store holds.
/// </summary>
/// <param name="Id">The unique identifier of the capture.</param>
/// <param name="Name">The name of the capture, derived from its declaration.</param>
/// <param name="Declaration">The capture declaration language source text.</param>
/// <param name="Status">Whether the capture is started or stopped.</param>
/// <remarks>
/// Named for what it carries rather than for the concept, because <c>Capture</c> in this namespace is already
/// the stored capture the engine works with.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.Captures)]
public record CaptureDetails(
    string Id,
    string Name,
    string Declaration,
    Contracts.Captures.CaptureStatus Status)
{
    /// <summary>
    /// Gets every capture an event store holds.
    /// </summary>
    /// <param name="eventStore">The event store to get captures for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the captures.</param>
    /// <returns>A collection of captures.</returns>
    internal static async Task<IEnumerable<CaptureDetails>> GetCaptures(EventStoreName eventStore, IStorage storage)
    {
        var captures = await storage.GetEventStore(eventStore).Captures.GetAll();
        return captures.ToReadModel();
    }

    /// <summary>
    /// Observes every capture an event store holds.
    /// </summary>
    /// <param name="eventStore">The event store to observe captures for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the captures.</param>
    /// <returns>An observable subject emitting collections of captures.</returns>
    internal static ISubject<IEnumerable<CaptureDetails>> ObserveCaptures(EventStoreName eventStore, IStorage storage) =>
        storage.GetEventStore(eventStore).Captures.ObserveAll().TransformSubject(_ => _.ToReadModel());
}
