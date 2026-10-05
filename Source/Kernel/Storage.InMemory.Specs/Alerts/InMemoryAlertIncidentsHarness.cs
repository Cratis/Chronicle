// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Storage.InMemory.Alerts;

/// <summary>
/// Supplies isolated in-memory incident storage.
/// </summary>
public class InMemoryAlertIncidentsHarness : IAlertIncidentsStorageHarness
{
    /// <inheritdoc/>
    public Task<IAlertIncidentsStorage> Create() => Task.FromResult<IAlertIncidentsStorage>(new AlertIncidentsStorage());

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
