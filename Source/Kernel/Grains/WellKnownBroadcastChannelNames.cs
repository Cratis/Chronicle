// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains;

/// <summary>
/// Holds well known broadcast channel names.
/// </summary>
public static class WellKnownBroadcastChannelNames
{
    /// <summary>
    /// The name of the channel for when a projection changes definition.
    /// </summary>
    public const string ProjectionChanged = "projection-changed";
}
