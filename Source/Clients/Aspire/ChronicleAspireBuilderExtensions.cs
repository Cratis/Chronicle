// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Cratis.Chronicle.Aspire;

/// <summary>
/// Extension methods for configuring a Chronicle resource via <see cref="IChronicleAspireBuilder"/>.
/// </summary>
public static class ChronicleAspireBuilderExtensions
{
    /// <summary>
    /// Configures the Chronicle resource to use an external MongoDB connection string.
    /// </summary>
    /// <remarks>
    /// Sets the <c>Cratis__Chronicle__Storage__Type</c> container environment variable to <c>MongoDB</c>
    /// and <c>Cratis__Chronicle__Storage__ConnectionDetails</c> to the resolved MongoDB connection string.
    /// These map to <c>Cratis:Chronicle:Storage:Type</c> and <c>Cratis:Chronicle:Storage:ConnectionDetails</c>
    /// in the Chronicle server configuration respectively.
    /// </remarks>
    /// <param name="builder">The <see cref="IChronicleAspireBuilder"/> to configure.</param>
    /// <param name="mongoDB">The <see cref="IResourceBuilder{T}"/> providing the MongoDB connection string.</param>
    /// <returns>The same <see cref="IChronicleAspireBuilder"/> for continuation.</returns>
    public static IChronicleAspireBuilder WithMongoDB(
        this IChronicleAspireBuilder builder,
        IResourceBuilder<IResourceWithConnectionString> mongoDB)
    {
        builder.ResourceBuilder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[ChronicleContainerImageTags.StorageTypeEnvironmentVariable] = "MongoDB";
            context.EnvironmentVariables[ChronicleContainerImageTags.StorageConnectionDetailsEnvironmentVariable] = mongoDB.Resource.ConnectionStringExpression;
        });
        return builder;
    }
}
