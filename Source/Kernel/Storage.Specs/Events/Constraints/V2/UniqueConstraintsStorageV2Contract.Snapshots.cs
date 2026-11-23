// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

public abstract partial class UniqueConstraintsStorageV2Contract
{
    [Fact]
    public async Task should_detach_an_enumerable_snapshot_from_later_mutations()
    {
        await Seed();
        var snapshot = await _storage.GetFor(_owner, _definition, _scope);
        (await _storage.RemoveAllFor(_definition, _authority)).IsSuccess.ShouldBeTrue();
        snapshot.ShouldContainOnly(_value);
        (await _storage.GetFor(_owner, _definition, _scope)).ShouldBeEmpty();
    }

    [Fact]
    public async Task should_snapshot_a_stream_before_enumeration()
    {
        await Seed();
        var stream = await _storage.GetForStream(_owner, _definition, _scope);
        (await _storage.RemoveAllFor(_definition, _authority)).IsSuccess.ShouldBeTrue();
        (await Values(stream)).ShouldContainOnly(_value);
    }

    [Fact]
    public async Task should_honor_the_read_token_during_stream_enumeration()
    {
        await Seed();
        using var cancellation = new CancellationTokenSource();
        var stream = await _storage.GetForStream(_owner, _definition, _scope, cancellation.Token);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Values(stream));
        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_honor_the_enumerator_token_during_stream_enumeration()
    {
        await Seed();
        var stream = await _storage.GetForStream(_owner, _definition, _scope);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Values(stream, cancellation.Token));
        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_not_expose_a_mutable_backing_collection()
    {
        await Seed();
        var snapshot = await _storage.GetFor(_owner, _definition, _scope);
        if (snapshot is UniqueConstraintValue[] values)
        {
            values[0] = _otherValue;
        }

        await ShouldRetainClaim();
    }

    static async Task<UniqueConstraintValue[]> Values(IAsyncEnumerable<UniqueConstraintValue> stream, CancellationToken cancellationToken = default)
    {
        var values = new List<UniqueConstraintValue>();
        await foreach (var value in stream.WithCancellation(cancellationToken))
        {
            values.Add(value);
        }

        return values.ToArray();
    }
}
