// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Matches the legacy benchmark's system identity for every stored chain, without recording calls.</summary>
internal sealed class BenchmarkIdentities : IIdentityStorage
{
    readonly Task<Identity> _system = Task.FromResult(Identity.System);

    /// <inheritdoc/>
    public Task<Identity> GetFor(IEnumerable<IdentityId> chain) => _system;

    /// <inheritdoc/>
    public Task Populate() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<bool> HasFor(IdentityId identityId) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<IImmutableList<IdentityId>> GetFor(Identity identity) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<IdentityId> GetSingleFor(Identity identity) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<Identity> GetSingleFor(IdentityId identityId) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task Rename(string subject, string name) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<IEnumerable<Identity>> GetAll() => throw new NotSupportedException();

    /// <inheritdoc/>
    public ISubject<IEnumerable<Identity>> ObserveAll() => throw new NotSupportedException();
}
