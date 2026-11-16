// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_getting_a_token_concurrently : given.a_caching_provider
{
    readonly TaskCompletionSource<string?> _acquisition = new(TaskCreationOptions.RunContinuationsAsynchronously);
    string?[] _results;

    void Establish() => _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns(_acquisition.Task);

    async Task Because()
    {
        var requests = Enumerable.Range(0, 16).Select(_ => _provider.GetAccessToken()).ToArray();
        _acquisition.SetResult("shared-token");
        _results = await Task.WhenAll(requests);
    }

    [Fact] void should_acquire_only_once() => _inner.Received(1).GetAccessToken(Arg.Any<CancellationToken>());
    [Fact] void should_return_the_same_token_to_every_caller() => _results.ShouldEachConformTo(token => token == "shared-token");
    [Fact] void should_leave_a_complete_cache_entry() => CacheProperty("AccessToken").ShouldEqual("shared-token");
    [Fact] void should_remove_temporary_files() => Directory.GetFiles(CacheDirectory).Length.ShouldEqual(1);
}
