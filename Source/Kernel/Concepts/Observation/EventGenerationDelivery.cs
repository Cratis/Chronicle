// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Defines how an observer receives event generations.
/// </summary>
public enum EventGenerationDelivery
{
    /// <summary>
    /// Retains the existing delivery behavior.
    /// </summary>
    Compatibility = 0,

    /// <summary>
    /// Selects and releases the subscribed generation in the kernel.
    /// </summary>
    Pinned = 1
}
