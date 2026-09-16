// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_ordered_batch_receipts : given.an_acknowledged_append
{
    AppendManyResult _result;

    async Task Because() => _result = await _eventSequence.AppendMany(_source, ["first", "second"]);

    [Fact] void should_use_the_single_source_rpc_without_explicit_routes() => _legacyRequest.ShouldNotBeNull();
    [Fact] void should_return_ordered_receipts() => _result.Receipts.Select(_ => _.SequenceNumber.Value).ShouldEqual([42UL, 47UL]);
    [Fact] void should_associate_receipts_with_input_events() => _notifications.Select(_ => _.Event.Content).ShouldEqual(["first", "second"]);
    [Fact] void should_notify_with_ordered_persisted_contexts() => _notifications.Select(_ => _.Event.Context).ShouldEqual(_result.Receipts);
    [Fact] void should_carry_each_receipt_on_its_notification_result() => _notifications.All(_ => _.Result.Receipt == _.Event.Context).ShouldBeTrue();
    [Fact] void should_use_persisted_store_for_observer_completion() => _result.EventStore.ShouldEqual(_eventStoreName);
    [Fact] void should_use_persisted_namespace_for_observer_completion() => _result.EventStoreNamespace.ShouldEqual(_namespace);
}
