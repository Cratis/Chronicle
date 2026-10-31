// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery;

/// <summary>
/// Controls how likely artifact identity changes are handled before registration.
/// </summary>
public enum ArtifactIdentityDriftPolicy
{
    /// <summary>
    /// Log findings and continue registering.
    /// </summary>
    Warn = 0,

    /// <summary>
    /// Fail before registering when a Warning-level reactor or event type finding exists.
    /// Information-level reducer and projection findings never prevent registration.
    /// </summary>
    Fail = 1,

    /// <summary>
    /// Skip the identity check and its kernel calls.
    /// </summary>
    Ignore = 2
}
