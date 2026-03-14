// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire;

/// <summary>
/// Represents a Chronicle server resource for use in Aspire distributed applications.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class ChronicleResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    /// <summary>
    /// Gets the default gRPC port for the Chronicle server.
    /// </summary>
    public const int DefaultGrpcPort = 35000;

    /// <summary>
    /// Gets the default management API port for the Chronicle server.
    /// </summary>
    public const int DefaultManagementPort = 8080;

    /// <summary>
    /// Gets the gRPC endpoint reference.
    /// </summary>
    public EndpointReference GrpcEndpoint => new(this, ChronicleContainerImageTags.GrpcEndpointName);

    /// <summary>
    /// Gets the management endpoint reference.
    /// </summary>
    public EndpointReference ManagementEndpoint => new(this, ChronicleContainerImageTags.ManagementEndpointName);

    /// <inheritdoc/>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"chronicle://{GrpcEndpoint.Property(EndpointProperty.Host)}:{GrpcEndpoint.Property(EndpointProperty.Port)}");
}
