// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.Projections;

/// <summary>
/// Defines append-only history of distinct accepted projection definition content.
/// This history does not select the active definition or a projection generation.
/// </summary>
public interface IProjectionDefinitionHistoryStorage
{
    /// <summary>
    /// Records content if its projection and digest have not already been recorded.
    /// Existing content and first-registration metadata are never overwritten.
    /// </summary>
    /// <param name="entry">The original definition and its registration metadata.</param>
    /// <returns>The asynchronous operation.</returns>
    Task Record(ProjectionDefinitionHistoryEntry entry);

    /// <summary>
    /// Gets the distinct content for a projection, ordered by recording time then digest.
    /// </summary>
    /// <param name="projectionId">The projection identifier.</param>
    /// <returns>The recorded content, or an empty collection when none exists.</returns>
    Task<IEnumerable<ProjectionDefinitionHistoryEntry>> GetFor(ProjectionId projectionId);
}
