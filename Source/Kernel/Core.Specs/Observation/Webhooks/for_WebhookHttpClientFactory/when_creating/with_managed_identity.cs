// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookHttpClientFactory.when_creating;

public class with_managed_identity : Specification
{
    IManagedIdentityTokens _tokens;
    ManagedIdentityAuthorization _authorization;
    string? _sentToken;

    void Establish()
    {
        _authorization = new("client", "api://resource/.default");
        _tokens = Substitute.For<IManagedIdentityTokens>();
        _tokens.AcquireToken(_authorization, Arg.Any<CancellationToken>()).Returns(new AccessTokenInfo("managed-token", DateTimeOffset.MaxValue));
    }

    async Task Because()
    {
        var transport = Substitute.For<IHttpClientFactory>();
        using var inner = new HttpClient(new Receiver(value => _sentToken = value));
        transport.CreateClient(Arg.Any<string>()).Returns(inner);
        var policy = Substitute.For<IWebhookTargetPolicy>();
        policy.CheckManagedIdentity(_authorization, Arg.Any<string>(), Arg.Any<Concepts.EventStoreName?>()).Returns(WebhookTargetValidation.Allowed);
        var factory = new WebhookHttpClientFactory(transport, Substitute.For<IOAuthClient>(), _tokens, policy);
        using var client = factory.Create(new("https://target.example", _authorization, new Dictionary<string, string>()));
        using var response = await client.GetAsync("/");
    }

    [Fact] void should_send_the_bearer_token() => _sentToken.ShouldEqual("managed-token");
    [Fact] async Task should_use_the_managed_identity_provider() => await _tokens.Received(1).AcquireToken(_authorization, Arg.Any<CancellationToken>());

    class Receiver(Action<string?> receive) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            receive(request.Headers.Authorization?.Parameter);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
