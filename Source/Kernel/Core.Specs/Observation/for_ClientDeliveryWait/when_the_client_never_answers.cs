// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_ClientDeliveryWait;

public class when_the_client_never_answers : Specification
{
    readonly DeliveryClock _clock = new();
    readonly TaskCompletionSource<ObserverSubscriberResult> _completion = new();
    Task<ObserverSubscriberResult> _delivery;
    Exception? _exception;

    void Establish()
    {
        var context = new ObserverSubscriberContext(null).WithDeadlines(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(300), _clock);
        _delivery = new ClientDeliveryWait(_clock, NullLogger.Instance, ObserverKey.NotSet, "partition").For(_completion.Task, result => result, context);
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(331));
        try { await _delivery; }
        catch (Exception exception) { _exception = exception; }
    }

    [Fact] void should_bound_the_subscriber_turn() => _exception.ShouldBeOfExactType<AbandonedDeliveryDiscarded>();
    [Fact] void should_not_complete_the_client_work() => _completion.Task.IsCompleted.ShouldBeFalse();
}
