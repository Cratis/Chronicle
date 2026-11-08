// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Storage;
using Orleans.Storage;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for <see cref="ImportGrainState"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class ImportGrainStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var key = ImportKey.Parse(grainId.Key.ToString()!);
        await storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Imports.Remove(key.ImportId);
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var key = ImportKey.Parse(grainId.Key.ToString()!);
        var actual = (grainState as IGrainState<ImportGrainState>)!;
        var record = await storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Imports.Get(key.ImportId);
        actual.State = new() { Record = record };
        actual.RecordExists = record is not null;
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var key = ImportKey.Parse(grainId.Key.ToString()!);
        var actual = (grainState as IGrainState<ImportGrainState>)!;
        if (actual.State?.Record is not { } record) return;
        await storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Imports.Save(record);
        actual.RecordExists = true;
    }
}
