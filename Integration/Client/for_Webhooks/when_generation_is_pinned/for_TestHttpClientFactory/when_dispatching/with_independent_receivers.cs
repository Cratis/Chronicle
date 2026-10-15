// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Integration.for_Webhooks.given;

namespace Cratis.Chronicle.Integration.for_Webhooks.when_generation_is_pinned.for_TestHttpClientFactory.when_dispatching;

public class with_independent_receivers
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_dispatch_to_each_receiver_regardless_of_registration_order(bool pinnedFirst)
    {
        var factory = new TestHttpClientFactory();
        using var retainedClient = factory.CreateClient("webhook");
        using var pinnedReceiver = new Receiver(HttpStatusCode.BadRequest);
        using var legacyReceiver = new Receiver(HttpStatusCode.OK);
        var pinnedTarget = new Uri($"http://pinned-{Guid.NewGuid():N}.test/webhooks");
        var legacyTarget = new Uri($"http://legacy-{Guid.NewGuid():N}.test/webhooks");
        var firstTarget = pinnedFirst ? pinnedTarget : legacyTarget;
        var secondTarget = pinnedFirst ? legacyTarget : pinnedTarget;
        using var first = factory.Register(firstTarget, pinnedFirst ? pinnedReceiver : legacyReceiver);
        using var second = factory.Register(secondTarget, pinnedFirst ? legacyReceiver : pinnedReceiver);

        // This client predates both registrations, just as the mediator can retain the first factory.
        using var pinned = await retainedClient.GetAsync(pinnedTarget);
        using var legacy = await retainedClient.GetAsync(legacyTarget);
        pinned.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
        legacy.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }

    [Fact]
    public async Task should_normalize_host_and_default_port_consistently()
    {
        var factory = new TestHttpClientFactory();
        using var receiver = new Receiver(HttpStatusCode.OK);
        using var lease = factory.Register(new Uri("http://WEBHOOK.test:80/webhooks#fragment"), receiver);
        using var client = factory.CreateClient("webhook");
        using var response = await client.GetAsync("http://webhook.test/webhooks");
        response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    }

    sealed class Receiver(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status));
    }
}
