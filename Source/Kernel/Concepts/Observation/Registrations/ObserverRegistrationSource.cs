// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Registrations;

/// <summary>
/// Defines the origin of an observer registration.
/// </summary>
public enum ObserverRegistrationSource
{
    /// <summary>
    /// Registered through the existing client registration path.
    /// </summary>
    Stream = 0,

    /// <summary>
    /// Registered as part of an artifact set.
    /// </summary>
    ArtifactSet = 1
}
