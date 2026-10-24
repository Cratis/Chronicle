// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Core;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Server.Observation.Webhooks.for_AzureIdentityManagedIdentityTokens.given;

public class a_token_provider : Specification
{
    protected AzureIdentityManagedIdentityTokens _provider;
    protected IManagedIdentityCredentialFactory _factory;
    protected Credential _credential;
    protected Clock _clock;
    protected ManagedIdentityAuthorization _authorization;
    protected AccessTokenInfo _first;
    protected AccessTokenInfo _result;

    void Establish()
    {
        _clock = new();
        _credential = new(_clock);
        _factory = Substitute.For<IManagedIdentityCredentialFactory>();
        _factory.Create(Arg.Any<ManagedIdentityClientId>()).Returns(_credential);
        _provider = new(_factory, _clock);
        _authorization = new("client", "api://resource/.default");
    }

    protected class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    protected class Credential(Clock clock) : TokenCredential
    {
        public int Requests;
        public string[] Scopes = [];
        public TaskCompletionSource? Release;
        public bool Fail;

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => throw new ManagedIdentityNotAvailable();

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            var number = Interlocked.Increment(ref Requests);
            Scopes = requestContext.Scopes;
            if (Release is not null) await Release.Task.WaitAsync(cancellationToken);
            if (Fail) throw new ManagedIdentityNotAvailable();
            return new($"token-{number}", clock.GetUtcNow().AddHours(1));
        }
    }
}
