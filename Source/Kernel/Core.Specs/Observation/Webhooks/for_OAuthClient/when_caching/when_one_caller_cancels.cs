// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class when_one_caller_cancels : given.a_cached_oauth_client
{
    bool _cancelled;
    AccessTokenInfo _second;

    void Establish() => _endpoint.Block = true;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var first = _client.AcquireToken(_authorization, cancellation.Token);
        var second = _client.AcquireToken(_authorization);
        await cancellation.CancelAsync();
        try
        {
            await first;
        }
        catch (OperationCanceledException)
        {
            _cancelled = true;
        }

        _release.SetResult();
        _second = await second;
    }

    [Fact] void should_cancel_only_first_wait() => _cancelled.ShouldBeTrue();
    [Fact] void should_finish_second_wait() => _second.AccessToken.ShouldEqual("token-1");
    [Fact] void should_share_fetch() => _endpoint.TokenRequests.ShouldEqual(1);
}
