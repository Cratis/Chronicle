// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402, SA1649

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Chronicle.Testing.for_3434_repros.issue_3744;

/// <summary>
/// A username was claimed.
/// </summary>
/// <param name="Username">The username.</param>
[EventType]
public record Username3744Claimed([property: Unique(Username3744Claimed.ConstraintName)] string Username)
{
    /// <summary>
    /// The constraint name.
    /// </summary>
    public const string ConstraintName = "UniqueUsername3744";
}

/// <summary>
/// A username was released.
/// </summary>
[EventType]
[RemoveConstraint(Username3744Claimed.ConstraintName)]
public record Username3744Released;

public class when_two_event_sources_claim_the_same_value_sequentially : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _first;
    AppendResult _second;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        var value = Guid.NewGuid().ToString();
        _first = await _scenario.EventLog.Append(EventSourceId.New(), new Username3744Claimed(value));
        _second = await _scenario.EventLog.Append(EventSourceId.New(), new Username3744Claimed(value));
    }

    [Fact] void should_accept_the_first() => _first.ShouldBeSuccessful();
    [Fact] void should_reject_the_second() => _second.ShouldHaveConstraintViolation(Username3744Claimed.ConstraintName);

    public void Dispose() => _scenario.Dispose();
}

public class when_many_event_sources_claim_the_same_value_concurrently : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult[] _results;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        var value = Guid.NewGuid().ToString();
        _results = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() => _scenario.EventLog.Append(EventSourceId.New(), new Username3744Claimed(value)))));
    }

    [Fact] void should_accept_exactly_one() => _results.Count(_ => _.IsSuccess).ShouldEqual(1);

    public void Dispose() => _scenario.Dispose();
}

public class when_a_released_value_is_reclaimed_by_another_event_source : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _release;
    AppendResult _reclaim;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        var value = Guid.NewGuid().ToString();
        var holder = EventSourceId.New();
        await _scenario.EventLog.Append(holder, new Username3744Claimed(value));
        _release = await _scenario.EventLog.Append(holder, new Username3744Released());
        _reclaim = await _scenario.EventLog.Append(EventSourceId.New(), new Username3744Claimed(value));
    }

    [Fact] void should_accept_the_release() => _release.ShouldBeSuccessful();
    [Fact] void should_accept_the_reclaim() => _reclaim.ShouldBeSuccessful();

    public void Dispose() => _scenario.Dispose();
}

public class when_the_same_event_source_reclaims_its_value_after_release : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _reclaim;
    AppendResult _otherClaim;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        var value = Guid.NewGuid().ToString();
        var holder = EventSourceId.New();
        await _scenario.EventLog.Append(holder, new Username3744Claimed(value));
        await _scenario.EventLog.Append(holder, new Username3744Released());
        _reclaim = await _scenario.EventLog.Append(holder, new Username3744Claimed(value));
        _otherClaim = await _scenario.EventLog.Append(EventSourceId.New(), new Username3744Claimed(value));
    }

    [Fact] void should_accept_the_reclaim() => _reclaim.ShouldBeSuccessful();
    [Fact] void should_reject_another_source_after_the_reclaim() => _otherClaim.ShouldHaveConstraintViolation(Username3744Claimed.ConstraintName);

    public void Dispose() => _scenario.Dispose();
}
