// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Text.Json;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Occurrences;
using KernelOccurrences = KernelCore::Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Testing.Occurrences;

/// <summary>
/// Runs the real delivery policy without the grain's reminder/timer shell. Calls are serialized as Orleans turns.
/// </summary>
internal sealed class InProcessOccurrenceQueue : KernelOccurrences::IOccurrenceQueue, IDisposable
{
    readonly SemaphoreSlim _turn = new(1);
    readonly IOccurrencesStorage _storage;
    readonly TriggerId _trigger;
    readonly KernelOccurrences::OccurrenceDelivery _delivery;
    readonly ScenarioTime _time;

    /// <summary>
    /// Initializes a queue over the real delivery policy and storage.
    /// </summary>
    /// <param name="storage">The scenario storage.</param>
    /// <param name="key">The serialized queue key.</param>
    /// <param name="time">The occurrence clock.</param>
    internal InProcessOccurrenceQueue(IStorage storage, string key, ScenarioTime time)
    {
        var parsed = JsonSerializer.Deserialize<QueueKey>(key)!;
        var store = storage.GetEventStore(new(parsed.EventStore));
        _storage = parsed.Namespace is null ? store.Occurrences : store.GetNamespace(new(parsed.Namespace)).Occurrences;
        _trigger = new(parsed.Trigger);
        _time = time;
        _delivery = new(_storage, store.TriggerDefinitions, _trigger, new(), time);
    }

    /// <inheritdoc/>
    public Task<KernelOccurrences::OccurrenceIngressResult> Record(string idempotencyId, string payload, OccurrenceKind kind) => Turn(() => _delivery.Record(idempotencyId, payload, kind));

    /// <inheritdoc/>
    public Task<string> Claim(string worker, int leaseSeconds, int max) => Turn(async () => JsonSerializer.Serialize(await _delivery.Claim(worker, leaseSeconds, max)));

    /// <inheritdoc/>
    public Task<OccurrenceLeaseResult> Renew(string id, long epoch, int leaseSeconds) => Turn(() => _delivery.Renew(new(new(id), epoch), leaseSeconds));

    /// <inheritdoc/>
    public Task<OccurrenceLeaseResult> Complete(string id, long epoch) => Turn(() => _delivery.Complete(new(new(id), epoch)));

    /// <inheritdoc/>
    public Task<OccurrenceLeaseResult> Fail(string id, long epoch, string error) => Turn(() => _delivery.Fail(new(new(id), epoch), error));

    /// <inheritdoc/>
    public Task<bool> Retry(string id, string principal) => Turn(async () => (await _storage.Get(new(id)))?.Trigger == _trigger && await _storage.RetryQuarantined(new(id), principal, _time.Now));

    /// <inheritdoc/>
    public async Task<string?> Get(string id)
    {
        var occurrence = await _storage.Get(new(id));
        return occurrence?.Trigger == _trigger ? JsonSerializer.Serialize(occurrence) : null;
    }

    /// <inheritdoc/>
    public async Task<string> GetAll() => JsonSerializer.Serialize(await _storage.GetForTrigger(_trigger));

    /// <inheritdoc/>
    public Task Wake() => Turn(async () =>
    {
        await _delivery.QuarantineExhaustedLeases();
        return true;
    });

    /// <inheritdoc/>
    public Task Prepare() => Task.CompletedTask;

    /// <inheritdoc/>
    public void Dispose() => _turn.Dispose();

    async Task<T> Turn<T>(Func<Task<T>> action)
    {
        await _turn.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            _turn.Release();
        }
    }

    sealed record QueueKey(string EventStore, string? Namespace, string Trigger);
}
