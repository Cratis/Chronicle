// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

public class and_stores_and_namespaces_share_keys : given.a_sink
{
    EventSequenceSink _otherStore;
    EventSequenceSink _otherNamespace;
    System.Dynamic.ExpandoObject _stateInOtherStore;

    async Task Because()
    {
        _otherStore = new("other-store", Tenant, _definition, _configuration, _destinations.Grains, _destinations.Storage, _destinations.Converter);
        _otherNamespace = new(Store, "other-tenant", _definition, _configuration, _destinations.Grains, _destinations.Storage, _destinations.Converter);
        await _sink.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);
        await _otherStore.ApplyChanges("customer-1", Fold(new(), 20, 1, 5), 5);
        await _otherNamespace.ApplyChanges("customer-1", Fold(new(), 30, 1, 5), 5);
        _stateInOtherStore = (await _otherStore.FindOrDefault("customer-1"))!;
    }

    [Fact] void should_publish_independently_per_store() => _destinations.Events("other-store", Tenant, EventSequenceId.Outbox).Count.ShouldEqual(1);
    [Fact] void should_publish_independently_per_namespace() => _destinations.Events(Store, "other-tenant", EventSequenceId.Outbox).Count.ShouldEqual(1);
    [Fact] void should_keep_the_original_untouched() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).Count.ShouldEqual(1);
    [Fact] void should_read_only_its_own_state() => TotalOf(_stateInOtherStore).ShouldEqual("20");
}
