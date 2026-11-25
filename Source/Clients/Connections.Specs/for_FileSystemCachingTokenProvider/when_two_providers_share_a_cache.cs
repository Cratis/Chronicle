// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_two_providers_share_a_cache : given.a_caching_provider
{
    readonly TaskCompletionSource<string?> _acquisition = new(TaskCreationOptions.RunContinuationsAsynchronously);
    FileSystemCachingTokenProvider _other;
    string?[] _results;

    void Establish()
    {
        _other = new(_inner, Path.Combine(CacheDirectory, ".", "token.json"));
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns(_acquisition.Task);
    }

    async Task Because()
    {
        var first = _provider.GetAccessToken();
        var second = _other.GetAccessToken();
        _acquisition.SetResult("shared-token");
        _results = await Task.WhenAll(first, second);
    }

    void Destroy() => _other.Dispose();

    [Fact] void should_acquire_only_once() => _inner.Received(1).GetAccessToken(Arg.Any<CancellationToken>());
    [Fact] void should_return_the_same_token() => _results.ShouldEachConformTo(token => token == "shared-token");
    [Fact] void should_leave_valid_json() => CacheProperty("AccessToken").ShouldEqual("shared-token");
    [Fact] void should_remove_temporary_files() => Directory.GetFiles(CacheDirectory).Length.ShouldEqual(1);
}
