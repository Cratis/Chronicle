// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// The reason reported when a catch-up was deferred because the observer's client is not connected.
    /// </summary>
    internal const string NoClientDeferral = "no_client";

    /// <summary>
    /// The reason reported when a catch-up was deferred because too many catch-up jobs are running.
    /// </summary>
    internal const string CapacityDeferral = "capacity";

    static readonly TimeSpan _catchUpDeferralBase = TimeSpan.FromSeconds(5);
    static readonly TimeSpan _catchUpDeferralJitter = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets whether the observer is owned by a client application and none of its instances is connected.
    /// </summary>
    /// <remarks>
    /// Such an observer can only fail: there is nobody to deliver to, so a catch-up or retry job started for it costs the
    /// kernel work and produces a failed job. It stays behind instead; its client subscribing again routes it, and the
    /// catch-up starts then.
    /// </remarks>
    bool IsWithoutClient =>
        Definition.Owner == ObserverOwner.Client &&
        !(_subscription.IsSubscribed && (_subscription.Targets.Count > 0 || _subscription.Arguments is ConnectedClient));

    [LoggerMessage(LogLevel.Information, "Not starting catch-up: the observer is owned by a client and none is connected. It catches up when its client subscribes again")]
    static partial void DeferringCatchUpWithoutClient(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Deferring catch-up: {Limit} catch-up jobs are already running in the namespace. Trying again in {Delay}")]
    static partial void DeferringCatchUpForCapacity(ILogger logger, int limit, TimeSpan delay);

    [LoggerMessage(LogLevel.Information, "Not starting catch-up for partition {Partition}: the observer is owned by a client and none is connected")]
    static partial void DeferringPartitionCatchUpWithoutClient(ILogger logger, Key partition);

    [LoggerMessage(LogLevel.Information, "Not retrying failed partition {Partition}: the observer is owned by a client and none is connected")]
    static partial void DeferringRecoveryWithoutClient(ILogger logger, Key partition);

    /// <summary>
    /// Admits the observer to start a catch-up job, or defers the catch-up.
    /// </summary>
    /// <returns>A lease to hold while the job is acquired, or null when the catch-up was deferred.</returns>
    /// <remarks>
    /// An observer without a client is left behind without a retry: nothing it can do now would succeed, and the client
    /// subscribing again routes it. An observer held back by the limit settles into observing for a short delay, like a
    /// catch-up that moved nothing, and the watchdog routes it again once the delay has passed. The places that come free go
    /// to the observers furthest behind (see <see cref="CatchUpAdmission"/>); the delay is only jittered so that the held-back
    /// observers do not all ask at the same moment.
    /// </remarks>
    async Task<IDisposable?> AdmitCatchUpOrDefer()
    {
        if (IsWithoutClient)
        {
            DeferringCatchUpWithoutClient(logger);
            await EndDeferredCatchUp(NoClientDeferral);
            return null;
        }

        if (catchUpAdmission is null)
        {
            return NoAdmissionLease.Instance;
        }

        var config = await configurationProvider.GetFor(_observerKey);
        var lease = await catchUpAdmission.TryAdmit(_observerKey, config.MaxConcurrentCatchUpJobs, await GetEventsBehind());
        if (lease is not null)
        {
            return lease;
        }

        var delay = _catchUpDeferralBase + TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * _catchUpDeferralJitter.TotalMilliseconds);
        DeferringCatchUpForCapacity(logger, config.MaxConcurrentCatchUpJobs, delay);
        _catchUpBackoffUntil = DateTimeOffset.UtcNow + delay;
        await EndDeferredCatchUp(CapacityDeferral);
        return null;
    }

    async Task<ulong> GetEventsBehind()
    {
        try
        {
            var tail = await GetTailForWatchdog();
            if (!tail.IsActualValue)
            {
                return 0;
            }

            var next = State.NextEventSequenceNumber;
            return next.IsActualValue ? (tail.Value >= next.Value ? tail.Value - next.Value + 1 : 0) : tail.Value + 1;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // How far behind it is only orders the observers; not knowing must not stop it from catching up.
            return 0;
        }
    }

    async Task EndDeferredCatchUp(string reason)
    {
        _metrics?.CatchUpDeferred(reason);
        _isPreparingCatchup = false;
        NotePreparationEnded();
        await RepairMissedDeliveries();
    }

    sealed class NoAdmissionLease : IDisposable
    {
        internal static readonly NoAdmissionLease Instance = new();

        public void Dispose()
        {
        }
    }
}
