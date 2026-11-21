// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>
/// A running storage provider (a container or a file) that the benchmarks build real Chronicle storage on.
/// </summary>
internal interface IStorageProviderHost : IAsyncDisposable
{
    /// <summary>Gets the provider.</summary>
    StorageProvider Provider { get; }

    /// <summary>Gets the counter that sees every command sent to the database.</summary>
    CommandCounter Counter { get; }

    /// <summary>Starts the database.</summary>
    /// <returns>The startup task.</returns>
    Task Start();

    /// <summary>Creates a fresh, empty event sequence so cases never see each other's rows.</summary>
    /// <returns>The storage over the new sequence.</returns>
    IEventSequenceStorage CreateSequence();

    /// <summary>Creates a fresh unique-constraint storage.</summary>
    /// <returns>The storage and the definition it governs.</returns>
    Task<(IUniqueConstraintsStorage Storage, UniqueConstraintDefinition Definition)> CreateConstraints();

    /// <summary>Reads the settings that decide durability from the server and builds the ledger.</summary>
    /// <param name="options">The run options.</param>
    /// <param name="loadAtStart">The machine load when the run started.</param>
    /// <returns>The ledger.</returns>
    Task<SettingsLedger> BuildLedger(StorageRunOptions options, LoadReading loadAtStart);
}
