// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Reflection;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.Occurrences;

/// <summary>
/// Builds synthetic handler contexts. Detached contexts have no durable lease; recorded contexts hold a real storage claim.
/// </summary>
/// <param name="time">The clock used by detached contexts.</param>
public sealed class OccurrenceContextBuilder(ScenarioTime time)
{
    readonly OccurrenceId _id = new(Guid.NewGuid().ToString());
    TriggerId _trigger = new("testing");
    OccurrenceId? _explicitId;
    string? _idempotencyId;
    OccurrenceKind _kind;
    DateTimeOffset? _recordedAt;
    DateTimeOffset? _dueAt;
    int _attempt = 1;
    long _epoch = 1;

    /// <summary>
    /// Initializes a builder with the fixed default scenario clock.
    /// </summary>
    public OccurrenceContextBuilder() : this(new ScenarioTime())
    {
    }

    /// <summary>
    /// Selects the identity declared on a trigger type.
    /// </summary>
    /// <typeparam name="TTrigger">The attributed trigger type.</typeparam>
    /// <returns>This builder.</returns>
    /// <exception cref="TriggerNotValid">The type has no trigger identity.</exception>
    public OccurrenceContextBuilder For<TTrigger>() => ForTrigger(typeof(TTrigger).GetCustomAttribute<TriggerAttribute>()?.Id ?? throw new TriggerNotValid("The context trigger needs a Trigger attribute."));

    /// <summary>
    /// Selects a trigger identity.
    /// </summary>
    /// <param name="trigger">The identity.</param>
    /// <returns>This builder.</returns>
    public OccurrenceContextBuilder ForTrigger(TriggerId trigger)
    {
        _trigger = trigger;
        return this;
    }

    /// <summary>
    /// Selects an explicit occurrence identity.
    /// </summary>
    /// <param name="id">The identity.</param>
    /// <returns>This builder.</returns>
    public OccurrenceContextBuilder WithId(OccurrenceId id)
    {
        _explicitId = id;
        return this;
    }

    /// <summary>
    /// Derives an identity using the kernel's ingress encoding and the trigger selected when the context is built,
    /// so the call order relative to <see cref="For{TTrigger}"/> and <see cref="ForTrigger"/> does not matter.
    /// An identity set with <see cref="WithId"/> wins over the derived one.
    /// </summary>
    /// <param name="idempotencyId">The caller's retry identity.</param>
    /// <returns>This builder.</returns>
    public OccurrenceContextBuilder WithIdempotencyId(string idempotencyId)
    {
        _idempotencyId = idempotencyId;
        return this;
    }

    /// <summary>
    /// Selects the occurrence kind.
    /// </summary>
    /// <param name="kind">The source kind.</param>
    /// <returns>This builder.</returns>
    public OccurrenceContextBuilder Kind(OccurrenceKind kind)
    {
        _kind = kind;
        return this;
    }

    /// <summary>
    /// Selects a recorded instant instead of scenario time.
    /// </summary>
    /// <param name="instant">The recorded instant.</param>
    /// <returns>This builder.</returns>
    public OccurrenceContextBuilder RecordedAt(DateTimeOffset instant)
    {
        _recordedAt = instant;
        return this;
    }

    /// <summary>
    /// Selects a scheduled due instant.
    /// </summary>
    /// <param name="instant">The due instant.</param>
    /// <returns>This builder.</returns>
    public OccurrenceContextBuilder DueAt(DateTimeOffset instant)
    {
        _dueAt = instant;
        return this;
    }

    /// <summary>
    /// Selects the attempt number (one by default).
    /// </summary>
    /// <param name="attempt">The attempt number.</param>
    /// <returns>This builder.</returns>
    public OccurrenceContextBuilder Attempt(int attempt)
    {
        _attempt = attempt;
        return this;
    }

    /// <summary>
    /// Selects the lease epoch (one by default).
    /// </summary>
    /// <param name="epoch">The fencing epoch.</param>
    /// <returns>This builder.</returns>
    public OccurrenceContextBuilder Epoch(long epoch)
    {
        _epoch = epoch;
        return this;
    }

    /// <summary>
    /// Builds a detached context. Receipts deduplicate, but completing this lease returns OccurrenceLeaseLost.
    /// </summary>
    /// <returns>The detached context.</returns>
    public OccurrenceContext BuildDetached() => Context(_recordedAt ?? time.Now);

    /// <summary>
    /// Seeds a synthetic occurrence and claims it in an otherwise empty trigger queue. No schema validation or handler runs.
    /// The builder's attempt and epoch become the first real claim's values; lease expiry uses scenario time.
    /// </summary>
    /// <param name="eventStore">The isolated store holding the lease.</param>
    /// <returns>The context with its durable fencing token.</returns>
    /// <exception cref="OccurrenceContextCannotBeClaimed">The queue is not empty or the context is invalid.</exception>
    public async Task<OccurrenceContext> RecordAndClaimIn(EventStoreForTesting eventStore)
    {
        var storage = eventStore.TestingStore.Store.GetNamespace(new KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName(eventStore.Namespace.Value)).Occurrences;
        if (_attempt < 1 || _epoch < 1 || (await storage.GetForTrigger(new(_trigger.Value))).Any()) throw new OccurrenceContextCannotBeClaimed();
        var at = _recordedAt ?? eventStore.Time.Now;
        var id = ResolveId();
        var recorded = await storage.Record(new(new(id.Value), new(_trigger.Value), (Storage.Occurrences.OccurrenceKind)_kind, "testing-context", "{}", 1, at, _dueAt, null, Storage.Occurrences.OccurrenceState.Pending, _attempt - 1, _epoch - 1, null, null, null, null));
        if (recorded != Storage.Occurrences.OccurrenceRecordResult.Recorded) throw new OccurrenceContextCannotBeClaimed();
        var claims = await storage.Claim(new(_trigger.Value), "scenario-context", eventStore.Time.Now, TimeSpan.FromSeconds(60), 1, 1);
        if (claims.SingleOrDefault()?.Id.Value != id.Value) throw new OccurrenceContextCannotBeClaimed();
        return Context(at, id) with { Namespace = eventStore.Namespace };
    }

    OccurrenceId ResolveId() => _explicitId ?? (_idempotencyId is null ? _id : new(Storage.Occurrences.OccurrenceIdentities.ForIngress(new(_trigger.Value), _idempotencyId).Value));

    OccurrenceContext Context(DateTimeOffset recordedAt) => Context(recordedAt, ResolveId());

    OccurrenceContext Context(DateTimeOffset recordedAt, OccurrenceId id) => new(id, _trigger, _kind, _dueAt, recordedAt, null, _attempt, new(id, _epoch));
}
