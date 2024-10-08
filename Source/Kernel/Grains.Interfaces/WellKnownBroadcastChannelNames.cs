// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains;

/// <summary>
/// Holds well known broadcast channel names.
/// </summary>
public static class WellKnownBroadcastChannelNames
{
    /// <summary>
    /// The name of the channel for when a namespace is added.
    /// </summary>
    public const string NamespaceAdded = "namespace-added";

    /// <summary>
    /// The name of the channel for when constraints in an event store are changed.
    /// </summary>
    public const string ConstraintsChanged = "constraints-changed";

    /// <summary>
    /// The name of the channel for notifications telling to reload state.
    /// </summary>
    public const string ReloadState = "reload-state";
}
