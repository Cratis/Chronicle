// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Describes publication independently of retention and occurrence bookkeeping.
/// </summary>
public enum ReplayPublication
{
    /// <summary>
    /// Another attempt owns publication; this attempt must not change the observer.
    /// </summary>
    Superseded = 0,

    /// <summary>
    /// The rebuilt model is published.
    /// </summary>
    Published = 1,

    /// <summary>
    /// The rebuilt model is published, but recording its occurrence failed.
    /// </summary>
    PublishedWithBookkeepingFailure = 2
}
