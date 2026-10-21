// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_ClientDeliveryWait;

public class when_the_result_arrives_after_the_deadline : Specification
{
    readonly DeliveryClock _clock = new();
    readonly TaskCompletionSource<ObserverSubscriberResult> _completion = new();
    Task<ObserverSubscriberResult> _delivery;
    bool _held;
    Exception? _exception;

    void Establish()
    {
        var context = new ObserverSubscriberContext(null).WithDeadlines(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(300), _clock);
        _delivery = new ClientDeliveryWait(_clock, NullLogger.Instance, ObserverKey.NotSet, "partition").For(_completion.Task, result => result, context);
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(31));
        _held = !_delivery.IsCompleted;
        _completion.SetResult(ObserverSubscriberResult.Ok(42));
        try { await _delivery; }
        catch (Exception exception) { _exception = exception; }
    }

    [Fact] void should_hold_the_subscriber_turn_past_the_caller_timeout() => _held.ShouldBeTrue();
    [Fact] void should_discard_the_late_result() => _exception.ShouldBeOfExactType<AbandonedDeliveryDiscarded>();
}
