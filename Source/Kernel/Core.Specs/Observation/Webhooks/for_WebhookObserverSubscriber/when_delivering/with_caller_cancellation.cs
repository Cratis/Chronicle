// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_caller_cancellation : given.a_webhook_delivery
{
    static readonly TimeSpan _safetyTimeout = TimeSpan.FromSeconds(5);
    CancellationTokenSource _cancellation;
    CancellationTokenSource _safety;
    Exception? _exception;

    void Establish()
    {
        _cancellation = new();
        _safety = new(_safetyTimeout);
        _receiver.ResponseGate = new TaskCompletionSource().Task;
    }

    async Task Because()
    {
        var delivery = _subscriber.OnNext(_partition, _events, new(null), _cancellation.Token);
        await _receiver.RequestReceived.Task.WaitAsync(_safety.Token);
        await _cancellation.CancelAsync();
        try
        {
            _deliveryResult = await delivery.WaitAsync(_safety.Token);
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
    }

    void Destroy()
    {
        _cancellation.Dispose();
        _safety.Dispose();
    }

    [Fact] void should_not_let_the_cancellation_escape() => _exception.ShouldBeNull();
    [Fact] void should_fail_the_delivery() => _deliveryResult.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_acknowledge_any_event() => _deliveryResult.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_defer_the_delivery() => _deliveryResult.Deferral.ShouldBeNull();
    [Fact] void should_keep_the_retry_policy() => _deliveryResult.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.Retry);
    [Fact] void should_report_an_exception_message() => _deliveryResult.ExceptionMessages.ShouldNotBeEmpty();
    [Fact] void should_have_sent_one_payload() => _receiver.Payloads.Count.ShouldEqual(1);
}
