// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_the_first_page_run_fails : given.a_page_subscriber
{
    ObserverSubscriberResult _result;

    void Establish() => _cachedPipeline.Configure().Handle(_events[0]).Returns(Task.FromException<ProjectionEventContext>(new InvalidOperationException("boom")));

    async Task Because() => _result = await _subscriber.OnNextPage(_events, new(null));

    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_notify_a_page() => _notifier.DidNotReceive().NotifyMany(_key.Namespace, Arg.Any<IReadOnlyList<ReadModelChangeNotification>>());
    [Fact] void should_not_notify_individual_runs() => _notifier.DidNotReceive().Notify(_key.Namespace, Arg.Any<ReadModelKey>(), Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
}
