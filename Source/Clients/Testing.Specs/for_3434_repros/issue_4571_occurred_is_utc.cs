// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402, SA1649

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.Reactors;
using Cratis.Chronicle.Testing.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.for_3434_repros.issue_4571;

/// <summary>
/// Something happened at a point in time.
/// </summary>
/// <param name="Value">A value.</param>
[EventType]
public record Moment4571Recorded(string Value);

/// <summary>
/// Read model capturing the event's occurred time.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Value">The value.</param>
/// <param name="OccurredAt">When the event occurred.</param>
[FromEvent<Moment4571Recorded>]
public record Moment4571(
    Guid Id,
    string Value,
    [SetFromContext<Moment4571Recorded>(nameof(EventContext.Occurred))]
    DateTimeOffset OccurredAt);

/// <summary>
/// Records event contexts seen by <see cref="Moment4571Reactor"/>.
/// </summary>
public class Moment4571Recorder
{
    /// <summary>
    /// Gets the recorded contexts.
    /// </summary>
    public List<EventContext> Contexts { get; } = [];
}

/// <summary>
/// Reactor recording the event context.
/// </summary>
/// <param name="recorder">The recorder.</param>
public class Moment4571Reactor(Moment4571Recorder recorder) : IReactor
{
    /// <summary>
    /// Records the context.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The context.</param>
    /// <returns>Awaitable task.</returns>
    public Task Recorded(Moment4571Recorded @event, EventContext context)
    {
        recorder.Contexts.Add(context);
        return Task.CompletedTask;
    }
}

public class when_appending_through_event_scenario : Specification, IDisposable
{
    EventScenario _scenario;
    IImmutableList<AppendedEvent> _events;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        await _scenario.EventLog.Append(EventSourceId.New(), new Moment4571Recorded("a"));
        _events = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First);
    }

    [Fact] void should_stamp_occurred_in_utc() => _events[0].Context.Occurred.Offset.ShouldEqual(TimeSpan.Zero);

    public void Dispose() => _scenario.Dispose();
}

public class when_projecting_occurred_in_read_model_scenario : Specification
{
    ReadModelScenario<Moment4571> _scenario;
    DateTimeOffset _before;

    void Establish()
    {
        _scenario = new ReadModelScenario<Moment4571>();
        _before = DateTimeOffset.UtcNow.AddSeconds(-1);
    }

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new Moment4571Recorded("a"));

    [Fact] void should_stamp_occurred_in_utc() => _scenario.Instance!.OccurredAt.Offset.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_stamp_occurred_no_earlier_than_the_seed_time() => (_scenario.Instance!.OccurredAt >= _before).ShouldBeTrue();
}

public class when_reacting_in_reactor_scenario : Specification
{
    ReactorScenario<Moment4571Reactor> _scenario;
    Moment4571Recorder _recorder;

    void Establish()
    {
        _recorder = new();
        _scenario = new ReactorScenario<Moment4571Reactor>();
        _scenario.Services.AddSingleton(_recorder);
    }

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new Moment4571Recorded("a"));

    [Fact] void should_stamp_occurred_in_utc() => _recorder.Contexts.Single().Occurred.Offset.ShouldEqual(TimeSpan.Zero);
}
