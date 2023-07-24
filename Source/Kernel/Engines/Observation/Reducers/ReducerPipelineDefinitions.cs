// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Observation.Reducers;

namespace Aksio.Cratis.Kernel.Engines.Observation.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerPipelineDefinitions"/>.
/// </summary>
[SingletonPerMicroservice]
public class ReducerPipelineDefinitions : IReducerPipelineDefinitions
{
    readonly ConcurrentDictionary<ReducerId, ReducerDefinition> _definitions = new();

    /// <inheritdoc/>
    public Task<ReducerDefinition> GetFor(ReducerId reducerId)
    {
        ThrowIfMissingReducerDefinition(reducerId);
        return Task.FromResult(_definitions[reducerId]);
    }

    /// <inheritdoc/>
    public Task<bool> HasFor(ReducerId reducerId) => _definitions.ContainsKey(reducerId) ? Task.FromResult(true) : Task.FromResult(false);

    /// <inheritdoc/>
    public Task Register(ReducerDefinition definition)
    {
        _definitions[definition.ReducerId] = definition;
        return Task.CompletedTask;
    }

    void ThrowIfMissingReducerDefinition(ReducerId reducerId)
    {
        if (!_definitions.ContainsKey(reducerId)) throw new MissingReducerPipelineDefinition(reducerId);
    }
}
