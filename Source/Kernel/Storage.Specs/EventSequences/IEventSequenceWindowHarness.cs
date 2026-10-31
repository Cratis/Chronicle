// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Supplies an isolated event sequence for the shared window contract.
/// </summary>
public interface IEventSequenceWindowHarness : IDisposable
{
    /// <summary>
    /// Creates the storage to exercise using real writes and reads.
    /// </summary>
    /// <returns>The isolated event sequence storage.</returns>
    Task<IEventSequenceStorage> CreateStorage();
}
