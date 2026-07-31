// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Reactive;
using ICapturesService = Cratis.Chronicle.Contracts.Captures.ICaptures;
using IEventSequencesService = Cratis.Chronicle.Contracts.EventSequences.IEventSequences;

namespace Cratis.Chronicle.Api.Captures;

/// <summary>
/// Represents a capture.
/// </summary>
/// <param name="Id">The unique identifier of the capture.</param>
/// <param name="Name">The name of the capture.</param>
/// <param name="Declaration">The capture declaration language source text.</param>
/// <param name="Status">The <see cref="CaptureStatus"/>.</param>
[ReadModel]
public record Capture(string Id, string Name, string Declaration, CaptureStatus Status)
{
    /// <summary>
    /// Get all captures for an event store.
    /// </summary>
    /// <param name="captures">The <see cref="ICapturesService"/> contract.</param>
    /// <param name="eventStore">The event store to get captures for.</param>
    /// <returns>Collection of <see cref="Capture"/>.</returns>
    public static async Task<IEnumerable<Capture>> GetCaptures(ICapturesService captures, string eventStore) =>
        (await captures.GetCaptures(new() { EventStore = eventStore })).ToApi();

    /// <summary>
    /// Get and observe all captures for an event store.
    /// </summary>
    /// <param name="captures">The <see cref="ICapturesService"/> contract.</param>
    /// <param name="eventStore">The event store to observe captures for.</param>
    /// <returns>An observable of collections of <see cref="Capture"/>.</returns>
    public static ISubject<IEnumerable<Capture>> AllCaptures(ICapturesService captures, string eventStore) =>
        captures.InvokeAndWrapWithTransformSubject(
            token => captures.ObserveCaptures(new() { EventStore = eventStore }, token),
            captureCollection => captureCollection.ToApi());

    /// <summary>
    /// Get the events ingested by a capture - the events tagged with the capture's name, most recent first.
    /// </summary>
    /// <param name="eventSequences">The <see cref="IEventSequencesService"/> for getting the events.</param>
    /// <param name="eventStore">The event store the capture belongs to.</param>
    /// <param name="captureName">The name of the capture.</param>
    /// <param name="namespace">Optional namespace the events were ingested into - defaults to the default namespace.</param>
    /// <param name="maxEvents">Optional maximum number of events to return, most recent first.</param>
    /// <returns>Collection of <see cref="AppendedEvent"/>, most recent first.</returns>
    public static async Task<IEnumerable<AppendedEvent>> CapturedEvents(
        IEventSequencesService eventSequences,
        string eventStore,
        string captureName,
        string? @namespace = default,
        int maxEvents = 200)
    {
        var response = await eventSequences.GetEventsFromEventSequenceNumber(new()
        {
            EventStore = eventStore,
            Namespace = @namespace ?? "Default",
            EventSequenceId = EventSequenceId.Log,
            FromEventSequenceNumber = 0,
            Tags = [captureName]
        });

        return response.Events.ToApi()
            .OrderByDescending(@event => @event.Context.SequenceNumber)
            .Take(maxEvents)
            .ToArray();
    }
}
