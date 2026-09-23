// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Extension methods for building and starting the MongoDB spec containers safely on a Linux kernel 6.19
/// through 7.0.13 host, with a clear diagnostic for the failure this does not cover.
/// </summary>
public static class MongoDBContainerStartExtensions
{
    /// <summary>
    /// The environment variable name that controls whether glibc or TCMalloc owns Restartable Sequences (rseq)
    /// registration - see <see cref="WithMongoDBKernelCompatibility"/>.
    /// </summary>
    public const string GlibcRseqCompatibilityVariable = "GLIBC_TUNABLES";

    /// <summary>
    /// The value that reverts to the pre-8.0.5 behavior where glibc, not TCMalloc, owns rseq registration - the
    /// documented fix for MongoDB's Linux kernel 6.19 through 7.0.13 incompatibility that does not require a
    /// different MongoDB version or a different kernel. See
    /// https://jira.mongodb.org/browse/SERVER-121912 and
    /// https://github.com/docker-library/mongo/discussions/748.
    /// </summary>
    public const string GlibcRseqCompatibilityValue = "glibc.pthread.rseq=1";

    const string KernelIncompatibilityMarker = "known incompatibility with this version of MongoDB";

    /// <summary>
    /// Configures the container to start reliably on a host whose Linux kernel falls in MongoDB's documented
    /// incompatible range (6.19 through 7.0.13), without changing the MongoDB image or version.
    /// </summary>
    /// <param name="builder">The <see cref="ContainerBuilder"/> to configure.</param>
    /// <returns>The configured <see cref="ContainerBuilder"/>.</returns>
    /// <remarks>
    /// The official MongoDB 8.0.5+ image bakes in <c language="csharp">GLIBC_TUNABLES=glibc.pthread.rseq=0</c>, handing rseq
    /// registration to the TCMalloc allocator it vendors - a performance optimization that a newer kernel's rseq
    /// ABI change makes unsafe, which is exactly what MongoDB's own startup guard exists to catch. Overriding the
    /// variable to <see cref="GlibcRseqCompatibilityValue"/> hands rseq registration back to <c language="csharp">glibc</c> instead, the
    /// behavior every MongoDB version used before 8.0.5 and that the guard itself never objects to. This is
    /// documented by MongoDB as the fix for kernels in the affected range - not a workaround this project invented
    /// - and applying it unconditionally is safe on every kernel: it is a value MongoDB shipped as the only
    /// behavior for years before the newer default existed.
    /// </remarks>
    public static ContainerBuilder WithMongoDBKernelCompatibility(this ContainerBuilder builder) =>
        builder.WithEnvironment(GlibcRseqCompatibilityVariable, GlibcRseqCompatibilityValue);

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
