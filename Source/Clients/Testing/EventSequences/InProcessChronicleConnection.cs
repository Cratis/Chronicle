// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-process implementation of <see cref="IChronicleConnection"/> and
/// <see cref="IChronicleServicesAccessor"/> for test scenarios.
/// </summary>
/// <remarks>
/// The connection is considered always-connected; <see cref="Connect"/> is a no-op.
/// <see cref="IChronicleServicesAccessor.Services"/> returns the <see cref="InProcessServices"/>
/// instance wired to the in-process kernel grain and storage.
/// </remarks>
/// <param name="services">The <see cref="IServices"/> backed by in-process implementations.</param>
internal sealed class InProcessChronicleConnection(IServices services) : IChronicleConnection, IChronicleServicesAccessor
{
    /// <inheritdoc/>
    public IConnectionLifecycle Lifecycle { get; } = new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance);

    /// <inheritdoc/>
    IServices IChronicleServicesAccessor.Services => services;

    /// <inheritdoc/>
    public Task Connect() => Task.CompletedTask;

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
