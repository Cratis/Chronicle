// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors.when_registering;

public class and_sql_restrictions_change : given.persistent_registrations
{
    protected override bool UseMongoDB => false;

    async Task Establish() => await _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default);

    [Theory]
    [InlineData("tags")]
    [InlineData("filter-tags")]
    [InlineData("source")]
    [InlineData("stream")]
    [InlineData("event-types")]
    async Task should_persist_changed_metadata_and_then_stop_rewriting(string change)
    {
        var original = _persisted.Values.Single();
        var changed = change switch
        {
            "tags" => original with { Tags = ["restricted"] },
            "filter-tags" => original with { Filters = new ObserverFilters(["restricted"]) },
            "source" => original with { Filters = new ObserverFilters([], new EventSourceType("restricted")) },
            "stream" => original with { Filters = new ObserverFilters([], EventStreamType: new EventStreamType("restricted")) },
            _ => original with { EventTypes = [] }
        };
        _persisted[original.Identifier] = RoundTrip(changed);
        _definitions.ClearReceivedCalls();
        await _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default);
        await _reactors.DiscoverAndRegister(_eventStore, (EventStoreNamespaceName)"another-tenant");
        await _definitions.Received(1).Save(Arg.Any<ReactorDefinition>());
        ReferenceEquals(original, _persisted.Values.Single()).ShouldBeFalse();
        _persisted.Values.Single().EventTypes.Single().EventType.ShouldEqual(typeof(given.registrations.TenantReactor).GetEventTypes().Single());
    }
}
