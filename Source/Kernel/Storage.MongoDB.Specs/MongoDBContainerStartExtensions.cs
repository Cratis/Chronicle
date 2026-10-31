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

    /// <summary>
    /// The default deadline for a MongoDB spec container to become ready. It also covers starting the resource reaper and pulling the image
    /// on a cold runner, and stays far below the test session limit.
    /// </summary>
    public static readonly TimeSpan DefaultStartDeadline = TimeSpan.FromMinutes(5);

    const string KernelIncompatibilityMarker = "known incompatibility with this version of MongoDB";
    const int LogTailCharacters = 4000;

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
    /// Starts the given MongoDB container within a deadline, turning MongoDB's kernel-incompatibility startup
    /// guard into a <see cref="MongoDBKernelIncompatible"/> exception instead of Testcontainers'
    /// raw container-exit trace, and a container that never becomes ready into a
    /// <see cref="MongoDBContainerStartTimedOut"/> carrying its logs instead of a hung test run.
    /// </summary>
    /// <param name="container">The <see cref="IContainer"/> to start.</param>
    /// <param name="deadline">How long the container may take to become ready; <see cref="DefaultStartDeadline"/> when omitted.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="MongoDBKernelIncompatible">Thrown when the container failed to start because of MongoDB's Linux kernel 6.19-7.0.13 startup guard.</exception>
    /// <exception cref="MongoDBContainerStartTimedOut">Thrown when the container did not become ready within the deadline.</exception>
    public static async Task StartMongoDBWithDiagnostics(this IContainer container, TimeSpan? deadline = default)
    {
        var startDeadline = deadline ?? DefaultStartDeadline;
        using var timeout = new CancellationTokenSource(startDeadline);
        try
        {
            await container.StartAsync(timeout.Token);
        }
        catch (Exception ex) when (timeout.IsCancellationRequested && ex is OperationCanceledException or TimeoutException)
        {
            // Testcontainers reports a wait cancelled by the token as a TimeoutException, a cancelled request as an OperationCanceledException.
            throw new MongoDBContainerStartTimedOut(startDeadline, await ReadLogTail(container), ex);
        }
        catch (Exception ex) when (ex.Message.Contains(KernelIncompatibilityMarker, StringComparison.OrdinalIgnoreCase))
        {
            throw new MongoDBKernelIncompatible(ex);
        }
    }

    static async Task<string> ReadLogTail(IContainer container)
    {
        try
        {
            using var logsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var (stdout, stderr) = await container.GetLogsAsync(ct: logsTimeout.Token);
            return $"stderr:{Environment.NewLine}{Tail(stderr)}{Environment.NewLine}stdout:{Environment.NewLine}{Tail(stdout)}";
        }
        catch (Exception error)
        {
            // The timeout is the failure being reported; a log read failure only explains missing logs.
            return $"(could not read container logs: {error.Message})";
        }
    }

    static string Tail(string value) => value.Length <= LogTailCharacters ? value : value[^LogTailCharacters..];
}
