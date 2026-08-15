// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Holds log messages for <see cref="EventStoreSubscriptionObserverSubscriber"/>.
/// </summary>
internal static partial class EventStoreSubscriptionObserverSubscriberLogging
{
    [LoggerMessage(LogLevel.Warning, "Missing target event store in subscriber context for observer '{ObserverKey}'")]
    internal static partial void MissingTargetEventStore(this ILogger<EventStoreSubscriptionObserverSubscriber> logger, ObserverKey observerKey);

    [LoggerMessage(LogLevel.Debug, "Successfully forwarded events from observer '{ObserverKey}' to event store '{TargetEventStore}' inbox sequence '{InboxSequenceId}'")]
    internal static partial void SuccessfullyForwardedEvents(this ILogger<EventStoreSubscriptionObserverSubscriber> logger, ObserverKey observerKey, EventStoreName targetEventStore, EventSequenceId inboxSequenceId);

    [LoggerMessage(LogLevel.Error, "Error forwarding events from observer '{ObserverKey}' to event store '{TargetEventStore}' inbox sequence '{InboxSequenceId}'")]
    internal static partial void ErrorForwardingEvents(this ILogger<EventStoreSubscriptionObserverSubscriber> logger, Exception exception, ObserverKey observerKey, EventStoreName targetEventStore, EventSequenceId inboxSequenceId);

    [LoggerMessage(LogLevel.Debug, "Copied a subject's encryption key from event store '{SourceEventStore}' to event store '{TargetEventStore}' in namespace '{Namespace}' while forwarding events. That subject now holds a key in both event stores, and an erasure reaches every event store in the namespace")]
    internal static partial void CopiedEncryptionKeyToTargetEventStore(this ILogger<EventStoreSubscriptionObserverSubscriber> logger, EventStoreName sourceEventStore, EventStoreName targetEventStore, EventStoreNamespaceName @namespace);

    [LoggerMessage(LogLevel.Information, "Did not copy a subject's encryption key from event store '{SourceEventStore}' to event store '{TargetEventStore}' in namespace '{Namespace}' while forwarding events, because that subject was erased in the target. Forwarding continues; an event carrying PII for the subject cannot be appended there until a new encryption key is authorized")]
    internal static partial void SkippedCopyingEncryptionKeyToErasedEventStore(this ILogger<EventStoreSubscriptionObserverSubscriber> logger, EventStoreName sourceEventStore, EventStoreName targetEventStore, EventStoreNamespaceName @namespace);
}
