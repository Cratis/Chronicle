// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber;

public class when_an_expired_delivery_reaches_the_subscriber : given.a_subscriber_with_a_cached_pipeline
{
    ObserverSubscriberResult _result;

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromDays(365 * 100));

        // Deserializing also exercises compatibility with the optional, cross-silo context fields.
        var context = JsonSerializer.Deserialize<ObserverSubscriberContext>("""
            {"Metadata":null,"DeliverBy":"2000-01-01T00:00:00Z","AbandonAt":"2000-01-01T00:05:00Z"}
            """)! with { Metadata = new ConnectedClient() };
        _result = await _subscriber.OnNext("the-partition", [], context);
    }

    [Fact] void should_not_dispatch_to_the_pipeline() => _ = _firstPipeline.DidNotReceive().Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>());
    [Fact] void should_fail_the_expired_delivery() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
}
