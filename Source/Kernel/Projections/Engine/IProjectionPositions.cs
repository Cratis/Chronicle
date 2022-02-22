// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Defines a system for maintaining positions for projections.
/// </summary>
public interface IProjectionPositions
{
    /// <summary>
    /// Get the sequence number last processed for a given <see cref="IProjection"/>.
    /// </summary>
    /// <param name="projection"><see cref="IProjection"/> to get for.</param>
    /// <param name="configurationId"><see cref="ProjectionSinkConfigurationId"/> to get for.</param>
    /// <returns>The <see cref="EventSequenceNumber"/>.</returns>
    Task<EventSequenceNumber> GetFor(IProjection projection, ProjectionSinkConfigurationId configurationId);

    /// <summary>
    /// Save the last sequence number processed for a given <see cref="IProjection"/>.
    /// </summary>
    /// <param name="projection"><see cref="IProjection"/> to save for.</param>
    /// <param name="configurationId"><see cref="ProjectionSinkConfigurationId"/> to get for.</param>
    /// <param name="position">The <see cref="EventSequenceNumber"/>.</param>
    /// <returns>Asynchronous task.</returns>
    Task Save(IProjection projection, ProjectionSinkConfigurationId configurationId, EventSequenceNumber position);

    /// <summary>
    /// Reset the position for a specific <see cref="IProjection"/>.
    /// </summary>
    /// <param name="projection"><see cref="IProjection"/> to reset for.</param>
    /// <param name="configurationId"><see cref="ProjectionSinkConfigurationId"/> to get for.</param>
    /// <returns>Asynchronous task.</returns>
    Task Reset(IProjection projection, ProjectionSinkConfigurationId configurationId);
}
