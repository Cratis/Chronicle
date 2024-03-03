// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Configuration;

/// <summary>
/// Represents the configuration for a specific microservice.
/// </summary>
public class Microservice
{
    /// <summary>
    /// Gets the name of the microservice.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the inbox configuration for the microservice.
    /// </summary>
    public Inbox Inbox { get; init; } = new Inbox();
}
