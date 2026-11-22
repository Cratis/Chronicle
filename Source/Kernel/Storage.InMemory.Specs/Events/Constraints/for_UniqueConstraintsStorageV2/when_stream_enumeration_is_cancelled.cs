// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_stream_enumeration_is_cancelled : given.a_seeded_v2_store
{
    Exception _originError;
    Exception _enumerationError;

    async Task Because()
    {
        using var source = new CancellationTokenSource();
        var originStream = await _storage.GetForStream(_source, _definition, _scope, source.Token);
        await source.CancelAsync();
        await using var origin = originStream.GetAsyncEnumerator();
        _originError = await Catch.Exception(async () => await origin.MoveNextAsync());
        var stream = await _storage.GetForStream(_source, _definition, _scope);
        await using var enumerator = stream.GetAsyncEnumerator(new CancellationToken(true));
        _enumerationError = await Catch.Exception(async () => await enumerator.MoveNextAsync());
    }

    [Fact] void should_honor_the_originating_token() => _originError.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_honor_the_enumeration_token() => _enumerationError.ShouldBeOfExactType<OperationCanceledException>();
}
