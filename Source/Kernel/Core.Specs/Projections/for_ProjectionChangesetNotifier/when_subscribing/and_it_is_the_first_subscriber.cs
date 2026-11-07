// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.when_subscribing;

public class and_it_is_the_first_subscriber : given.a_subscribed_notifier
{
    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_publish_that_the_projection_is_watched_once() => _projectionsServiceClient.Received(1).SetWatchState(_eventStore, _namespace, _projection, true);
    [Fact] void should_not_publish_anything_else() => _projectionsServiceClient.ReceivedWithAnyArgs(1).SetWatchState(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ObserverId>(), Arg.Any<bool>());
}
