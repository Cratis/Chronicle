// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Registrations;

/// <summary>
/// Defines how a registered observer receives deliveries.
/// </summary>
public enum ObserverTargetKind
{
    /// <summary>
    /// Uses the existing client stream.
    /// </summary>
    Stream = 0,

    /// <summary>
    /// Uses an HTTP endpoint.
    /// </summary>
    Http = 1
}
