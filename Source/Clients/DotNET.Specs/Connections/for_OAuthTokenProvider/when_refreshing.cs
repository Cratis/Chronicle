// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider;

public class when_refreshing : given.an_oauth_token_provider
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _provider.Refresh().GetAwaiter().GetResult());

    [Fact] void should_attempt_to_get_new_token() => _exception.ShouldNotBeNull();
}
