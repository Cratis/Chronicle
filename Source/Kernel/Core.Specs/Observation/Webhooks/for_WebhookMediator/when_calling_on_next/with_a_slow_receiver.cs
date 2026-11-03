// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_a_slow_receiver : given.a_webhook_mediator
{
    HttpClient _client;
    IWebhookObserverSubscriber _subscriber;
    SlowReceiver _receiver;
    WebhookTarget _target;
    int _activeWhenTimeoutReturned;
    bool _returnedBeforeCleanup;
    bool _cancelledBeforeDeadline;
    bool _cancelledAtDeadline;
    readonly given.DeliveryClock _clock = new();
    Task _delivery = Task.CompletedTask;
    Exception? _timeout;
    Exception? _retryTimeout;

    void Establish()
    {
        _target = new(new WebhookTargetUrl("https://example.com/webhook"), WebhookAuthorization.None, new Dictionary<string, string>());
        _receiver = new SlowReceiver();
        _client = new HttpClient(_receiver) { BaseAddress = new Uri(_target.Url), Timeout = System.Threading.Timeout.InfiniteTimeSpan };
        _httpClientFactory.Create(_target).Returns(_client);
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
        var delivery = Catch.Exception(() => ObserverSubscriberExtensions.OnNextWebhook(_subscriber, new(TimeSpan.FromMilliseconds(100), _clock), "partition", [], new(null), CancellationToken.None));
        await _receiver.Entered.Task;
        _clock.Advance(TimeSpan.FromMilliseconds(99));
        _cancelledBeforeDeadline = _receiver.Token.IsCancellationRequested;
        _clock.Advance(TimeSpan.FromMilliseconds(1));
        _cancelledAtDeadline = _receiver.Token.IsCancellationRequested;
        try
        {
            await _receiver.CancellationStarted.Task;
            _returnedBeforeCleanup = delivery.IsCompleted;
        }
        finally
        {
            _receiver.ReleaseCleanup.TrySetResult();
        }
        _timeout = await delivery;
        _activeWhenTimeoutReturned = _receiver.Active;
        _receiver.Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var retry = Catch.Exception(() => ObserverSubscriberExtensions.OnNextWebhook(_subscriber, new(TimeSpan.FromMilliseconds(100), _clock), "partition", [], new(null), CancellationToken.None));
        await _receiver.Entered.Task;
        _clock.Advance(TimeSpan.FromMilliseconds(100));
        _retryTimeout = await retry;
        await _receiver.Finished.Task;
    }

    [Fact] void should_not_cancel_before_the_deadline() => _cancelledBeforeDeadline.ShouldBeFalse();
    [Fact] void should_cancel_at_the_deadline() => _cancelledAtDeadline.ShouldBeTrue();
    [Fact] void should_report_the_delivery_timeout() => _timeout.ShouldBeOfExactType<SubscriberCallTimedOut>();
    [Fact] void should_report_the_retry_timeout() => _retryTimeout.ShouldBeOfExactType<SubscriberCallTimedOut>();
    [Fact] void should_wait_for_cancellation_cleanup() => _returnedBeforeCleanup.ShouldBeFalse();
    [Fact] void should_cancel_and_drain_before_returning() => _activeWhenTimeoutReturned.ShouldEqual(0);
    [Fact] void should_never_overlap_the_retry() => _receiver.Peak.ShouldEqual(1);
    [Fact] void should_cancel_the_http_request() => _receiver.Cancelled.ShouldEqual(2);

    void Destroy() => _client.Dispose();

    class SlowReceiver : HttpMessageHandler
    {
        public int Active;
        public int Peak;
        public int Cancelled;
        public CancellationToken Token;
        public TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource CancellationStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource ReleaseCleanup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int _completed;
        readonly HttpResponseMessage _response = new(HttpStatusCode.OK);

        protected override void Dispose(bool disposing)
        {
            if (disposing) _response.Dispose();
            base.Dispose(disposing);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Peak = Math.Max(Peak, Interlocked.Increment(ref Active));
            Token = cancellationToken;
            Entered.TrySetResult();
            try
            {
                await new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task.WaitAsync(cancellationToken);
                return _response;
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
