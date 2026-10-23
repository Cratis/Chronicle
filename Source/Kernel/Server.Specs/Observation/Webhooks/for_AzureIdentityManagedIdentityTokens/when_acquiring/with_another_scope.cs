// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Observation.Webhooks.for_AzureIdentityManagedIdentityTokens.when_acquiring;

public class with_another_scope : given.a_token_provider
{
    async Task Establish() => _first = await _provider.AcquireToken(_authorization, CancellationToken.None);

    async Task Because() => _result = await _provider.AcquireToken(_authorization with { Scope = "api://other/.default" }, CancellationToken.None);

    [Fact] void should_fetch_a_separate_token() => _credential.Requests.ShouldEqual(2);
    [Fact] void should_reuse_the_credential() => _factory.Received(1).Create(_authorization.ClientId);
}
