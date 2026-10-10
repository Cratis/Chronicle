// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402, SA1649

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;

namespace Cratis.Chronicle.Testing.for_3434_repros.issue_4520;

/// <summary>
/// Claims a handle that is unique across event sources.
/// </summary>
/// <param name="Handle">The handle.</param>
[EventType]
public record Handle4520Claimed([property: Unique(Handle4520Claimed.ConstraintName)] string Handle)
{
    /// <summary>
    /// The constraint name.
    /// </summary>
    public const string ConstraintName = "UniqueHandle4520";
}

/// <summary>
/// Control: the parameterless constructor discovers the [Unique] constraint.
/// </summary>
public class when_using_the_parameterless_constructor : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _duplicate;

    void Establish() => _scenario = new EventScenario();

    async Task Because()
    {
        await _scenario.EventLog.Append(EventSourceId.New(), new Handle4520Claimed("ada"));
        _duplicate = await _scenario.EventLog.Append(EventSourceId.New(), new Handle4520Claimed("ada"));
    }

    [Fact] void should_reject_the_duplicate() => _duplicate.ShouldHaveConstraintViolation(Handle4520Claimed.ConstraintName);

    public void Dispose() => _scenario.Dispose();
}

/// <summary>
/// The explicit-identity constructor treats a null provider as "no constraints"; there is no overload that combines
/// an explicit identity with discovery. Expected (per #4520): an overload exists and the duplicate is rejected.
/// </summary>
public class when_using_an_explicit_identity_with_a_null_provider : Specification, IDisposable
{
    EventScenario _scenario;
    AppendResult _duplicate;
    IImmutableList<AppendedEvent> _events;

    void Establish() => _scenario = new EventScenario(EventSequenceId.Log, "explicit-store-4520", "explicit-ns-4520", null);

    async Task Because()
    {
        await _scenario.EventLog.Append(EventSourceId.New(), new Handle4520Claimed("ada"));
        _duplicate = await _scenario.EventLog.Append(EventSourceId.New(), new Handle4520Claimed("ada"));
        _events = await _scenario.EventSequence.GetFromSequenceNumber(EventSequenceNumber.First);
    }

    [Fact] void should_reject_the_duplicate_as_discovery_would() => _duplicate.ShouldHaveConstraintViolation(Handle4520Claimed.ConstraintName);
    [Fact] void should_report_the_explicit_event_store_on_read_back() => _events[0].Context.EventStore.Value.ShouldEqual("explicit-store-4520");
    [Fact] void should_report_the_explicit_namespace_on_read_back() => _events[0].Context.Namespace.Value.ShouldEqual("explicit-ns-4520");

    public void Dispose() => _scenario.Dispose();
}
