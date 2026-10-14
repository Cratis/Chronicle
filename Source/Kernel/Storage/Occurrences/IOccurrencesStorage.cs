// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Defines direct infrastructure storage for durable application work.
/// </summary>
public interface IOccurrencesStorage
{
    /// <summary>
    /// Inserts an occurrence without replacing an existing identity. Compares the fingerprint on replay.
    /// </summary>
    /// <param name="occurrence">The validated ingress facts.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<OccurrenceRecordResult> Record(Occurrence occurrence);

    /// <summary>
    /// Reads durable delivery facts.
    /// </summary>
    /// <param name="id">The occurrence identity.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<Occurrence?> Get(OccurrenceId id);

    /// <summary>
    /// Reads the occurrences for a trigger.
    /// </summary>
    /// <param name="trigger">The trigger identity.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<IEnumerable<Occurrence>> GetForTrigger(TriggerId trigger);

    /// <summary>
    /// Atomically claims due work in due or recording order, incrementing epoch and attempts.
    /// </summary>
    /// <param name="trigger">The trigger identity.</param>
    /// <param name="worker">The worker identity.</param>
    /// <param name="now">The kernel instant.</param>
    /// <param name="duration">The lease duration.</param>
    /// <param name="max">The maximum claims.</param>
    /// <param name="maxInFlight">The maximum live leases per trigger.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<IEnumerable<Occurrence>> Claim(TriggerId trigger, string worker, DateTimeOffset now, TimeSpan duration, int max, int maxInFlight);

    /// <summary>
    /// Renews the current epoch, including an expired but unsuperseded lease.
    /// </summary>
    /// <param name="token">The lease token.</param>
    /// <param name="expiry">The new kernel expiry.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<OccurrenceLeaseResult> Renew(LeaseToken token, DateTimeOffset expiry);

    /// <summary>
    /// Completes the current epoch; repeating the completing epoch is idempotent.
    /// </summary>
    /// <param name="token">The lease token.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<OccurrenceLeaseResult> Complete(LeaseToken token);

    /// <summary>
    /// Fails the current leased epoch without changing its attempt count.
    /// </summary>
    /// <param name="token">The lease token.</param>
    /// <param name="error">The error.</param>
    /// <param name="nextAttempt">The retry instant; null means quarantine.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<OccurrenceLeaseResult> Fail(LeaseToken token, string error, DateTimeOffset? nextAttempt);

    /// <summary>
    /// Re-arms a quarantined occurrence, preserving the fencing epoch.
    /// </summary>
    /// <param name="id">The occurrence identity.</param>
    /// <returns>The durable storage outcome.</returns>
    Task<bool> RetryQuarantined(OccurrenceId id);
}
