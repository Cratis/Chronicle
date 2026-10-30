// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_receiving_a_page_for_an_inactive_definition : given.a_page_subscriber
{
    ObserverSubscriberResult _result;

    async Task Establish()
    {
        _pipelines.GetFor(_key.EventStore, _key.Namespace, _projection).Returns(_cachedPipeline);
        await _subscriber.OnProjectionDefinitionsChanged(_definition with { IsActive = false });
    }

    async Task Because() => _result = await _subscriber.OnNextPage(_events, new(null));

    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_not_notify_a_page() => _notifier.DidNotReceive().NotifyMany(_key.Namespace, Arg.Any<IReadOnlyList<ReadModelChangeNotification>>());
    [Fact] void should_not_notify_individual_runs() => _notifier.DidNotReceive().Notify(_key.Namespace, Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
}
