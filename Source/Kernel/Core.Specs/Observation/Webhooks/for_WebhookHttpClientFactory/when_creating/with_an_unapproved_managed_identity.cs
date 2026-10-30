// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookHttpClientFactory.when_creating;

public class with_an_unapproved_managed_identity : for_WebhookTargetPolicy.given.a_target_policy
{
    IManagedIdentityTokens _tokens;
    Exception? _error;
    int _requests;

    void Establish()
    {
        Configure(new()
        {
            AllowedHosts = ["target.example"],
            ManagedIdentity = new() { AllowedScopes = ["api://approved/.default"], AllowedClientIds = ["client"] }
        });
        _tokens = Substitute.For<IManagedIdentityTokens>();
    }

    async Task Because()
    {
        var transport = Substitute.For<IHttpClientFactory>();
        using var inner = new HttpClient(new Receiver(() => _requests++));
        transport.CreateClient(Arg.Any<string>()).Returns(inner);
        var factory = new WebhookHttpClientFactory(transport, Substitute.For<IOAuthClient>(), _tokens, _policy);
        using var client = factory.Create(new("https://target.example", new ManagedIdentityAuthorization("client", "https://management.azure.com/.default"), new Dictionary<string, string>()));
        _error = await Catch.Exception(async () => { using var response = await client.GetAsync("/"); });
    }

    [Fact] void should_fail_delivery() => _error.ShouldBeOfExactType<ManagedIdentityWebhookNotAllowed>();
    [Fact] async Task should_not_mint_a_token() => await _tokens.DidNotReceive().AcquireToken(Arg.Any<ManagedIdentityAuthorization>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_send_anything() => _requests.ShouldEqual(0);

    class Receiver(Action received) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            received();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
