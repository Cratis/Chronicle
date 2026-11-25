// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class after_invalidating_during_discovery : given.a_controlled_oauth_client
{
    readonly TaskCompletionSource<HttpResponseMessage> _discovery = new(TaskCreationOptions.RunContinuationsAsynchronously);
    AccessTokenInfo _first;
    AccessTokenInfo _second;

    void Establish()
    {
        _endpoint.Responses.Enqueue(() => _discovery.Task);
        _endpoint.Responses.Enqueue(() => Task.FromResult(Token()));
    }

    async Task Because()
    {
        _client.Invalidate(_authorization, AccessTokenInfo.Empty);
        var pending = _client.AcquireToken(_authorization);
        _client.Invalidate(_authorization, AccessTokenInfo.Empty);
        _discovery.SetResult(Discovery());
        _first = await pending;
        _second = await _client.AcquireToken(_authorization);
    }

    [Fact] void should_not_discard_the_pending_discovery() => _endpoint.Requests.ShouldEqual(2);
    [Fact] void should_reuse_the_completed_token() => ReferenceEquals(_first, _second).ShouldBeTrue();
}
