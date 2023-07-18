// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;

namespace Aksio.Configuration;

/// <summary>
/// Represents the client configuration.
/// </summary>
public class ClientOptions
{
    /// <summary>
    /// Gets or sets the <see cref="MicroserviceId"/> for the client.
    /// </summary>
    public MicroserviceId MicroserviceId { get; set; } = MicroserviceId.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="MicroserviceName"/> for the client.
    /// </summary>
    public MicroserviceName MicroserviceName { get; set; } = MicroserviceName.Unspecified;

    /// <summary>
    /// Gets the Kernel connectivity configuration.
    /// </summary>
    public KernelConnectivity Kernel { get; set; } = new();
}
