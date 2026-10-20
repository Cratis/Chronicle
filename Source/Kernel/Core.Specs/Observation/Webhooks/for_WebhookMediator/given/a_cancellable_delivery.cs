// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.given;

public class a_cancellable_delivery : a_webhook_mediator
{
    protected WebhookTarget _target;
    protected bool _cancelled;
    protected Cratis.Monads.Catch _result;

    void Establish()
    {
        _target = new(new WebhookTargetUrl("https://example.com/webhook"), WebhookAuthorization.None, new Dictionary<string, string>());
        var client = new HttpClient(new CancellableReceiver(() => _cancelled = true)) { BaseAddress = new Uri(_target.Url) };
        _httpClientFactory.Create(_target).Returns(client);
    }

    class CancellableReceiver(Action cancelled) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                await new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task.WaitAsync(cancellationToken);
                return new(System.Net.HttpStatusCode.OK);
            }
            finally
            {
                cancelled();
            }
        }
    }
}
