// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors.when_registering;

public class and_many_tenants_have_sql_metadata : given.persistent_registrations
{
    protected override bool UseMongoDB => false;
    ReactorDefinition _original;

    async Task Establish()
    {
        await _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default);
        _original = _persisted.Values.Single();
        _definitions.ClearReceivedCalls();
        _observer.ClearReceivedCalls();
    }

    Task Because() => Task.WhenAll(Enumerable.Range(0, 40).Select(tenant =>
        _reactors.DiscoverAndRegister(_eventStore, (EventStoreNamespaceName)$"tenant-{tenant}")));

    [Fact] async Task should_not_rewrite_unchanged_persistent_metadata() => await _definitions.DidNotReceive().Save(Arg.Any<ReactorDefinition>());
    [Fact] void should_have_non_null_empty_tags() => _original.Tags!.ShouldBeEmpty();
    [Fact] void should_have_unrestricted_source_sentinel() => _original.Filters!.EventSourceType.ShouldEqual(EventSourceType.Unspecified);
    [Fact] void should_have_unrestricted_stream_sentinel() => _original.Filters!.EventStreamType!.IsAll.ShouldBeTrue();
    [Fact] void should_preserve_the_persisted_definition() => ReferenceEquals(_original, _persisted.Values.Single()).ShouldBeTrue();
    [Fact] void should_keep_the_exact_subscribed_event_definition() => _persisted.Values.Single().EventTypes.Single().EventType.ShouldEqual(typeof(given.registrations.TenantReactor).GetEventTypes().Single());
    [Fact] void should_subscribe_every_tenant() => _observer.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IObserver.Subscribe)).ShouldEqual(40);
}
