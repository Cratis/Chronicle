// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Serverless;

internal static partial class DeploymentFingerprintCheckLogging
{
    [LoggerMessage(LogLevel.Debug, "Deployment fingerprint is current for event store {EventStore}")]
    internal static partial void DeploymentFingerprintCurrent(this ILogger logger, EventStoreName eventStore);

    [LoggerMessage(LogLevel.Warning, "Deployment definitions are {Verdict} for event store {EventStore}; expected generation {ExpectedGeneration}, stored generation {StoredGeneration}, expected fingerprint {ExpectedFingerprint}, stored fingerprint {StoredFingerprint}. Deploy the current definitions with RegisterDefinitions")]
    internal static partial void DeploymentFingerprintDiffers(this ILogger logger, EventStoreName eventStore, DeploymentFingerprintVerdict verdict, ulong expectedGeneration, ulong storedGeneration, string expectedFingerprint, string storedFingerprint);

    [LoggerMessage(LogLevel.Warning, "Could not check deployment definitions for event store {EventStore}; registration readiness is unchanged")]
    internal static partial void DeploymentFingerprintFailed(this ILogger logger, Exception exception, EventStoreName eventStore);
}
