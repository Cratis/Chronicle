// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage.given;

/// <summary>
/// Represents shared storage whose listing can be held after taking its snapshot, to overlap it with other operations.
/// </summary>
/// <param name="shared">The <see cref="SharedProjectionDefinitionsStorage"/> holding the definitions.</param>
public class HeldListingStorage(SharedProjectionDefinitionsStorage shared) : IProjectionDefinitionsStorage
{
    TaskCompletionSource? _hold;
    TaskCompletionSource? _release;

    /// <summary>
    /// Gets a task that completes when a held listing has taken its snapshot.
    /// </summary>
    public TaskCompletionSource SnapshotTaken { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Holds the next listing after it has taken its snapshot, until <see cref="Release"/> is called.
    /// </summary>
    public void HoldNextListing()
    {
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _hold = _release;
    }

    /// <summary>
    /// Releases a held listing.
    /// </summary>
    public void Release() => _release?.TrySetResult();

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionDefinition>> GetAll()
    {
        var snapshot = (await shared.GetAll()).ToArray();
        var release = _hold;
        _hold = null;
        if (release is not null)
        {
            SnapshotTaken.TrySetResult();
            await release.Task;
        }

        return snapshot;
    }

    /// <inheritdoc/>
    public Task<bool> Has(ProjectionId id) => shared.Has(id);

    /// <inheritdoc/>
    public Task<ProjectionDefinition> Get(ProjectionId id) => shared.Get(id);

    /// <inheritdoc/>
    public Task Delete(ProjectionId id) => shared.Delete(id);

    /// <inheritdoc/>
    public Task Save(ProjectionDefinition definition) => shared.Save(definition);
}
