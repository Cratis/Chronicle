// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationPullService"/>.
/// </summary>
/// <remarks>
/// The caller is identified solely by the client certificate of the connection: its pin must belong to an active grant
/// that covers the requested event store and namespace. Everything else, including a disabled federation, an unknown or
/// revoked pin, and a grant for another store or namespace, gives the same empty <see cref="FederatedEventBatchStatus.Denied"/>
/// answer so nothing about what exists can be learned.
/// <para>
/// An event is delivered only when its event type is registered as public in the producer event store, is permitted by the
/// grant, and is wanted by the consumer. Event types whose schema carries protected personal data (PII, encrypted
/// properties) are never forwarded, whatever the grant says; they are counted, never their content.
/// </para>
/// </remarks>
/// <param name="caller">The <see cref="IFederationCaller"/> giving the presented client certificate.</param>
/// <param name="registry">The <see cref="IFederationRegistry"/> holding the grants.</param>
/// <param name="status">The <see cref="IFederationStatus"/> to record on.</param>
/// <param name="storage">The <see cref="IStorage"/> to read the outbox and event types from.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for turning event content into JSON.</param>
/// <param name="rateLimiter">The <see cref="FederationRateLimiter"/>.</param>
/// <param name="metrics">The <see cref="FederationMetrics"/>.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/> of <see cref="ChronicleOptions"/>.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
[Singleton]
public class FederationPullService(
    IFederationCaller caller,
    IFederationRegistry registry,
    IFederationStatus status,
    IStorage storage,
    IExpandoObjectConverter expandoObjectConverter,
    FederationRateLimiter rateLimiter,
    FederationMetrics metrics,
    IOptions<ChronicleOptions> options,
    ILogger<FederationPullService> logger) : IFederationPullService
{
    /// <summary>
    /// The most events inspected for one pull, as a multiple of the page size, so a long run of events the peer may not
    /// receive costs a bounded amount of work per pull while <see cref="FederatedEventBatch.NextSequenceNumber"/> still advances.
    /// </summary>
    internal const int ScanFactor = 10;

    /// <summary>
    /// The upper bound on events inspected for one pull.
    /// </summary>
    internal const int MaxScan = 10_000;

    /// <summary>
    /// The content byte budget of one batch, to stay within message size limits. At least one event is always returned.
    /// </summary>
    internal const long MaxBatchBytes = 4 * 1024 * 1024;

    /// <inheritdoc/>
    public async Task<FederatedEventBatch> Pull(string eventStore, string @namespace, ulong fromSequenceNumber, int limit, IEnumerable<string> eventTypes)
    {
        var federation = options.Value.Federation;
        if (!federation.Enabled)
        {
            return Denied(FederationMetrics.UnknownLink, "federation is disabled", fromSequenceNumber);
        }

        var pin = PinOf(caller.ClientCertificate);
        if (pin is null)
        {
            return Denied(FederationMetrics.UnknownLink, "no usable client certificate", fromSequenceNumber);
        }

        var grants = (await registry.FindActiveGrants(pin))
            .Where(grant => !grant.Revoked &&
                PinMatches(grant, pin) &&
                string.Equals(grant.EventStore, eventStore, StringComparison.Ordinal) &&
                grant.Namespaces.Contains(@namespace, StringComparer.Ordinal))
            .OrderBy(grant => grant.Id, StringComparer.Ordinal)
            .ToArray();
        if (grants.Length == 0)
        {
            return Denied(FederationMetrics.UnknownLink, "no active grant covers the request", fromSequenceNumber);
        }

        var linkId = grants[0].Id;
        var limits = federation.Limits;
        if (!rateLimiter.TryBegin(linkId, limits.MaxEventsPerSecond, limits.MaxBytesPerSecond))
        {
            status.RecordThrottled(linkId);
            metrics.Throttled(linkId);
            logger.PullThrottled(linkId);
            return Empty(FederatedEventBatchStatus.Throttled, string.Empty, fromSequenceNumber, 0);
        }

        var permitted = grants.Select(ToGrant).ToArray();
        var wanted = eventTypes?.Where(_ => !string.IsNullOrEmpty(_)).ToHashSet(StringComparer.Ordinal) ?? [];
        var pageSize = Math.Max(1, limit > 0 ? Math.Min(limit, limits.MaxBatchSize) : limits.MaxBatchSize);

        var eventStoreStorage = storage.GetEventStore(eventStore);
        var sequence = eventStoreStorage.GetNamespace(@namespace).GetEventSequence(EventSequenceId.Outbox);
        var tail = await sequence.GetTailSequenceNumber();
        if (!tail.IsActualValue || fromSequenceNumber > tail.Value)
        {
            var tailValue = tail.IsActualValue ? tail.Value : 0UL;
            return Empty(FederatedEventBatchStatus.Ok, eventStore, fromSequenceNumber, tailValue);
        }

        var window = (ulong)Math.Min(MaxScan, Math.Max(pageSize, pageSize * ScanFactor));
        var end = tail.Value - fromSequenceNumber >= window ? fromSequenceNumber + window - 1 : tail.Value;

        var decisions = new Dictionary<EventType, EventTypeDecision>();
        var skippedProtected = new Dictionary<string, int>(StringComparer.Ordinal);
        var delivered = new List<FederatedEvent>();
        var bytes = 0L;
        var next = end + 1;

        using var cursor = await sequence.GetRange(fromSequenceNumber, end);
        var full = false;
        while (!full && await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                var sequenceNumber = @event.Context.SequenceNumber.Value;
                if (wanted.Count > 0 && !wanted.Contains(@event.Context.EventType.Id.Value))
                {
                    continue;
                }

                if (!decisions.TryGetValue(@event.Context.EventType, out var decision))
                {
                    decision = await Decide(eventStoreStorage, @event.Context.EventType, permitted);
                    decisions[@event.Context.EventType] = decision;
                }

                if (decision.Schema is null)
                {
                    if (decision.IsProtected)
                    {
                        var id = @event.Context.EventType.Id.Value;
                        skippedProtected[id] = skippedProtected.GetValueOrDefault(id) + 1;
                    }

                    continue;
                }

                var content = expandoObjectConverter.ToJsonObject(@event.Content, decision.Schema).ToJsonString();
                var size = Encoding.UTF8.GetByteCount(content);
                if (delivered.Count > 0 && bytes + size > MaxBatchBytes)
                {
                    next = sequenceNumber;
                    full = true;
                    break;
                }

                delivered.Add(ToFederatedEvent(@event, content));
                bytes += size;

                if (delivered.Count >= pageSize)
                {
                    next = sequenceNumber + 1;
                    full = true;
                    break;
                }
            }
        }

        var skipped = skippedProtected.Values.Sum();
        if (skipped > 0)
        {
            metrics.SkippedProtected(linkId, skipped);
            foreach (var (id, count) in skippedProtected)
            {
                logger.SkippedProtected(count, id, linkId);
            }
        }

        rateLimiter.Consume(linkId, delivered.Count, bytes);
        status.RecordPull(linkId, delivered.Count, bytes);
        metrics.Delivered(linkId, delivered.Count, bytes);

        return new FederatedEventBatch(FederatedEventBatchStatus.Ok, eventStore, delivered, next, tail.Value);
    }

    static CertificatePin? PinOf(System.Security.Cryptography.X509Certificates.X509Certificate2? certificate)
    {
        if (certificate is null)
        {
            return null;
        }

        try
        {
            return CertificatePin.From(certificate);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    static bool PinMatches(FederationGrantState grant, CertificatePin pin)
    {
        try
        {
            return CertificatePin.Parse(grant.ClientPin).Equals(pin);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    static FederationGrant ToGrant(FederationGrantState state) =>
        new(state.AllPublic, state.EventTypes.Select(_ => new EventTypeId(_)).ToHashSet());

    static FederatedEvent ToFederatedEvent(Concepts.Events.AppendedEvent @event, string content)
    {
        var context = @event.Context;
        return new FederatedEvent(
            context.SequenceNumber.Value,
            context.EventType.Id.Value,
            context.EventType.Generation.Value,
            context.EventSourceType.Value,
            context.EventSourceId.Value,
            context.EventStreamType.Value,
            context.EventStreamId.Value,
            context.Occurred,
            context.CorrelationId.Value.ToString(),
            context.Subject is { IsSet: true } ? context.Subject.Value : string.Empty,
            content,
            context.Tags.Select(_ => _.Value).ToArray());
    }

    static FederatedEventBatch Empty(string status, string originEventStore, ulong next, ulong tail) =>
        new(status, originEventStore, [], next, tail);

    async Task<EventTypeDecision> Decide(Storage.IEventStoreStorage eventStoreStorage, EventType eventType, FederationGrant[] grants)
    {
        var eventTypes = eventStoreStorage.EventTypes;
        if (!await eventTypes.HasFor(eventType.Id, eventType.Generation))
        {
            return EventTypeDecision.NotPermitted;
        }

        var schema = await eventTypes.GetFor(eventType.Id, eventType.Generation);
        if (!grants.Any(_ => _.Permits(eventType.Id, schema.Visibility)))
        {
            return EventTypeDecision.NotPermitted;
        }

        // Protected personal data (PII, encrypted properties) is never federated, whatever the grant says. The
        // consumer is another event store with its own compliance regime, and the keys that decrypt stay here.
        return schema.Schema.HasComplianceMetadata() ? EventTypeDecision.Withheld : new EventTypeDecision(schema.Schema, false);
    }

    FederatedEventBatch Denied(string linkId, string reason, ulong fromSequenceNumber)
    {
        metrics.Denied(linkId);
        logger.PullDenied(reason);
        return Empty(FederatedEventBatchStatus.Denied, string.Empty, fromSequenceNumber, 0);
    }

    sealed record EventTypeDecision(JsonSchema? Schema, bool IsProtected)
    {
        public static EventTypeDecision NotPermitted { get; } = new(null, false);

        public static EventTypeDecision Withheld { get; } = new(null, true);
    }
}
