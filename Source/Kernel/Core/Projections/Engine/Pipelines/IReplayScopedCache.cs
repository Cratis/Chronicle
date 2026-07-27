// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Defines a cache whose lifetime is bound to a projection replay session.
/// </summary>
/// <remarks>
/// The cache is populated only between <see cref="BeginReplaySession"/> and <see cref="EndReplaySession"/>
/// and both boundaries clear it, so nothing cached during a replay is ever served to live event handling.
/// </remarks>
public interface IReplayScopedCache
{
    /// <summary>
    /// Starts a replay session, clearing any previously cached entries and enabling caching.
    /// </summary>
    void BeginReplaySession();

    /// <summary>
    /// Ends the replay session, disabling caching and clearing any cached entries.
    /// </summary>
    void EndReplaySession();
}
