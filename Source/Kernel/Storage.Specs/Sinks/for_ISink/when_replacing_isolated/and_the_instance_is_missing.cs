// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

public abstract class and_the_instance_is_missing<THarness> : given.an_isolated_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    IsolatedReplaceResult _result;

    async Task Because() => _result = await _sink.ReplaceIsolated(_key, State(20), 20);

    [Fact] public void should_replace() => _result.ShouldEqual(IsolatedReplaceResult.Replaced);
    [Fact] public async Task should_store_the_complete_state() => (await CurrentCount()).ShouldEqual(20);
}
