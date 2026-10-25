// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

internal static partial class EventStoreDefinitionsLogMessages
{
    [LoggerMessage(LogLevel.Trace, "Registering definitions for event store {EventStore} with deployment generation {Generation}")]
    internal static partial void RegisteringDefinitions(this ILogger<EventStore> logger, EventStoreName eventStore, DeploymentGeneration generation);

    [LoggerMessage(LogLevel.Information, "Registered definitions for event store {EventStore} with deployment generation {Generation} and fingerprint {Fingerprint}")]
    internal static partial void DefinitionsRegistered(this ILogger<EventStore> logger, EventStoreName eventStore, DeploymentGeneration generation, ArtifactFingerprint fingerprint);
}
