// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Serverless;

/// <summary>
/// Performs the advisory deployment check. It never registers definitions or changes readiness.
/// </summary>
internal sealed class DeploymentFingerprintCheck
{
    /// <summary>
    /// Gets whether the diagnostic is enabled for this client.
    /// </summary>
    /// <param name="options">The client's options.</param>
    /// <returns>Whether to schedule the check.</returns>
    internal static bool IsEnabled(ChronicleOptions options) => ServerlessActivation.IsOn(options) &&
        options.Serverless.Registration == ServerlessRegistration.Deploy && options.Serverless.CheckArtifactFingerprint;

    /// <summary>
    /// Compares deployment metadata with the local prepared definitions.
    /// </summary>
    /// <param name="deployment">The deployment metadata.</param>
    /// <param name="generation">The configured generation, or zero when unspecified.</param>
    /// <param name="fingerprint">The local fingerprint.</param>
    /// <returns>Whether deployment is current.</returns>
    internal static DeploymentFingerprintVerdict Compare(EventStoreDeploymentResponse deployment, ulong generation, ArtifactFingerprint fingerprint)
    {
        if (string.IsNullOrEmpty(deployment.Fingerprint)) return DeploymentFingerprintVerdict.NotRegistered;
        return (generation == 0 || generation == deployment.Generation) && string.Equals(fingerprint.Value, deployment.Fingerprint, StringComparison.Ordinal)
            ? DeploymentFingerprintVerdict.UpToDate
            : DeploymentFingerprintVerdict.Stale;
    }

    /// <summary>
    /// Runs one fully observed, cancellable diagnostic.
    /// </summary>
    /// <param name="eventStore">The prepared store.</param>
    /// <param name="generation">The configured generation.</param>
    /// <param name="loggerFactory">The diagnostic logger factory.</param>
    /// <param name="cancellationToken">The client lifetime cancellation.</param>
    /// <returns>The diagnostic operation; failures are logged rather than propagated.</returns>
    internal static async Task Run(EventStore eventStore, ulong generation, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        ILogger? logger = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger = loggerFactory.CreateLogger<DeploymentFingerprintCheck>();
            var deployment = await eventStore.GetDeployment(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var computation = eventStore.ComputeDefinitionsFingerprint();

            // Cancellation can stop waiting before a user schema callback finishes. Still observe a later failure.
            _ = computation.ContinueWith(task => _ = task.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            var fingerprint = await computation.WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var verdict = Compare(deployment, generation, fingerprint);
            if (verdict == DeploymentFingerprintVerdict.UpToDate)
            {
                logger.DeploymentFingerprintCurrent(eventStore.Name);
            }
            else
            {
                logger.DeploymentFingerprintDiffers(eventStore.Name, verdict, generation, deployment.Generation, fingerprint.Value, deployment.Fingerprint);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            try
            {
                logger?.DeploymentFingerprintFailed(exception, eventStore.Name);
            }
            catch
            {
                // Logging providers must not turn a best-effort diagnostic into an unobserved task failure.
            }
        }
    }
}
