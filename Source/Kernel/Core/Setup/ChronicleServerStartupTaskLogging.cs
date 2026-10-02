// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Microsoft.Extensions.Logging;

namespace Orleans.Hosting;

/// <summary>
/// Holds log messages for <see cref="ChronicleServerStartupTask"/>.
/// </summary>
internal static partial class ChronicleServerStartupTaskLogging
{
    [LoggerMessage(LogLevel.Warning, "Failed subscribing pattern capture for event store {EventStore} in namespace {Namespace}. Startup will continue; the event log will reconcile the subscription when active")]
    internal static partial void FailedSubscribingPatternCapture(this ILogger<ChronicleServerStartupTask> logger, Exception exception, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Warning, "Cannot bootstrap alerts for legacy observer '{ObserverId}' in '{EventStore}/{Namespace}': its shared definition is missing and the observed event sequence is unknown. Restore the definition or manage the observer using its original event-sequence key")]
    internal static partial void CannotBootstrapObserverAlerts(this ILogger<ChronicleServerStartupTask> logger, ObserverId observerId, EventStoreName eventStore, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Warning, "Skipping persisted projection definition '{Identifier}' during startup because the current engine rejected it. Chronicle will continue starting so a client can re-register the projection with its current definition")]
    internal static partial void FailedRegisteringPersistedProjectionDefinition(this ILogger<ChronicleServerStartupTask> logger, Exception exception, ProjectionId identifier);

    [LoggerMessage(LogLevel.Warning, "Startup step '{Step}' could not reach a sibling silo on attempt {Attempt} of {MaxAttempts} - retrying in {Delay}. A cluster that is still forming answers late, which is not a failure of the step itself")]
    internal static partial void RetryingStartupStep(this ILogger<ChronicleServerStartupTask> logger, Exception exception, string step, int attempt, int maxAttempts, TimeSpan delay);

    [LoggerMessage(LogLevel.Critical, "Startup step '{Step}' still could not reach a sibling silo after {MaxAttempts} attempts. The cluster has had longer than membership normally takes to settle, so this is a real failure rather than a forming cluster")]
    internal static partial void StartupStepExhaustedRetries(this ILogger<ChronicleServerStartupTask> logger, Exception exception, string step, int maxAttempts);
}
