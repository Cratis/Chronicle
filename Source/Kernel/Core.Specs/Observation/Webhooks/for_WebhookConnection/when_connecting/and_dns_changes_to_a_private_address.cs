// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookConnection.when_connecting;

public class and_dns_changes_to_a_private_address : for_WebhookTargetPolicy.given.a_target_policy
{
    HttpClient _client;
    Exception _error;

    void Establish()
    {
        Configure(new() { Validation = WebhookValidationMode.Enforce });
        _resolver.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([IPAddress.Parse("10.0.0.1")]);
        var connection = new WebhookConnection(_resolver, _policy);
        _client = new HttpClient(new SocketsHttpHandler { UseProxy = false, ConnectCallback = connection.Connect });
    }

    async Task Because()
    {
        try { await _client.GetAsync("https://receiver.example"); }
        catch (HttpRequestException error) { _error = error; }
    }

    void Destroy() => _client.Dispose();

    [Fact] void should_fail_before_opening_a_socket() => _error.ToString().ShouldContain("private or special");
    [Fact] void should_resolve_at_connection_time() => _resolver.Received(1).Resolve("receiver.example", Arg.Any<CancellationToken>());
}
