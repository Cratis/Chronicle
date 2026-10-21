// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections;

/// <summary>
/// Delegates projection definition reads and writes to shared storage without retaining silo-local state.
/// </summary>
/// <remarks>
/// Kept for callers of the former cache. A cached hit cannot establish that another silo has not
/// changed or deleted the definition, so every read must reach the authoritative storage.
/// </remarks>
/// <param name="inner">The shared projection definition storage.</param>
public sealed class CachingProjectionDefinitionsStorage(IProjectionDefinitionsStorage inner) : IProjectionDefinitionsStorage
{
    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionDefinition>> GetAll() => inner.GetAll();

    /// <inheritdoc/>
    public Task<bool> Has(ProjectionId id) => inner.Has(id);

    /// <inheritdoc/>
    public Task<ProjectionDefinition> Get(ProjectionId id) => inner.Get(id);

    /// <inheritdoc/>
    public Task Delete(ProjectionId id) => inner.Delete(id);

    /// <inheritdoc/>
    public Task Save(ProjectionDefinition definition) => inner.Save(definition);
}
