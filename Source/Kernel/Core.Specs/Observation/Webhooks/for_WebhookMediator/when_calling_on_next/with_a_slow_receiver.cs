// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_a_slow_receiver : given.a_webhook_mediator
{
    IWebhookObserverSubscriber _subscriber;
    SlowReceiver _receiver;
    WebhookTarget _target;
    int _activeWhenTimeoutReturned;
    bool _returnedBeforeCleanup;
    Task _delivery = Task.CompletedTask;

    void Establish()
    {
        _target = new(new WebhookTargetUrl("https://example.com/webhook"), WebhookAuthorization.None, new Dictionary<string, string>());
        _receiver = new SlowReceiver();
        _httpClientFactory.Create(_target).Returns(new HttpClient(_receiver) { BaseAddress = new Uri(_target.Url) });
        _subscriber = Substitute.For<IWebhookObserverSubscriber>();
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(async call =>
            {
                await _mediator.OnNext("orders", _target, call.Arg<Key>(), call.Arg<IEnumerable<AppendedEvent>>());
                return ObserverSubscriberResult.Ok(EventSequenceNumber.First);
            });
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var token = call.Arg<CancellationToken>();
                var delivery = Deliver(call.Arg<Key>(), call.Arg<IEnumerable<AppendedEvent>>(), token);
                _delivery = delivery;

                // Orleans' default is to cancel the caller before the remote turn acknowledges cancellation.
                return delivery.WaitAsync(token);
            });
        _subscriber.Drain().Returns(_ => _delivery);
    }

    async Task<ObserverSubscriberResult> Deliver(Key partition, IEnumerable<AppendedEvent> events, CancellationToken token)
    {
        await _mediator.OnNext("orders", _target, partition, events, cancellationToken: token);
        return ObserverSubscriberResult.Ok(EventSequenceNumber.First);
    }

    async Task Because()
    {
        var delivery = Catch.Exception(() => _subscriber.OnNextWithin(TimeSpan.FromMilliseconds(100), "partition", [], new(null)));
        try
        {
            await _receiver.CancellationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            _returnedBeforeCleanup = delivery.IsCompleted;
        }
        finally
        {
            _receiver.ReleaseCleanup.TrySetResult();
        }
        await delivery;
        _activeWhenTimeoutReturned = _receiver.Active;
        await Catch.Exception(() => _subscriber.OnNextWithin(TimeSpan.FromMilliseconds(100), "partition", [], new(null)));
        await _receiver.Finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_wait_for_cancellation_cleanup() => _returnedBeforeCleanup.ShouldBeFalse();
    [Fact] void should_cancel_and_drain_before_returning() => _activeWhenTimeoutReturned.ShouldEqual(0);
    [Fact] void should_never_overlap_the_retry() => _receiver.Peak.ShouldEqual(1);
    [Fact] void should_cancel_the_http_request() => _receiver.Cancelled.ShouldEqual(2);

    class SlowReceiver : HttpMessageHandler
    {
        public int Active;
        public int Peak;
        public int Cancelled;
        public TaskCompletionSource Finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource CancellationStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseCleanup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _completed;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Peak = Math.Max(Peak, Interlocked.Increment(ref Active));
            try
            {
                await new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task.WaitAsync(cancellationToken);
                return new(HttpStatusCode.OK);
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref Cancelled);
                throw;
            }
            finally
            {
                // Cancellation must finish unwinding before a retry may enter the handler.
                CancellationStarted.TrySetResult();
                await ReleaseCleanup.Task;
                Interlocked.Decrement(ref Active);
                if (Interlocked.Increment(ref _completed) == 2) Finished.TrySetResult();
            }
        }
    }
}
