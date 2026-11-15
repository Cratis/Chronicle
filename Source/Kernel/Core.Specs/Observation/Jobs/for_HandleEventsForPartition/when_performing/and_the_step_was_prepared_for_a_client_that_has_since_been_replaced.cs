// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Reactors.Clients;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

/// <summary>
/// A job keeps only a window of its steps running, so a step prepared when its job started can run long after,
/// when the client it was prepared for has been replaced by a newer connection.
/// </summary>
public class and_the_step_was_prepared_for_a_client_that_has_since_been_replaced : given.a_performing_job_step
{
    IReactorObserverSubscriber _client;
    ObserverSubscriberContext _context;

    void Establish()
    {
        var silo = SiloAddress.FromParsableString("127.0.0.1:11111@1");
        var replaced = new ObserverSubscription(_performState.ObserverKey.ObserverId, _performState.ObserverKey, [], typeof(IReactorObserverSubscriber), silo)
        {
            Targets = [new ObserverSubscriberTarget(silo, new ConnectedClient { ConnectionId = "old" })]
        };
        var current = replaced with { Targets = [new ObserverSubscriberTarget(silo, new ConnectedClient { ConnectionId = "new" })] };
        _observer.GetSubscription().Returns(current);

        _client = Substitute.For<IReactorObserverSubscriber>();
        _client
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                _context = call.Arg<ObserverSubscriberContext>();
                return Task.FromResult(ObserverSubscriberResult.Disconnected());
            });
        _jobStep.SetupForTesting(_observer, _client, "some-partition", replaced);
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_deliver_to_the_client_that_is_subscribed_now() => ((ConnectedClient)_context.Metadata!).ConnectionId.Value.ShouldEqual("new");
}
