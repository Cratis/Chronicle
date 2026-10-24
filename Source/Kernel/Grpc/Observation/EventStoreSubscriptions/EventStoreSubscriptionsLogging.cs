// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Observation.EventStoreSubscriptions;

/// <summary>
/// Holds log messages for <see cref="EventStoreSubscriptions"/>.
/// </summary>
internal static partial class EventStoreSubscriptionsLogging
{
    [LoggerMessage(LogLevel.Information, "Subscription registration {RegistrationDiagnosticId} for target '{TargetEventStore}', source '{SourceEventStore}', subscription '{SubscriptionId}': memory count {DefinitionCount} (-1 means unavailable), matching definition {HasMatchingDefinition}")]
    internal static partial void RegistrationDecision(this ILogger<EventStoreSubscriptions> logger, string targetEventStore, EventStoreName sourceEventStore, EventStoreSubscriptionId subscriptionId, Guid registrationDiagnosticId, int definitionCount, bool hasMatchingDefinition);

    [LoggerMessage(LogLevel.Information, "Subscription registration {RegistrationDiagnosticId} for target '{TargetEventStore}', subscription '{SubscriptionId}': manager Add starting")]
    internal static partial void ManagerAddStarting(this ILogger<EventStoreSubscriptions> logger, string targetEventStore, EventStoreSubscriptionId subscriptionId, Guid registrationDiagnosticId);

    [LoggerMessage(LogLevel.Information, "Subscription registration {RegistrationDiagnosticId} for target '{TargetEventStore}', subscription '{SubscriptionId}': awaited manager Add completed; no storage readback performed")]
    internal static partial void ManagerAddCompleted(this ILogger<EventStoreSubscriptions> logger, string targetEventStore, EventStoreSubscriptionId subscriptionId, Guid registrationDiagnosticId);

    [LoggerMessage(LogLevel.Information, "Subscription registration {RegistrationDiagnosticId} for target '{TargetEventStore}', subscription '{SubscriptionId}': append starting")]
    internal static partial void SubscriptionAppendStarting(this ILogger<EventStoreSubscriptions> logger, string targetEventStore, EventStoreSubscriptionId subscriptionId, Guid registrationDiagnosticId);

    [LoggerMessage(LogLevel.Information, "Subscription registration {RegistrationDiagnosticId} for target '{TargetEventStore}', subscription '{SubscriptionId}': awaited append completed")]
    internal static partial void SubscriptionAppendCompleted(this ILogger<EventStoreSubscriptions> logger, string targetEventStore, EventStoreSubscriptionId subscriptionId, Guid registrationDiagnosticId);

    [LoggerMessage(LogLevel.Information, "Existing persisted subscription query completed for target '{TargetEventStore}', definition count {DefinitionCount} (-1 means unavailable without enumeration)")]
    internal static partial void PersistedDefinitionsRead(this ILogger<EventStoreSubscriptions> logger, string targetEventStore, int definitionCount);
}
