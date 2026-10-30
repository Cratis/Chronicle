// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Core;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Server.Observation.Webhooks.for_AzureIdentityManagedIdentityTokens.when_acquiring;

public class with_a_rejected_token_cached_by_the_credential : given.a_token_provider
{
    int _credentials;

    async Task Establish()
    {
        _factory.Create(Arg.Any<ManagedIdentityClientId>()).Returns(_ => new CachingCredential($"credential-{++_credentials}", _clock));
        _first = await _provider.AcquireToken(_authorization, CancellationToken.None);
        _provider.Invalidate(_authorization, _first);
    }

    async Task Because()
    {
        _result = await _provider.AcquireToken(_authorization, CancellationToken.None);
        _provider.Invalidate(_authorization, _first);
        _result = await _provider.AcquireToken(_authorization, CancellationToken.None);
    }

    [Fact] void should_create_a_new_credential_for_the_retry() => _credentials.ShouldEqual(2);
    [Fact] void should_not_reuse_the_credential_cached_rejected_token() => _result.AccessToken.ShouldEqual("credential-2");
    [Fact] void should_keep_the_original_token_separate() => _first.AccessToken.ShouldEqual("credential-1");
    [Fact] void should_keep_the_new_credential_after_a_stale_invalidation() => _factory.Received(2).Create(_authorization.ClientId);

    class CachingCredential(string token, Clock clock) : TokenCredential
    {
        readonly AccessToken _cached = new(token, clock.GetUtcNow().AddHours(1));

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => _cached;
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) => ValueTask.FromResult(_cached);
    }
}
