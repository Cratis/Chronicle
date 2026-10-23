// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_receiving_an_empty_page : given.a_page_subscriber
{
    ObserverSubscriberResult _result;

    void Establish() => _events = [];

    async Task Because() => _result = await _subscriber.OnNextPage(_events, new(null));

    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_not_notify_a_page() => _notifier.DidNotReceive().NotifyMany(_key.Namespace, Arg.Any<IReadOnlyList<ReadModelChangeNotification>>());
    [Fact] void should_not_prepare_a_page() => _cachedPipeline.DidNotReceive().PreparePage(Arg.Any<IReadOnlyList<AppendedEvent>>());
    [Fact] void should_not_notify_individual_runs() => _notifier.DidNotReceive().Notify(_key.Namespace, Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
}
