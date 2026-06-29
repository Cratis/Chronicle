// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints;

/// <summary>
/// Represents an in-memory implementation of <see cref="IConstraintsStorage"/>.
/// </summary>
public class ConstraintsStorage : IConstraintsStorage
{
    readonly ConcurrentDictionary<ConstraintName, IConstraintDefinition> _definitions = new();

    /// <inheritdoc/>
    public Task<IEnumerable<IConstraintDefinition>> GetDefinitions() =>
        Task.FromResult<IEnumerable<IConstraintDefinition>>(_definitions.Values.ToList());

    /// <inheritdoc/>
    public Task SaveDefinition(IConstraintDefinition definition)
    {
        _definitions[definition.Name] = definition;
        return Task.CompletedTask;
    }
}
