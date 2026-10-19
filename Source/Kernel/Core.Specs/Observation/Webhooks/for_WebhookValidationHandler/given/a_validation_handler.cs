// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookValidationHandler.given;

public class a_validation_handler : for_WebhookTargetPolicy.given.a_target_policy
{
    protected RecordingHandler _transport;
    protected ILogger<WebhookValidationHandler> _handlerLogger;
    protected HttpClient _client;
    protected HttpRequestMessage _request;
    protected Exception? _error;
    protected HttpResponseMessage? _response;

    void Establish() => _transport = new RecordingHandler();

    protected void CreateClient(string url)
    {
        _handlerLogger = Substitute.For<ILogger<WebhookValidationHandler>>();
        _handlerLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _client = new HttpClient(new WebhookValidationHandler(_policy, _handlerLogger) { InnerHandler = _transport });
        _request = new HttpRequestMessage(HttpMethod.Post, url);
    }

    protected async Task Send()
    {
        try { _response = await _client.SendAsync(_request); }
        catch (WebhookTargetRejected error) { _error = error; }
    }

    void Destroy()
    {
        _request?.Dispose();
        _response?.Dispose();
        _client?.Dispose();
    }

    protected class RecordingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(StatusCode));
        }
    }
}
