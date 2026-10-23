// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_receiving_a_page : given.a_page_subscriber
{
    ObserverSubscriberResult _result;

    async Task Because() => _result = await _subscriber.OnNextPage(_events, new(null));

    [Fact] void should_handle_in_global_sequence_order() => _handled.ShouldEqual(new EventSequenceNumber[] { 1, 2, 3 });
    [Fact] void should_finish_at_the_last_event() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_notify_each_parent_run() => _notifier.Received(2).Notify(_key.Namespace, "parent", Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
    [Fact] void should_notify_the_child_run() => _notifier.Received(1).Notify(_key.Namespace, "child", Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
}
