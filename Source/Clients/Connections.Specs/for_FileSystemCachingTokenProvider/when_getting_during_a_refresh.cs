// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_getting_during_a_refresh : given.a_caching_provider
{
    readonly TaskCompletionSource<string?> _refresh = new(TaskCreationOptions.RunContinuationsAsynchronously);
    string? _result;

    void Establish() => _inner.Refresh(Arg.Any<CancellationToken>()).Returns(_refresh.Task);

    async Task Because()
    {
        var refresh = _provider.Refresh();
        var get = _provider.GetAccessToken();
        _refresh.SetResult("refreshed-token");
        await refresh;
        _result = await get;
    }

    [Fact] void should_wait_for_the_refreshed_token() => _result.ShouldEqual("refreshed-token");
    [Fact] void should_not_start_another_acquisition() => _inner.DidNotReceive().GetAccessToken(Arg.Any<CancellationToken>());
    [Fact] void should_leave_valid_json() => CacheProperty("AccessToken").ShouldEqual("refreshed-token");
}
