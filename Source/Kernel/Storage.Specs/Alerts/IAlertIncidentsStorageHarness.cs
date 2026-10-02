// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Supplies a real provider for the shared incident storage contract.
/// </summary>
public interface IAlertIncidentsStorageHarness : IAsyncDisposable
{
    /// <summary>
    /// Creates isolated namespace storage.
    /// </summary>
    /// <returns>The real provider.</returns>
    Task<IAlertIncidentsStorage> Create();
}
