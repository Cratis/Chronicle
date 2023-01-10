// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the configuration for a single kernel configuration.
/// </summary>
public class SingleKernelOptions
{
    /// <summary>
    /// Gets the endpoint
    /// </summary>
    public Uri Endpoint { get; init; } = new Uri("http://localhost:8080");

    /// <summary>
    /// Gets the advertised client endpoint.
    /// </summary>
    /// <remarks>
    /// If this endpoint is not explicitly configured, it will attempt to resolve it based on the ASP.NET Core configuration and
    /// current running solution.
    /// </remarks>
    public Uri? AdvertisedClientEndpoint { get; init; }
}
