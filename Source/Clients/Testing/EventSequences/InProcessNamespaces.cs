// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using KernelCore::Cratis.Chronicle.Namespaces;
using KernelIncarnation = KernelConcepts::Cratis.Chronicle.Concepts.NamespaceIncarnationId;
using KernelName = KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-process <see cref="INamespaces"/> for the single-process test harness, where every namespace exists
/// as soon as it is named and there is no registry to keep.
/// </summary>
internal sealed class InProcessNamespaces : INamespaces
{
    /// <inheritdoc/>
    public Task EnsureDefault() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Ensure(KernelName @namespace) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<KernelName> EnsureCanonical(KernelName @namespace) => Task.FromResult(@namespace);

    /// <inheritdoc/>
    public Task<KernelName?> GetCanonical(KernelName @namespace) => Task.FromResult<KernelName?>(@namespace);

    /// <inheritdoc/>
    public Task<IEnumerable<KernelName>> GetAll() => Task.FromResult<IEnumerable<KernelName>>([]);

    /// <inheritdoc/>
    public Task Forget(KernelName @namespace) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ForgetIncarnation(KernelName @namespace, KernelIncarnation incarnation, Guid operation) => Task.CompletedTask;
}
