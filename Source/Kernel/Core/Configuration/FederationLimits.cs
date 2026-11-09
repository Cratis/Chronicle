// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the limits applied to every federation link.
/// </summary>
public class FederationLimits
{
    /// <summary>
    /// Gets or inits the maximum number of events returned by one pull. Defaults to 500.
    /// </summary>
    public int MaxBatchSize { get; init; } = 500;

    /// <summary>
    /// Gets or inits the maximum number of events a link may be served per second. Defaults to 5000.
    /// </summary>
    public int MaxEventsPerSecond { get; init; } = 5000;

    /// <summary>
    /// Gets or inits the maximum number of content bytes a link may be served per second. Defaults to 16 MiB.
    /// </summary>
    public long MaxBytesPerSecond { get; init; } = 16 * 1024 * 1024;
}
