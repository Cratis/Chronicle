// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Represents how a client registers its artifacts with the kernel when it connects.
/// </summary>
public enum RegistrationMode
{
    /// <summary>
    /// Register everything that is discovered when connecting. This is the default.
    /// </summary>
    Inline = 0,

    /// <summary>
    /// Ensure the event store exists and register the event types only.
    /// </summary>
    EventTypesOnly = 1,

    /// <summary>
    /// Register nothing when connecting. Another process of the application registered the artifacts at deployment time.
    /// </summary>
    AtDeployment = 2
}
