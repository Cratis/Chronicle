// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Setup;

internal static partial class KernelProjectionRegistrationRetriesLogging
{
    [LoggerMessage(LogLevel.Information, "Kernel projections registered for event store '{EventStore}' after a background retry.")]
    internal static partial void RegistrationSucceeded(this ILogger<KernelProjectionRegistrationRetries> logger, EventStoreName eventStore);

    [LoggerMessage(LogLevel.Warning, "Kernel projection registration for event store '{EventStore}' could not reach a sibling silo. Retrying in the background.")]
    internal static partial void TransientRegistrationFailure(this ILogger<KernelProjectionRegistrationRetries> logger, EventStoreName eventStore, Exception exception);

    [LoggerMessage(LogLevel.Error, "Kernel projection registration for event store '{EventStore}' failed with a non-transient error. The store remains unready; retrying in the background.")]
    internal static partial void RegistrationFailure(this ILogger<KernelProjectionRegistrationRetries> logger, EventStoreName eventStore, Exception exception);

    [LoggerMessage(LogLevel.Information, "Stopped background kernel projection registration for event store '{EventStore}' during silo shutdown.")]
    internal static partial void RegistrationRetryStopped(this ILogger<KernelProjectionRegistrationRetries> logger, EventStoreName eventStore);
}
