// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Observation.Webhooks.for_AzureIdentityManagedIdentityTokens.when_acquiring;

public class with_cached_token : given.a_token_provider
{
    async Task Establish() => _first = await _provider.AcquireToken(_authorization, CancellationToken.None);

    async Task Because() => _result = await _provider.AcquireToken(_authorization, CancellationToken.None);

    [Fact] void should_reuse_the_token() => _result.ShouldBeSame(_first);
    [Fact] void should_only_fetch_once() => _credential.Requests.ShouldEqual(1);
    [Fact] void should_pass_the_scope() => _credential.Scopes.ShouldContainOnly("api://resource/.default");
}
