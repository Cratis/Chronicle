// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Defines which stored read model roots a read returns, based on their initialization marker.
/// </summary>
public enum ReadModelInstanceVisibility
{
    /// <summary>
    /// Roots stored with the initialization marker set to false are hidden. This is the default for explicit-root projections.
    /// </summary>
    InitializedOnly = 0,

    /// <summary>
    /// No initialization marker filter is applied. Used for child-only projections, whose roots have no creation event of their own.
    /// </summary>
    IncludeUninitialized = 1
}
