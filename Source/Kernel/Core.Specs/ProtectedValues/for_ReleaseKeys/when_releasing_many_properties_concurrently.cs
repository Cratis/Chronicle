// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_releasing_many_properties_concurrently : given.a_key_store
{
    readonly TaskCompletionSource<EncryptionKeyErasure?> _fence = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EncryptionKey?[] _results;

    void Establish() => _keys.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(_fence.Task);

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope();
        var properties = Enumerable.Range(0, 100).Select(_ => Release(_keys)).ToArray();
        _fence.SetResult(null);
        _results = await Task.WhenAll(properties);
    }

    [Fact] void should_read_the_fence_once() => _keys.Received(1).GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
    [Fact] void should_release_every_property() => _results.All(key => key == _pair.Key).ShouldBeTrue();
}
