// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Storage.InMemory.Generations;

/// <summary>Atomic in-memory floor and audit storage with the same CAS semantics as durable providers.</summary>
public sealed class GenerationFloorStorage : IGenerationFloorStorage
{
    readonly Lock _lock = new();
    readonly List<GenerationFloor> _history = [];
    GenerationFloor? _floor;

    /// <inheritdoc/>
    public Task<GenerationFloor?> Get()
    {
        lock (_lock)
        {
            return Task.FromResult(_floor);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<GenerationFloor>> GetHistory()
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<GenerationFloor>>(_history.ToArray());
        }
    }

    /// <inheritdoc/>
    public Task<GenerationFloorAdvanceResult> Advance(GenerationFloorAdvance advance)
    {
        lock (_lock)
        {
            var result = GenerationFloorExtensions.Compare(_floor, advance);
            if (result is not null)
            {
                return Task.FromResult(result);
            }
            _floor = advance.Next();
            _history.Add(_floor);
            return Task.FromResult<GenerationFloorAdvanceResult>(new GenerationFloorAdvanced(_floor));
        }
    }
}
