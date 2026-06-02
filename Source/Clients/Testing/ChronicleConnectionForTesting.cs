// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents an implementation of <see cref="IChronicleConnection"/> for testing.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for grain-based operations.</param>
/// <param name="storage">The <see cref="IStorage"/> backed by in-memory implementations.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> for serialization.</param>
internal sealed class ChronicleConnectionForTesting(
    IGrainFactory grainFactory,
    IStorage storage,
    JsonSerializerOptions jsonSerializerOptions) : IChronicleConnection, IChronicleServicesAccessor
{
    readonly TestingServices _services = new(grainFactory, storage, jsonSerializerOptions);

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
