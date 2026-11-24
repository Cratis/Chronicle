// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

public abstract partial class UniqueConstraintsStorageV2Contract
{
    [Fact]
    public async Task should_reject_invalid_value_tokens_before_mutation()
    {
        await Seed();
        foreach (var value in new UniqueConstraintValue[] { "plaintext", new(new string('A', 64)), new(new string('a', 63)), new(new string('a', 65)) })
        {
            await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.Save(_owner, _definition, 99, value, _scope, _authority));
            await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.RemoveValue(_owner, PerValue, value, _scope, _authority));
            await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.IsAllowed(_owner, _definition, value, _scope));
        }

        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_reject_null_proofs_for_every_mutation()
    {
        await Seed();
        for (var operation = 0; operation < 6; operation++)
        {
            await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => Mutation(operation, null!));
        }

        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_reject_null_authority_inputs_without_initializing_authority()
    {
        await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.InitializeAuthority(_definition.Name, null!));
        await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.InitializeAuthority(_definition.Name, _authority with { Phase = null! }));
        await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.TryTransition(_definition.Name, null!, Run(1)));
        await Assert.ThrowsAsync<InvalidUniqueConstraintV2Input>(() => _storage.TryTransition(_definition.Name, _authority, null!));
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual(Option<UniqueConstraintWriteAuthority>.None());
    }

    [Fact]
    public async Task should_cancel_every_mutation_before_changing_claims()
    {
        await Seed();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        for (var operation = 0; operation < 6; operation++)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Mutation(operation, _authority, cancellation.Token));
        }

        await ShouldRetainClaim();
        await ShouldRetainAuthority();
    }

    [Fact]
    public async Task should_cancel_authority_operations_before_writing()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.GetAuthority(_definition.Name, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.InitializeAuthority(_definition.Name, _authority, cancellation.Token));
        await Initialize();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.TryTransition(_definition.Name, _authority, Run(1), cancellation.Token));
        await ShouldRetainAuthority();
    }

    [Fact]
    public async Task should_cancel_reads_without_changing_claims()
    {
        await Seed();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.IsAllowed(_owner, _definition, _value, _scope, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.GetFor(_owner, _definition, _scope, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _storage.GetForStream(_owner, _definition, _scope, cancellation.Token));
        await ShouldRetainClaim();
    }
}
