// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Extension methods for starting the MongoDB spec containers with a clear diagnostic for
/// a known, environment-caused startup failure.
/// </summary>
public static class MongoDBContainerStartExtensions
{
    const string KernelIncompatibilityMarker = "known incompatibility with this version of MongoDB";

    /// <summary>
    /// Starts the given MongoDB container, turning MongoDB's kernel-incompatibility startup
    /// guard into a <see cref="MongoDBKernelIncompatible"/> exception instead of Testcontainers'
    /// raw container-exit trace.
    /// </summary>
    /// <param name="container">The <see cref="IContainer"/> to start.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="MongoDBKernelIncompatible">Thrown when the container failed to start because of MongoDB's Linux kernel 6.19-7.0.13 startup guard.</exception>
    public static async Task StartMongoDBWithDiagnostics(this IContainer container)
    {
        try
        {
            await container.StartAsync();
        }
        catch (Exception ex) when (ex.Message.Contains(KernelIncompatibilityMarker, StringComparison.OrdinalIgnoreCase))
        {
            throw new MongoDBKernelIncompatible(ex);
        }
    }
}
