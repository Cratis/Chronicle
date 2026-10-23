// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_delivering;

public class and_managed_identity_token_fails : Specification
{
    WebhookDeliveryResult _result;

    async Task Because()
    {
        var transport = Substitute.For<IHttpClientFactory>();
        using var inner = new HttpClient();
        transport.CreateClient(Arg.Any<string>()).Returns(inner);
        var factory = new WebhookHttpClientFactory(transport, Substitute.For<IOAuthClient>(), new ManagedIdentityTokens());
        var mediator = new WebhookMediator(factory, new JsonSerializerOptions());
        var target = new WebhookTarget("https://target.example", new ManagedIdentityAuthorization("", "api://resource/.default"), new Dictionary<string, string>());
        _result = await mediator.Deliver("store", target, "partition", []);
    }

    [Fact] void should_fail_delivery() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_token_failure() => _result.Error.ShouldBeOfExactType<ManagedIdentityNotAvailable>();
}
