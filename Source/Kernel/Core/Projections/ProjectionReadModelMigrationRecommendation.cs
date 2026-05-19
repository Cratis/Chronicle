// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents migration recommendations for projection read model changes.
/// </summary>
internal enum ProjectionReadModelMigrationRecommendation
{
    /// <summary>
    /// Full replay is required.
    /// </summary>
    ReplayRequired = 0,

    /// <summary>
    /// Existing read models can be updated with defaults.
    /// </summary>
    UpdateAvailable = 1,

    /// <summary>
    /// Only latest-version events should be replayed.
    /// </summary>
    SelectiveReplayAvailable = 2,
}
