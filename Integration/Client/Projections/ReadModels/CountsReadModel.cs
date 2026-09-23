// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.ReadModels;

/// <summary>
/// A single fixed-key document that counts things across every event source, the shape an application
/// reaches for when a dashboard needs a live count rather than a query that walks a collection.
/// </summary>
/// <param name="Id">The document's identity - the constant the projection keys on.</param>
/// <param name="Registrations">A root-level counter, so the root carries its own event types alongside children.</param>
/// <param name="Things">The membership set.</param>
/// <param name="__lastHandledEventSequenceNumber">Chronicle's own position marker.</param>
public record CountsReadModel(
    string Id,
    int Registrations,
    IEnumerable<CountedThing> Things,
    EventSequenceNumber? __lastHandledEventSequenceNumber = default)
{
    /// <summary>
    /// Gets how many things are still open - the number the dashboard would actually show.
    /// </summary>
    public int Open => Things?.Count(thing => thing.IsOpen) ?? 0;
}
