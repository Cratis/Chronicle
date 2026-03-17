// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Chronicle resources to an Aspire distributed application.
/// </summary>
public static class ChronicleDistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Adds a Chronicle server resource to the distributed application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When called without a <paramref name="configure"/> callback (development mode), the Chronicle
    /// development container image is used, which includes an embedded MongoDB instance. This is
    /// ideal for local development and testing without needing an external database.
    /// </para>
    /// <para>
    /// When a <paramref name="configure"/> callback is provided (production mode), the standard
    /// Chronicle production image is used. Use <see cref="ChronicleAspireBuilderExtensions.WithMongoDB"/>
    /// inside the callback to wire up an external MongoDB connection string.
    /// </para>
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resource to.</param>
    /// <param name="name">The name for the Chronicle resource. Defaults to <c>"chronicle"</c>.</param>
    /// <param name="configure">
    /// Optional callback for configuring Chronicle for production. When provided, the production
    /// image is used and the callback receives an <see cref="IChronicleAspireBuilder"/> for further
    /// configuration (e.g. wiring up an external MongoDB connection string).
    /// When omitted, the development image with embedded MongoDB is used.
    /// </param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> for the <see cref="ChronicleResource"/>.</returns>
    public static IResourceBuilder<ChronicleResource> AddCratisChronicle(
        this IDistributedApplicationBuilder builder,
        string name = "chronicle",
        Action<IChronicleAspireBuilder>? configure = default)
    {
        var resource = new ChronicleResource(name);
        var imageTag = configure is null
            ? ChronicleContainerImageTags.DevelopmentTag
            : ChronicleContainerImageTags.Tag;

        var resourceBuilder = builder
            .AddResource(resource)
            .WithImage(ChronicleContainerImageTags.Image, imageTag)
            .WithImageRegistry(ChronicleContainerImageTags.Registry)
            .WithHttpEndpoint(targetPort: ChronicleResource.DefaultGrpcPort, name: ChronicleContainerImageTags.GrpcEndpointName)
            .WithHttpEndpoint(targetPort: ChronicleResource.DefaultManagementPort, name: ChronicleContainerImageTags.ManagementEndpointName);

        if (configure is not null)
        {
            var aspireBuilder = new ChronicleAspireBuilder(resourceBuilder);
            configure(aspireBuilder);
        }

        return resourceBuilder;
    }
}
