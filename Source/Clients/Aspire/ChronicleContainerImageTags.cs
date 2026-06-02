// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aspire;

/// <summary>
/// Constants for the Chronicle container image tags and endpoint names.
/// </summary>
public static class ChronicleContainerImageTags
{
    /// <summary>
    /// Docker Hub registry.
    /// </summary>
    public const string Registry = "docker.io";

    /// <summary>
    /// Chronicle container image name.
    /// </summary>
    public const string Image = "cratis/chronicle";

    /// <summary>
    /// Tag for the Chronicle production image (no embedded MongoDB).
    /// </summary>
    public const string Tag = "latest";

    /// <summary>
    /// Tag for the Chronicle development image (with embedded MongoDB).
    /// </summary>
    public const string DevelopmentTag = "latest-development";

    /// <summary>
    /// Tag for the Chronicle slim development image (no embedded MongoDB).
    /// </summary>
    public const string DevelopmentSlimTag = "latest-development-slim";

    /// <summary>
    /// Name of the gRPC endpoint.
    /// </summary>
    public const string GrpcEndpointName = "grpc";

    /// <summary>
    /// Name of the management HTTP endpoint.
    /// </summary>
    public const string ManagementEndpointName = "management";

    /// <summary>
    /// Environment variable key for the Chronicle storage type (e.g. <c>MongoDB</c>).
    /// Maps to <c>Cratis:Chronicle:Storage:Type</c> in the Chronicle server configuration.
    /// </summary>
    public const string StorageTypeEnvironmentVariable = "Cratis__Chronicle__Storage__Type";

    /// <summary>
    /// Environment variable key for the Chronicle storage connection details (e.g. MongoDB connection string).
    /// Maps to <c>Cratis:Chronicle:Storage:ConnectionDetails</c> in the Chronicle server configuration.
    /// </summary>
    public const string StorageConnectionDetailsEnvironmentVariable = "Cratis__Chronicle__Storage__ConnectionDetails";
}
