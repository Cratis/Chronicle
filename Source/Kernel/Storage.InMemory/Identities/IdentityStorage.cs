// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.InMemory.Identities;

/// <summary>
/// Represents a no-op in-memory implementation of <see cref="IIdentityStorage"/>.
/// </summary>
/// <remarks>
/// Returns <see cref="Identity.System"/> for all identity lookups and a single
/// sentinel <see cref="IdentityId"/> for chain resolution. Observation and
/// population operations are no-ops.
/// </remarks>
public class IdentityStorage : IIdentityStorage
{
    static readonly IdentityId _systemIdentityId = IdentityId.New();

    /// <inheritdoc/>
    public Task Populate() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<bool> HasFor(IdentityId identityId) =>
        Task.FromResult(true);

    /// <inheritdoc/>
    public Task<IImmutableList<IdentityId>> GetFor(Identity identity) =>
        Task.FromResult<IImmutableList<IdentityId>>(
            ImmutableList.Create(_systemIdentityId));

    /// <inheritdoc/>
    public Task<Identity> GetFor(IEnumerable<IdentityId> chain) =>
        Task.FromResult(Identity.System);

    /// <inheritdoc/>
    public Task<IdentityId> GetSingleFor(Identity identity) =>
        Task.FromResult(_systemIdentityId);

    /// <inheritdoc/>
    public Task<Identity> GetSingleFor(IdentityId identityId) =>
        Task.FromResult(Identity.System);

    /// <inheritdoc/>
    public Task<IEnumerable<Identity>> GetAll() =>
        Task.FromResult(Enumerable.Empty<Identity>());

    /// <inheritdoc/>
    public ISubject<IEnumerable<Identity>> ObserveAll() =>
        new ReplaySubject<IEnumerable<Identity>>(1);
}
