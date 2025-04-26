// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Configuration;

/// <summary>
/// Represents the Chronicle options.
/// </summary>
public class ChronicleOptions
{
    /// <summary>
    /// Port to listen on for gRPC.
    /// </summary>
    public int Port { get; init; } = 35000;

    /// <summary>
    /// Gets the port for the REST API.
    /// </summary>
    public int ApiPort { get; init; } = 8080;

    /// <summary>
    /// Gets the port for the Orleans Silo-to-Silo communication.
    /// </summary>
    public int SiloPort { get; init; } = 11111;

    /// <summary>
    /// Gets the port for the Orleans Client-to-Silo communication.
    /// </summary>
    public int GatewayPort { get; init; } = 30000;

    /// <summary>
    /// Gets or sets the IP address to be advertised in membership tables.
    /// </summary>
    /// <remarks>
    /// If this is not set, the <see cref="AdvertisedHostname"/> will be used as the advertised IP address.
    /// </remarks>
    public string AdvertisedIPAddress { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the host name.
    /// </summary>
    /// <remarks>
    /// If <see cref="AdvertisedIPAddress"/> is not set, this will be used as the advertised IP address.
    /// </remarks>
    public string AdvertisedHostname { get; init; } = string.Empty;

    /// <summary>
    /// Gets the <see cref="Events"/> configuration.
    /// </summary>
    public Events Events { get; init; } = new Events();

    /// <summary>
    /// Feature toggles for Chronicle.
    /// </summary>
    public Features Features { get; init; } = new Features();

    /// <summary>
    /// Gets or inits the storage configuration.
    /// </summary>
    public Storage Storage { get; init; } = new Storage();

    /// <summary>
    /// Gets the observers configuration.
    /// </summary>
    public Observers Observers { get; init; } = new Observers();
}
