// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// The shared attribute keys, used by metrics and spans.
/// </summary>
public static class WellKnownTelemetryAttributes
{
    /// <summary>The correlation identifier of the operation.</summary>
    public const string CorrelationId = "cratis.correlation_id";

    /// <summary>The name of the event store.</summary>
    public const string EventStoreName = "cratis.event_store.name";

    /// <summary>The name of the event store namespace.</summary>
    public const string EventStoreNamespace = "cratis.event_store.namespace";

    /// <summary>The tenant identifier.</summary>
    public const string TenantId = "cratis.tenant.id";

    /// <summary>The identifier of the event sequence.</summary>
    public const string EventSequenceId = "cratis.event_sequence.id";

    /// <summary>The sequence number of an event in an event sequence.</summary>
    public const string EventSequenceNumber = "cratis.event_sequence.number";

    /// <summary>The identifier of the event type.</summary>
    public const string EventTypeId = "cratis.event_type.id";

    /// <summary>The generation of the event type.</summary>
    public const string EventTypeGeneration = "cratis.event_type.generation";

    /// <summary>The type of the event source.</summary>
    public const string EventSourceType = "cratis.event_source.type";

    /// <summary>The identifier of the event source, exposed according to the identifier exposure configuration.</summary>
    public const string EventSourceId = "cratis.event_source.id";

    /// <summary>The number of events.</summary>
    public const string EventCount = "cratis.event.count";

    /// <summary>The identifier of the observer.</summary>
    public const string ObserverId = "cratis.observer.id";

    /// <summary>The type of the observer.</summary>
    public const string ObserverType = "cratis.observer.type";

    /// <summary>Whether the operation is part of a replay.</summary>
    public const string Replay = "cratis.replay";

    /// <summary>The retry attempt number.</summary>
    public const string RetryAttempt = "cratis.retry.attempt";

    /// <summary>The identifier of the client connection.</summary>
    public const string ConnectionId = "cratis.chronicle.connection.id";

    /// <summary>The identifier of an appended events queue.</summary>
    public const string AppendedEventsQueueId = "cratis.chronicle.appended_events_queue.id";

    /// <summary>The reason an observer was spilled to catch-up.</summary>
    public const string ObserverSpillReason = "cratis.chronicle.observer.spill_reason";

    /// <summary>The kind of claim divergence.</summary>
    public const string ObserverClaimDivergence = "cratis.chronicle.observer.claim_divergence";

    /// <summary>The condition of an alert.</summary>
    public const string AlertCondition = "cratis.chronicle.alert.condition";

    /// <summary>The severity of an alert.</summary>
    public const string AlertSeverity = "cratis.chronicle.alert.severity";

    /// <summary>The result state of an abandoned delivery.</summary>
    public const string AbandonedDeliveryResultState = "cratis.chronicle.abandoned_delivery.result_state";

    /// <summary>The authority of a webhook target.</summary>
    public const string WebhookAuthority = "cratis.chronicle.webhook.authority";

    /// <summary>The SQL provider.</summary>
    public const string SqlProvider = "cratis.chronicle.sql.provider";

    /// <summary>The outcome of a schema lock wait.</summary>
    public const string SqlSchemaLockOutcome = "cratis.chronicle.sql.schema_lock.outcome";

    /// <summary>The name of a live query.</summary>
    public const string SqlLiveQueryName = "cratis.chronicle.sql.live_query.name";

    /// <summary>The kind of a live query failure.</summary>
    public const string SqlLiveQueryFailureKind = "cratis.chronicle.sql.live_query.failure_kind";

    /// <summary>The operation of an upsert.</summary>
    public const string SqlUpsertOperation = "cratis.chronicle.sql.upsert.operation";
}
