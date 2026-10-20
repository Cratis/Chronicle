// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_cancellation_during_http_retry : given.a_webhook_mediator
{
    readonly CancellationTokenSource _cancellation = new();
    ServiceProvider _services;
    int _requests;
    Cratis.Monads.Catch _result;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddHttpClient(WebhookHttpClientFactory.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new FailingReceiver(() =>
            {
                _requests++;
                _cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
            }))
            .AddResilienceHandler("cancellation-spec", pipeline => pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                Delay = TimeSpan.FromSeconds(5),
                UseJitter = false,
                MaxRetryAttempts = 3
            }));
        _services = services.BuildServiceProvider();
        _mediator = new WebhookMediator(new WebhookHttpClientFactory(_services.GetRequiredService<IHttpClientFactory>(), Substitute.For<IOAuthClient>()), _jsonOptions);
    }

    async Task Because()
    {
        var target = new WebhookTarget(new("https://example.com/webhook"), WebhookAuthorization.None, new Dictionary<string, string>());
        _result = await _mediator.OnNext(target, "partition", [], cancellationToken: _cancellation.Token);
    }

    void Destroy()
    {
        _services.Dispose();
        _cancellation.Dispose();
    }

    [Fact] void should_cancel_the_retry_delay() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_send_another_request() => _requests.ShouldEqual(1);

    class FailingReceiver(Action received) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            received();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }
}
