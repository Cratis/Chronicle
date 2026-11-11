// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class after_cancelling_a_discovery_waiter : given.a_controlled_oauth_client
{
    readonly TaskCompletionSource<HttpResponseMessage> _discovery = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _error;
    AccessTokenInfo _token;

    void Establish()
    {
        _endpoint.Responses.Enqueue(() => _discovery.Task);
        _endpoint.Responses.Enqueue(() => Task.FromResult(Token()));
    }

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var cancelled = _client.AcquireToken(_authorization, cancellation.Token);
        await cancellation.CancelAsync();
        _error = await Catch.Exception(() => cancelled);
        var next = _client.AcquireToken(_authorization);
        _discovery.SetResult(Discovery());
        _token = await next;
    }

    [Fact] void should_cancel_only_the_waiter() => _error.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_keep_the_pending_discovery() => _endpoint.Requests.ShouldEqual(2);
    [Fact] void should_allow_the_other_waiter_to_acquire_a_token() => _token.AccessToken.ShouldEqual("token");
}
