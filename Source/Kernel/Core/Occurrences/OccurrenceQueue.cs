// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Owns one trigger's active delivery coordination; durable storage remains the authority.
/// </summary>
/// <param name="storage">The infrastructure storage.</param>
/// <param name="options">The delivery policy.</param>
/// <param name="clock">The optional kernel clock.</param>
public class OccurrenceQueue(IStorage storage, IOptions<ChronicleOptions> options, TimeProvider? clock = null) : Grain, IOccurrenceQueue, IRemindable
{
    const string WakeReminder = "occurrence-wake";
    readonly TimeProvider _clock = clock ?? TimeProvider.System;
    IOccurrencesStorage _occurrences = null!;
    OccurrenceDelivery _delivery = null!;
    TriggerId _trigger = null!;
    IGrainTimer? _timer;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var key = JsonSerializer.Deserialize<OccurrenceQueueKey>(this.GetPrimaryKeyString())!;
        _trigger = new(key.Trigger);
        _occurrences = key.Storage(storage);
        _delivery = new(_occurrences, storage.GetEventStore(new(key.EventStore)).TriggerDefinitions, _trigger, options.Value.Occurrences, _clock);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<OccurrenceIngressResult> Record(string idempotencyId, string payload, OccurrenceKind kind)
    {
        // Install the persistent wake before writing: a lost ingress reply must not orphan durable work.
        await EnsureWake();
        return await _delivery.Record(idempotencyId, payload, kind);
    }

    /// <inheritdoc/>
    public async Task<string> Claim(string worker, int leaseSeconds, int max)
    {
        await EnsureWake();
        return JsonSerializer.Serialize(await _delivery.Claim(worker, leaseSeconds, max));
    }

    /// <inheritdoc/>
    public Task<OccurrenceLeaseResult> Renew(string id, long epoch, int leaseSeconds) => _delivery.Renew(new(new(id), epoch), leaseSeconds);

    /// <inheritdoc/>
    public Task<OccurrenceLeaseResult> Complete(string id, long epoch) => _delivery.Complete(new(new(id), epoch));

    /// <inheritdoc/>
    public Task<OccurrenceLeaseResult> Fail(string id, long epoch, string error) => _delivery.Fail(new(new(id), epoch), error);

    /// <inheritdoc/>
    public async Task<bool> Retry(string id, string principal)
    {
        if ((await _occurrences.Get(new(id)))?.Trigger != _trigger)
        {
            return false;
        }

        await EnsureWake();
        return await _occurrences.RetryQuarantined(new(id), principal, _clock.GetUtcNow());
    }

    /// <inheritdoc/>
    public async Task<string?> Get(string id)
    {
        var occurrence = await _occurrences.Get(new(id));
        return occurrence?.Trigger == _trigger ? JsonSerializer.Serialize(occurrence) : null;
    }

    /// <inheritdoc/>
    public async Task<string> GetAll() => JsonSerializer.Serialize(await _occurrences.GetForTrigger(_trigger));

    /// <inheritdoc/>
    public async Task Wake()
    {
        var hasWork = await _occurrences.HasPendingForTrigger(_trigger);
        if (hasWork)
        {
            await EnsureWake();
        }
        else
        {
            _timer?.Dispose();
            _timer = null;
            var reminder = await this.GetReminder(WakeReminder);
            if (reminder is not null)
            {
                await this.UnregisterReminder(reminder);
            }
        }
    }

    /// <inheritdoc/>
    public Task ReceiveReminder(string reminderName, TickStatus status) => reminderName == WakeReminder ? Wake() : Task.CompletedTask;

    async Task EnsureWake()
    {
        if (_timer is not null)
        {
            return;
        }

        await this.RegisterOrUpdateReminder(WakeReminder, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        _timer = this.RegisterGrainTimer(_ => Wake(), new() { DueTime = TimeSpan.FromSeconds(1), Period = TimeSpan.FromSeconds(1), Interleave = false });
    }
}
