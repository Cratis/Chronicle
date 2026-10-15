// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Applies kernel-time delivery policy over the providers' fenced writes.
/// </summary>
/// <param name="storage">The scope's occurrence store.</param>
/// <param name="definitions">The event-store trigger contracts.</param>
/// <param name="trigger">The queue's trigger.</param>
/// <param name="options">The retry and concurrency policy.</param>
/// <param name="clock">The kernel clock.</param>
public class OccurrenceDelivery(IOccurrencesStorage storage, ITriggerDefinitionsStorage definitions, TriggerId trigger, Configuration.Occurrences options, TimeProvider clock)
{
    /// <summary>
    /// Validates ingress before its insert-if-absent durable write.
    /// </summary>
    /// <param name="idempotencyId">The retry identity.</param>
    /// <param name="payload">The plaintext JSON payload.</param>
    /// <param name="kind">The application or lifecycle kind.</param>
    /// <returns>The stable identity and typed outcome.</returns>
    public async Task<OccurrenceIngressResult> Record(string idempotencyId, string payload, OccurrenceKind kind)
    {
        var id = OccurrenceIdentities.ForIngress(trigger, idempotencyId);
        var definition = await definitions.Get(trigger);
        if (definition is null || kind is not (OccurrenceKind.Application or OccurrenceKind.Startup or OccurrenceKind.Shutdown))
        {
            return new(id.Value, OccurrenceIngressStatus.OccurrencePayloadInvalid);
        }

        string fingerprint;
        try
        {
            using var document = JsonDocument.Parse(payload);
            var schema = await JsonSchema.FromJsonAsync(definition.Schema);
            if (schema.Validate(payload).Count != 0)
            {
                return new(id.Value, OccurrenceIngressStatus.OccurrencePayloadInvalid);
            }

            fingerprint = OccurrencePayloadFingerprint.Calculate(trigger.Value, document.RootElement);
        }
        catch (JsonException)
        {
            return new(id.Value, OccurrenceIngressStatus.OccurrencePayloadInvalid);
        }

        var occurrence = new Occurrence(id, trigger, kind, fingerprint, payload, definition.Revision, clock.GetUtcNow(), null, null, OccurrenceState.Pending, 0, 0, null, null, null, null);
        var result = await storage.Record(occurrence);
        var status = result switch
        {
            OccurrenceRecordResult.Recorded => OccurrenceIngressStatus.Recorded,
            OccurrenceRecordResult.AlreadyRecorded => OccurrenceIngressStatus.AlreadyRecorded,
            _ => OccurrenceIngressStatus.OccurrenceIdConflict
        };
        return new(id.Value, status);
    }

    /// <summary>
    /// Claims due work after quarantining exhausted, expired attempts.
    /// </summary>
    /// <param name="worker">The owning connection identity.</param>
    /// <param name="leaseSeconds">The requested duration, zero for the default.</param>
    /// <param name="max">The maximum number of claims.</param>
    /// <returns>The durable claims.</returns>
    public async Task<IEnumerable<Occurrence>> Claim(string worker, int leaseSeconds, int max)
    {
        await QuarantineExhaustedLeases();
        return await storage.Claim(trigger, worker, clock.GetUtcNow(), Duration(leaseSeconds), Math.Clamp(max, 0, 1000), Math.Max(1, options.MaxInFlight));
    }

    /// <summary>
    /// Renews a current epoch, even if its lease has already expired.
    /// </summary>
    /// <param name="token">The fencing token.</param>
    /// <param name="leaseSeconds">The duration, zero for the default.</param>
    /// <returns>The fenced result.</returns>
    public async Task<OccurrenceLeaseResult> Renew(LeaseToken token, int leaseSeconds) => await BelongsToQueue(token)
        ? await storage.Renew(token, clock.GetUtcNow().Add(Duration(leaseSeconds))) : OccurrenceLeaseResult.OccurrenceLeaseLost;

    /// <summary>
    /// Completes only the current leased epoch, idempotently.
    /// </summary>
    /// <param name="token">The fencing token.</param>
    /// <returns>The fenced result.</returns>
    public async Task<OccurrenceLeaseResult> Complete(LeaseToken token) => await BelongsToQueue(token)
        ? await storage.Complete(token) : OccurrenceLeaseResult.OccurrenceLeaseLost;

    /// <summary>
    /// Fails a current epoch with backoff or quarantine.
    /// </summary>
    /// <param name="token">The fencing token.</param>
    /// <param name="error">The failure description.</param>
    /// <returns>The fenced result.</returns>
    public async Task<OccurrenceLeaseResult> Fail(LeaseToken token, string error)
    {
        var occurrence = await storage.Get(token.OccurrenceId);
        return occurrence is null || occurrence.Trigger != trigger ? OccurrenceLeaseResult.OccurrenceLeaseLost : await storage.Fail(token, error, NextAttempt(occurrence));
    }

    /// <summary>
    /// Parks exhausted expired attempts; storage fencing decides whether a concurrent completion won.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public async Task QuarantineExhaustedLeases()
    {
        var now = clock.GetUtcNow();
        foreach (var occurrence in (await storage.GetForTrigger(trigger)).Where(occurrence => occurrence.State == OccurrenceState.Leased && occurrence.LeaseExpiry <= now && options.MaxAttempts > 0 && occurrence.Attempts >= options.MaxAttempts))
        {
            await storage.Fail(new(occurrence.Id, occurrence.Epoch), "OccurrenceLeaseExpired", null);
        }
    }

    static TimeSpan Duration(int seconds) => TimeSpan.FromSeconds(seconds <= 0 ? 60 : Math.Min(seconds, 600));

    async Task<bool> BelongsToQueue(LeaseToken token) => (await storage.Get(token.OccurrenceId))?.Trigger == trigger;

    DateTimeOffset? NextAttempt(Occurrence occurrence) => options.MaxAttempts > 0 && occurrence.Attempts >= options.MaxAttempts
        ? null
        : clock.GetUtcNow().AddSeconds(Math.Min(Math.Max(0, options.MaximumBackoffDelay), Math.Max(0, options.BackoffDelay) * Math.Pow(Math.Max(1, options.ExponentialBackoffDelayFactor), Math.Max(0, occurrence.Attempts - 1))));
}
