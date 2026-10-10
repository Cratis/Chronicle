// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402, SA1649

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.for_3434_repros.issue_3912;

/// <summary>
/// A thing was created.
/// </summary>
[EventType]
public record Thing3912Created;

/// <summary>
/// A thing was renamed.
/// </summary>
/// <param name="Name">The new name.</param>
[EventType]
public record Thing3912Renamed(string Name);

/// <summary>
/// Materialized (non-passive) read model.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
[FromEvent<Thing3912Created>]
public record Thing3912(Guid Id, [SetFrom<Thing3912Renamed>(nameof(Thing3912Renamed.Name))] string Name);

/// <summary>
/// Records handler invocations.
/// </summary>
public class Thing3912Recorder
{
    /// <summary>
    /// Gets the names seen (null when the read model was null).
    /// </summary>
    public List<string?> Seen { get; } = [];
}

/// <summary>
/// Reactor taking a non-nullable read model.
/// </summary>
/// <param name="recorder">The recorder.</param>
public class Thing3912Reactor(Thing3912Recorder recorder) : IReactor
{
    /// <summary>
    /// Handles the rename.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="thing">The read model.</param>
    /// <returns>Awaitable task.</returns>
    public Task Renamed(Thing3912Renamed @event, Thing3912 thing)
    {
        recorder.Seen.Add(thing?.Name);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Reactor taking a nullable read model.
/// </summary>
/// <param name="recorder">The recorder.</param>
public class Thing3912NullableReactor(Thing3912Recorder recorder) : IReactor
{
    /// <summary>
    /// Handles the rename.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="thing">The read model.</param>
    /// <returns>Awaitable task.</returns>
    public Task Renamed(Thing3912Renamed @event, Thing3912? thing)
    {
        recorder.Seen.Add(thing?.Name);
        return Task.CompletedTask;
    }
}

public class when_the_read_model_comes_only_from_given_events : Specification
{
    ReactorScenario<Thing3912Reactor> _scenario;
    Thing3912Recorder _recorder;
    Exception? _exception;

    void Establish()
    {
        _recorder = new();
        _scenario = new ReactorScenario<Thing3912Reactor>();
        _scenario.Services.AddSingleton(_recorder);
    }

    async Task Because() => _exception = await Catch.Exception(async () =>
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new Thing3912Created(), new Thing3912Renamed("New Name")));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_invoke_the_handler_once() => _recorder.Seen.Count.ShouldEqual(1);
    [Fact] void should_resolve_the_materialized_name() => _recorder.Seen.Single().ShouldEqual("New Name");
}

public class when_the_nullable_read_model_comes_only_from_given_events : Specification
{
    ReactorScenario<Thing3912NullableReactor> _scenario;
    Thing3912Recorder _recorder;
    Exception? _exception;

    void Establish()
    {
        _recorder = new();
        _scenario = new ReactorScenario<Thing3912NullableReactor>();
        _scenario.Services.AddSingleton(_recorder);
    }

    async Task Because() => _exception = await Catch.Exception(async () =>
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new Thing3912Created(), new Thing3912Renamed("New Name")));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_invoke_the_handler_once() => _recorder.Seen.Count.ShouldEqual(1);
    [Fact] void should_resolve_the_materialized_name() => _recorder.Seen.Single().ShouldEqual("New Name");
}
