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
/// Represents the API for working with capture queries.
/// </summary>
[Route("/api/event-store/{eventStore}/captures")]
public class CaptureQueries : ControllerBase
{
    /// <summary>
    /// The default maximum number of captured events returned by <see cref="CapturedEvents"/>.
    /// </summary>
    public const int DefaultMaxEvents = 200;

    readonly ICapturesService _captures;
    readonly IEventSequencesService _eventSequences;

    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureQueries"/> class.
    /// </summary>
    /// <param name="captures"><see cref="ICapturesService"/> for working with captures.</param>
    /// <param name="eventSequences"><see cref="IEventSequencesService"/> for getting captured events.</param>
    internal CaptureQueries(
        ICapturesService captures,
        IEventSequencesService eventSequences)
    {
        _captures = captures;
        _eventSequences = eventSequences;
    }

    /// <summary>
    /// Get all captures for an event store.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <returns>Collection of <see cref="Capture"/>.</returns>
    [HttpGet]
    public async Task<IEnumerable<Capture>> GetCaptures([FromRoute] string eventStore)
    {
        var captures = await _captures.GetCaptures(new() { EventStore = eventStore });
        return captures.ToApi();
    }

    /// <summary>
    /// Observe all captures for an event store.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <returns>An observable of collections of <see cref="Capture"/>.</returns>
    [HttpGet("all-captures/observe")]
    public ISubject<IEnumerable<Capture>> AllCaptures([FromRoute] string eventStore) =>
        _captures.InvokeAndWrapWithTransformSubject(
            token => _captures.ObserveCaptures(new() { EventStore = eventStore }, token),
            captures => captures.ToApi());

    /// <summary>
    /// Get the events ingested by a capture - the events tagged with the capture's name.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="captureName">Name of the capture.</param>
    /// <param name="namespace">Optional namespace the events were ingested into - defaults to the default namespace.</param>
    /// <param name="maxEvents">Optional maximum number of events to return, most recent first - defaults to <see cref="DefaultMaxEvents"/>.</param>
    /// <returns>Collection of <see cref="AppendedEvent"/>, most recent first.</returns>
    [HttpGet("{captureName}/events")]
    public async Task<IEnumerable<AppendedEvent>> CapturedEvents(
        [FromRoute] string eventStore,
        [FromRoute] string captureName,
        [FromQuery] string? @namespace = default,
        [FromQuery] int maxEvents = DefaultMaxEvents)
    {
        var response = await _eventSequences.GetEventsFromEventSequenceNumber(new()
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
