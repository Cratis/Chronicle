// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

/// <summary>
/// Defines the provider-independent v2 claim contract. Each fact owns a fresh namespace and store.
/// Provider adapters explicitly construct the dormant implementation; runtime discovery is not involved.
/// </summary>
[PerFactSpecificationLifecycle]
public abstract partial class UniqueConstraintsStorageV2Contract : Specification
{
    protected IUniqueConstraintsStorageV2 _storage;
    protected readonly UniqueConstraintDefinition _definition = new("constraint", []);
    protected readonly UniqueConstraintStorageScope _scope = new(new string('0', 32));
    protected readonly UniqueConstraintStorageScope _otherScope = new(new string('1', 32));
    protected readonly UniqueConstraintValue _value = new(new string('a', 64));
    protected readonly UniqueConstraintValue _otherValue = new(new string('b', 64));
    protected readonly EventSourceId _owner = "owner";
    protected readonly EventSourceId _otherOwner = "other-owner";
    protected readonly Subject _subject = "subject";
    protected readonly UniqueConstraintWriteAuthority _authority = new(new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")), UniqueConstraintWritePhase.Live, new(0));

    protected UniqueConstraintDefinition PerValue => _definition with { Mode = UniqueConstraintMode.PerValue };

    /// <summary>
    /// Constructs a fresh store for a single fact, with no authority or claims.
    /// </summary>
    /// <returns>The explicitly constructed provider.</returns>
    protected abstract Task<IUniqueConstraintsStorageV2> CreateStorage();

    /// <summary>
    /// Releases provider resources belonging to this fact, after all snapshots have been consumed.
    /// </summary>
    /// <returns>The cleanup operation.</returns>
    protected virtual Task DestroyStorage() => Task.CompletedTask;

    /// <summary>
    /// Reopens an independent provider handle. Volatile providers retain their in-memory backing store.
    /// </summary>
    /// <returns>A handle to the same namespace and sequence.</returns>
    protected virtual Task<IUniqueConstraintsStorageV2> ReopenStorage() => Task.FromResult(_storage);

    /// <summary>
    /// Arranges a transition around a mutation. SQL adapters pause at transaction boundaries on separate connections.
    /// </summary>
    /// <param name="transition">The authority change.</param>
    /// <param name="mutation">The claim change.</param>
    /// <param name="transitionFirst">Whether the transition must commit before the mutation acquires its fence.</param>
    /// <returns>The mutation result.</returns>
    protected virtual async Task<Result<bool, UniqueConstraintWriteFailure>> Interleave(Func<Task> transition, Func<Task<Result<bool, UniqueConstraintWriteFailure>>> mutation, bool transitionFirst)
    {
        if (transitionFirst)
        {
            await transition();
            return await mutation();
        }

        var result = await mutation();
        await transition();
        return result;
    }

    async Task Establish() => _storage = await CreateStorage();

    async Task Destroy() => await DestroyStorage();

    protected async Task Initialize() => (await _storage.InitializeAuthority(_definition.Name, _authority)).IsSuccess.ShouldBeTrue();

    protected async Task Seed(UniqueConstraintDefinition? definition = null)
    {
        await Initialize();
        (await _storage.Save(_owner, definition ?? _definition, 10, _value, _scope, _authority, _subject)).IsSuccess.ShouldBeTrue();
    }

    protected Task<Result<bool, UniqueConstraintWriteFailure>> Mutation(int operation, UniqueConstraintWriteAuthority proof, CancellationToken cancellationToken = default) => operation switch
    {
        0 => _storage.Save(_owner, _definition, 20, _otherValue, _scope, proof, cancellationToken: cancellationToken),
        1 => _storage.Remove(_owner, _definition, _scope, proof, cancellationToken),
        2 => _storage.RemoveValue(_owner, PerValue, _value, _scope, proof, cancellationToken),
        3 => _storage.ClearValues(_definition, _scope, proof, cancellationToken),
        4 => _storage.RemoveAllFor(_definition, proof, cancellationToken),
        5 => _storage.RemoveForSubject(_definition, _subject, proof, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    protected static UniqueConstraintWritePhase Run(int suffix) => UniqueConstraintWritePhase.Run(new(new Guid(suffix, 0, 0, new byte[8])));

    protected static void ShouldRefuse<T>(Result<T, UniqueConstraintWriteFailure> result, UniqueConstraintWriteFailure failure)
    {
        result.IsSuccess.ShouldBeFalse();
        result.AsT1.ShouldEqual(failure);
    }

    protected async Task ShouldRetainClaim(UniqueConstraintDefinition? definition = null)
    {
        var selected = definition ?? _definition;
        (await _storage.GetFor(_owner, selected, _scope)).SequenceEqual([_value]).ShouldBeTrue();
        (await _storage.IsAllowed(_owner, selected, _value, _scope)).SequenceNumber.ShouldEqual((EventSequenceNumber)10);
    }
}
