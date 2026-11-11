// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class after_a_cancelled_token_fetch : given.a_controlled_oauth_client
{
    Exception _error;
    AccessTokenInfo _token;

    void Establish()
    {
        _endpoint.Responses.Enqueue(() => Task.FromResult(Discovery()));
        _endpoint.Responses.Enqueue(() => Task.FromCanceled<HttpResponseMessage>(new CancellationToken(true)));
        _endpoint.Responses.Enqueue(() => Task.FromResult(Token()));
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _client.AcquireToken(_authorization));
        _token = await _client.AcquireToken(_authorization);
    }

    [Fact] void should_preserve_the_cancellation_exception() => _error.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_retry_without_discovering_again() => _endpoint.Requests.ShouldEqual(3);
    [Fact] void should_acquire_a_token_after_recovering() => _token.AccessToken.ShouldEqual("token");
}
