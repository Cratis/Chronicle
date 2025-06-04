// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the events configuration.
/// </summary>
public class Events
{
    /// <summary>
    /// Number of appended event queues to use.
    /// </summary>
    public int Queues { get; init; } = 8;
}
