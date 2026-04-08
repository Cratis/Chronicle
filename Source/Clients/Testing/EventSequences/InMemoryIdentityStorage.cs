// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Chronicle.Storage.Identities;
using KernelIdentities = KernelConcepts::Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a no-op in-memory implementation of <see cref="IIdentityStorage"/> for testing.
/// </summary>
/// <remarks>
/// Returns <see cref="KernelIdentities::Identity.System"/> for all identity lookups and a single
/// sentinel <see cref="KernelIdentities::IdentityId"/> for chain resolution. Observation and
/// population operations are no-ops.
/// </remarks>
internal sealed class InMemoryIdentityStorage : IIdentityStorage
{
    static readonly KernelIdentities::IdentityId _systemIdentityId = KernelIdentities::IdentityId.New();

    /// <inheritdoc/>
    public Task Populate() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<bool> HasFor(KernelIdentities::IdentityId identityId) =>
        Task.FromResult(true);

    /// <inheritdoc/>
    public Task<IImmutableList<KernelIdentities::IdentityId>> GetFor(KernelIdentities::Identity identity) =>
        Task.FromResult<IImmutableList<KernelIdentities::IdentityId>>(
            ImmutableList.Create(_systemIdentityId));

    /// <inheritdoc/>
    public Task<KernelIdentities::Identity> GetFor(IEnumerable<KernelIdentities::IdentityId> chain) =>
        Task.FromResult(KernelIdentities::Identity.System);

    /// <inheritdoc/>
    public Task<KernelIdentities::IdentityId> GetSingleFor(KernelIdentities::Identity identity) =>
        Task.FromResult(_systemIdentityId);

    /// <inheritdoc/>
    public Task<KernelIdentities::Identity> GetSingleFor(KernelIdentities::IdentityId identityId) =>
        Task.FromResult(KernelIdentities::Identity.System);

    /// <inheritdoc/>
    public Task<IEnumerable<KernelIdentities::Identity>> GetAll() =>
        Task.FromResult(Enumerable.Empty<KernelIdentities::Identity>());

    /// <inheritdoc/>
    public ISubject<IEnumerable<KernelIdentities::Identity>> ObserveAll() =>
        new Subject<IEnumerable<KernelIdentities::Identity>>();
}
