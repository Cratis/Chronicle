// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookConnection.given;

public class a_pooled_connection : Specification
{
    protected int _connections;
    protected Exception _exception;
    protected IPAddress _address = IPAddress.Loopback;
    protected WebhookValidationMode _mode = WebhookValidationMode.BlockLocalAddresses;
    protected EventStoreName? _secondStore = "store-b";
    protected Version _version = HttpVersion.Version11;
    protected bool _approveFirstStore = true;
    protected bool _approveSecondStore;
    HttpClient _client;

    protected async Task SendAcrossStores()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        var options = new Configuration.Webhooks
        {
            Validation = _mode,
            EventStoreAllowedHosts = new Dictionary<string, string[]>
            {
                ["store-a"] = _approveFirstStore ? ["receiver.example"] : [],
                ["store-b"] = _approveSecondStore ? ["receiver.example"] : []
            }
        };
        var policy = new WebhookTargetPolicy(
            Options.Create(new ChronicleOptions { Webhooks = options }),
            environment,
            Substitute.For<IWebhookHostResolver>(),
            Substitute.For<ILogger<WebhookTargetPolicy>>());
        var connection = new WebhookConnection(Substitute.For<IWebhookHostResolver>(), policy);
        var primary = WebhookHttpClientExtensions.CreateHandler(connection);
        primary.ConnectCallback = (context, _) =>
        {
            _connections++;
            connection.ValidateRequest(context.InitialRequestMessage, [_address]);
            return ValueTask.FromResult<Stream>(new http_response_stream());
        };
        var dedicated = WebhookHttpClientExtensions.CreateHandler(connection);
        dedicated.ConnectCallback = primary.ConnectCallback;
        var pools = new WebhookConnectionPoolHandler(dedicated) { InnerHandler = primary };
        _client = new(new WebhookValidationHandler(policy, Substitute.For<ILogger<WebhookValidationHandler>>()) { InnerHandler = pools }) { Timeout = TimeSpan.FromSeconds(10) };
        await Send("store-a");
        _exception = await Catch.Exception(() => Send(_secondStore));
    }

    async Task Send(EventStoreName? store)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://receiver.example") { Version = _version };
        if (store is not null) request.Options.Set(WebhookRequestOptions.EventStore, store);
        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    void Destroy() => _client?.Dispose();
}
