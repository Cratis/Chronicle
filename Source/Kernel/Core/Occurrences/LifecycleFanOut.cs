// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Records immutable lifecycle snapshots and reconciles bounded namespace prefixes.
/// </summary>
/// <param name="store">The event-store storage.</param>
/// <param name="wake">Installs delivery wake-ups before each namespace write.</param>
/// <param name="clock">The kernel clock.</param>
/// <param name="logger">Reports intents left pending for retry.</param>
public class LifecycleFanOut(IEventStoreStorage store, Func<string?, string, Task> wake, TimeProvider clock, ILogger<LifecycleFanOut> logger)
{
    /// <summary>
    /// Records one logical activation without ever refreshing an existing namespace snapshot.
    /// </summary>
    /// <param name="kind">Startup or shutdown.</param>
    /// <param name="activationId">The host-supplied activation identity.</param>
    /// <param name="allNamespaces">Whether to fix a namespace snapshot.</param>
    /// <returns>The durable outcome and fixed namespace snapshot.</returns>
    public async Task<LifecycleOccurrenceResult> Record(OccurrenceKind kind, string activationId, bool allNamespaces)
    {
        var id = OccurrenceIdentities.ForLifecycle(kind, activationId);
        if (string.IsNullOrWhiteSpace(activationId) || kind is not (OccurrenceKind.Startup or OccurrenceKind.Shutdown))
        {
            return new(id.Value, OccurrenceIngressStatus.OccurrencePayloadInvalid, []);
        }
        if (allNamespaces && await store.LifecycleFanOut.Get(id) is { } existing)
        {
            return new(id.Value, OccurrenceIngressStatus.AlreadyRecorded, existing.Namespaces);
        }
        var trigger = new TriggerId(kind.ToString().ToLowerInvariant());
        var definition = await store.TriggerDefinitions.Get(trigger);
        if (definition is null) return new(id.Value, OccurrenceIngressStatus.OccurrencePayloadInvalid, []);
        var schema = await Schemas.JsonSchema.FromJsonAsync(definition.Schema);
        if (schema.Validate("{}").Count != 0) return new(id.Value, OccurrenceIngressStatus.OccurrencePayloadInvalid, []);
        var occurrence = new Occurrence(id, trigger, kind, OccurrencePayloadFingerprint.Calculate(trigger.Value, System.Text.Json.JsonSerializer.SerializeToElement(new { })), "{}", definition.Revision, clock.GetUtcNow(), null, null, OccurrenceState.Pending, 0, 0, null, null, null, null);
        if (!allNamespaces)
        {
            await wake(null, trigger.Value);
            var recorded = await store.Occurrences.Record(occurrence);
            var status = recorded switch
            {
                OccurrenceRecordResult.Recorded => OccurrenceIngressStatus.Recorded,
                OccurrenceRecordResult.AlreadyRecorded => OccurrenceIngressStatus.AlreadyRecorded,
                _ => OccurrenceIngressStatus.OccurrenceIdConflict
            };
            return new(id.Value, status, []);
        }
        var namespaces = (await store.Namespaces.GetAll()).Select(_ => _.Name.Value).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var inserted = await store.LifecycleFanOut.TryInsert(new(activationId, occurrence, namespaces));
        var durable = (await store.LifecycleFanOut.Get(id))!;
        return new(id.Value, inserted ? OccurrenceIngressStatus.Recorded : OccurrenceIngressStatus.AlreadyRecorded, durable.Namespaces);
    }

    /// <summary>
    /// Completes one bounded page; an interrupted prefix is safe to repeat after restart.
    /// </summary>
    /// <returns>Whether unfinished intents remain. A failed intent retains its acknowledged prefix for retry.</returns>
    public async Task<bool> Reconcile()
    {
        foreach (var intent in await store.LifecycleFanOut.GetPending(16))
        {
            try
            {
                await Reconcile(intent);
            }
            catch (Exception exception)
            {
                logger.FailedReconcilingLifecycleIntent(exception, intent.Occurrence.Id.Value);
            }
        }
        return (await store.LifecycleFanOut.GetPending(1)).Any();
    }

    async Task Reconcile(LifecycleFanOutIntent intent)
    {
        var end = Math.Min(intent.Progress + 100, intent.Namespaces.Count);
        for (var index = intent.Progress; index < end; index++)
        {
            var name = intent.Namespaces[index];
            var template = intent.Occurrence;
            await wake(name, template.Trigger.Value);
            var occurrence = template with { Id = OccurrenceIdentities.ForLifecycle(template.Kind, intent.ActivationId, name) };
            var result = await store.GetNamespace(new EventStoreNamespaceName(name)).Occurrences.Record(occurrence);
            if (result == OccurrenceRecordResult.OccurrenceIdConflict) throw new LifecycleOccurrenceNotValid("A lifecycle identity is occupied by different content.");
        }
        await store.LifecycleFanOut.Advance(intent.Occurrence.Id, end);
    }
}
