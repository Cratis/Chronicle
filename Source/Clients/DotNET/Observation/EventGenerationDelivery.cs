// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines how client reactors and reducers receive event generations.
/// </summary>
public enum EventGenerationDelivery
{
    /// <summary>
    /// Retains the existing delivery behavior. The default for this major version.
    /// </summary>
    Compatibility = 0,

    /// <summary>
    /// Requests the subscribed generation selected and released by the kernel.
    /// </summary>
    Pinned = 1
}
