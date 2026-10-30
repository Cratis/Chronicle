// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Observation.Webhooks.for_AzureIdentityManagedIdentityTokens.when_acquiring;

public class with_concurrent_callers : given.a_token_provider
{
    void Establish() => _credential.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Because()
    {
        var first = _provider.AcquireToken(_authorization, CancellationToken.None);
        var second = _provider.AcquireToken(_authorization, CancellationToken.None);
        _credential.Release!.SetResult();
        _first = await first;
        _result = await second;
    }

    [Fact] void should_share_one_request() => _credential.Requests.ShouldEqual(1);
    [Fact] void should_share_the_result() => _result.ShouldBeSame(_first);
}
