// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents a consumer binding declared in configuration. See <see cref="Federation.Bindings"/>.
/// </summary>
public class FederationBindingConfiguration
{
    /// <summary>
    /// Gets or inits the logical source name consumer code refers to.
    /// </summary>
    public string LogicalSource { get; init; } = string.Empty;

    /// <summary>
    /// Gets or inits the peer federation endpoint.
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// Gets or inits the pinned server certificate, <c language="text">sha256:&lt;base64&gt;</c>.
    /// </summary>
    public string ServerPin { get; init; } = string.Empty;

    /// <summary>
    /// Gets or inits the name of the entry in <see cref="Federation.Certificates"/> to present.
    /// </summary>
    public string ClientCertificate { get; init; } = string.Empty;

    /// <summary>
    /// Gets or inits the event store on the peer.
    /// </summary>
    public string RemoteEventStore { get; init; } = string.Empty;

    /// <summary>
    /// Gets or inits the namespace on the peer.
    /// </summary>
    public string RemoteNamespace { get; init; } = "Default";

    /// <summary>
    /// Gets or inits the local event store whose inbox receives the events.
    /// </summary>
    public string TargetEventStore { get; init; } = string.Empty;

    /// <summary>
    /// Gets or inits the local namespace.
    /// </summary>
    public string TargetNamespace { get; init; } = "Default";
}
