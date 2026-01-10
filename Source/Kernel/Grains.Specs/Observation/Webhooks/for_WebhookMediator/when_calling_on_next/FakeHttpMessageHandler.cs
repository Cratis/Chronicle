// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    readonly HttpResponseMessage? _response;
    readonly Exception? _exception;

    public int RequestsSent { get; private set; }

    public FakeHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    public FakeHttpMessageHandler(Exception exception)
    {
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestsSent++;
        if (_exception is not null)
        {
            throw _exception;
        }
        return Task.FromResult(_response!);
    }
}
