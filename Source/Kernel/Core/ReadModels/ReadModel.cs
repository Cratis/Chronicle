// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Orleans.Providers;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModel"/>.
/// </summary>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ReadModels)]
public class ReadModel : Grain<ReadModelDefinition>, IReadModel
{
    bool _stateNeedsWrite;

    /// <inheritdoc/>
    public async Task SetDefinition(ReadModelDefinition definition)
    {
        definition.Sink.EnsureReadModelSupported();
        if (!_stateNeedsWrite && State is not null && ReadModelDefinitionComparison.Equals(State, definition)) return;

        _stateNeedsWrite = true;
        State = definition;
        await WriteStateAsync();
        _stateNeedsWrite = false;
    }

    /// <inheritdoc/>
    public Task<ReadModelDefinition> GetDefinition() => Task.FromResult(State);
}
