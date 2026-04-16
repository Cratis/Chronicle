// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents an implementation of <see cref="IChronicleConnection"/> for testing.
/// </summary>
/// <param name="readModelsService">The <see cref="InProcessReadModelsService"/> to expose via <see cref="IChronicleServicesAccessor.Services"/>.</param>
internal sealed class ChronicleConnectionForTesting(InProcessReadModelsService readModelsService) : IChronicleConnection, IChronicleServicesAccessor
{
    readonly TestingServices _services = new(readModelsService);

    /// <inheritdoc/>
    public IConnectionLifecycle Lifecycle { get; } = new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance);

    /// <inheritdoc/>
    IServices IChronicleServicesAccessor.Services => _services;

    /// <inheritdoc/>
    public Task Connect() => Task.CompletedTask;

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
