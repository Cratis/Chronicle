// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_a_page;

public class and_the_pipeline_reports_a_change : given.a_page_subscriber
{
    void Establish() => _notifications[(Key)"first"] = ChangeFor("first");

    Task Because() => Deliver();

    [Fact] void should_notify_watchers_of_the_change() => _notifier.Received(1).Notify(Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
    [Fact] void should_settle_both_partitions_as_handled() => _result.Results.Values.All(_ => _.State == ObserverSubscriberState.Ok).ShouldBeTrue();
}
