// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_using_an_explicit_identity;

public class with_discovered_constraints : Specification
{
    EventScenario _scenario;
    AppendResult _duplicate;
    IImmutableList<AppendedEvent> _events;
    string _handle;

    async Task Establish()
    {
        _scenario = new(EventSequenceId.Log, "explicit-store", "explicit-namespace");
        _handle = Guid.NewGuid().ToString();
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new DiscoveredHandleClaimed(_handle));
    }

    async Task Because()
    {
        _duplicate = await _scenario.EventLog.Append(EventSourceId.New(), new DiscoveredHandleClaimed(_handle));
        _events = await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_discover_the_unique_constraint() => _duplicate.ShouldHaveConstraintViolationFor(DiscoveredHandleClaimed.ConstraintName);
    [Fact] void should_preserve_the_store_identity() => _events.Single().Context.EventStore.Value.ShouldEqual("explicit-store");
    [Fact] void should_preserve_the_namespace_identity() => _events.Single().Context.Namespace.Value.ShouldEqual("explicit-namespace");
}
