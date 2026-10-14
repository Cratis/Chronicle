// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Integration.for_Webhooks.given;

namespace Cratis.Chronicle.Integration.for_Webhooks.when_generation_is_pinned.for_TestHttpClientFactory.when_releasing;

public class with_pending_and_later_receivers
{
    [Fact]
    public async Task should_not_route_an_expired_target_to_a_later_receiver()
    {
        var factory = new TestHttpClientFactory();
        using var receiver = new Receiver();
        var oldTarget = new Uri($"http://old-{Guid.NewGuid():N}.test/webhooks");
        var newTarget = new Uri($"http://new-{Guid.NewGuid():N}.test/webhooks");
        using var oldLease = factory.Register(oldTarget, receiver);
        oldLease.Dispose();
        using var newLease = factory.Register(newTarget, receiver);
        using var client = factory.CreateClient("webhook");
        using var response = await client.GetAsync(oldTarget);
        response.StatusCode.ShouldEqual(HttpStatusCode.Gone);
    }

    [Fact]
    public async Task should_not_remove_a_new_lease_on_repeated_old_cleanup()
    {
        var factory = new TestHttpClientFactory();
        using var receiver = new Receiver();
        var target = new Uri($"http://webhook-{Guid.NewGuid():N}.test/webhooks");
        using var oldLease = factory.Register(target, receiver);
        oldLease.Dispose();
        using var newLease = factory.Register(target, receiver);
        oldLease.Dispose();
        using var client = factory.CreateClient("webhook");
        using var response = await client.GetAsync(target);
        response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }

    [Fact]
    public async Task should_cancel_a_pending_request_on_cleanup()
    {
        var factory = new TestHttpClientFactory();
        using var receiver = new PendingReceiver();
        var target = new Uri($"http://webhook-{Guid.NewGuid():N}.test/webhooks");
        using var lease = factory.Register(target, receiver);
        using var client = factory.CreateClient("webhook");
        var pending = client.GetAsync(target);
        await receiver.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        lease.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    sealed class Receiver : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    sealed class PendingReceiver : HttpMessageHandler
    {
        readonly TaskCompletionSource<HttpResponseMessage> _response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Started.SetResult();
            return _response.Task.WaitAsync(cancellationToken);
        }
    }
}
