// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage.Seeding;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.given;

public class overlapping_global_seed_groups : a_global_event_seeding_grain
{
    protected EventSeeds _stored;
    protected IEventSequence _existingEvents;
    protected IEventSequence _futureEvents;
    protected int _futureCount;

    void Establish()
    {
        _stored = _state.State;
        _state.State.Returns(_ => _stored);
        _state.When(_ => _.State = Arg.Any<EventSeeds>()).Do(call => _stored = call.Arg<EventSeeds>());
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>(["existing"]));
        _grainFactory.GetGrain<INamespaces>(Arg.Any<string>()).Returns(namespaces);
        _existingEvents = CreateNamespace("existing");
    }

    protected async Task ApplyToFutureNamespace()
    {
        _futureEvents = CreateNamespace("future");
        var global = Substitute.For<IResultAwareEventSeeding>();
        global.GetSeededEvents().Returns(_ => _grain.GetSeededEvents());
        _grainFactory.GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForGlobal("TestEventStore").ToString()).Returns(global);
        var reactor = new NamespacesReactor(_grainFactory, Substitute.For<IPatternCapture>(), NullLogger<NamespacesReactor>.Instance);
        await reactor.Added(new NamespaceAdded("TestEventStore", "future"), null!);
        _futureCount = AppendedCount(_futureEvents);
    }

    protected static int AppendedCount(IEventSequence events) => events.ReceivedCalls()
        .Where(call => call.GetMethodInfo().Name == nameof(IEventSequence.AppendMany))
        .Sum(call => ((IEnumerable<EventToAppend>)call.GetArguments()[0]!).Count());

    protected static SeedingEntry StatusChanged(string status) =>
        new("work-item", "StatusChanged", $"{{\"status\":\"{status}\"}}", null);

    protected static IEnumerable<string> AppendedStatuses(IEventSequence events) => events.ReceivedCalls()
        .Where(call => call.GetMethodInfo().Name == nameof(IEventSequence.AppendMany))
        .SelectMany(call => (IEnumerable<EventToAppend>)call.GetArguments()[0]!)
        .Select(entry => entry.Content["status"]!.GetValue<string>());

    IEventSequence CreateNamespace(string name)
    {
        var events = Substitute.For<IEventSequence>();
        events.AppendMany(
            Arg.Any<IEnumerable<EventToAppend>>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScopes>())
            .Returns(_ => Task.FromResult(ASuccessfulResult()));
        var state = Substitute.For<IPersistentState<EventSeeds>>();
        state.State.Returns(new EventSeeds(
            new Dictionary<Concepts.Events.EventTypeId, IEnumerable<SeededEventEntry>>(),
            new Dictionary<Concepts.Events.EventSourceId, IEnumerable<SeededEventEntry>>()));
        var grain = new EventSeeding(state, _grainFactory, NullLogger<EventSeeding>.Instance);
        var key = EventSeedingKey.ForNamespace("TestEventStore", name);
        typeof(EventSeeding).GetField("_key", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(grain, key);
        typeof(EventSeeding).GetField("_eventSequence", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(grain, events);
        _grainFactory.GetGrain<IResultAwareEventSeeding>(key.ToString()).Returns(grain);
        return events;
    }
}
