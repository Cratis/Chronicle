// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Registrations;
using Cratis.Chronicle.Supervision;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Represents an implementation of <see cref="IRegistrationInbox"/>.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> that keeps the intents.</param>
/// <param name="payloads">The <see cref="IRegistrationPayloads"/> that write the content of a registration.</param>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>. Defaults to the system clock.</param>
/// <param name="supervisor">The optional <see cref="ISiloSupervisor"/> that resumes unapplied registrations on a schedule.</param>
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "No wait handle is used; the source is only cancelled on deactivation.")]
public class RegistrationInbox(
    IStorage storage,
    IRegistrationPayloads payloads,
    IOptions<ChronicleOptions> options,
    ILogger<RegistrationInbox> logger,
    TimeProvider? timeProvider = null,
    ISiloSupervisor? supervisor = null) : Grain, IRegistrationInbox
{
    static readonly TimeSpan _resumeInterval = TimeSpan.FromSeconds(30);

    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    readonly Dictionary<(RegistrationKind Kind, string Key), RegistrationIntent> _intents = [];
    readonly Dictionary<(RegistrationKind Kind, string Key), DateTimeOffset> _retryAt = [];
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "No wait handle is used; the source is only cancelled on deactivation.")]
    readonly CancellationTokenSource _stopping = new();
    EventStoreName _eventStore = EventStoreName.NotSet;
    IRegistrationIntentStorage? _storage;
    string _supervisionKey = string.Empty;
    long _revision;
    Task _worker = Task.CompletedTask;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventStore = this.GetPrimaryKeyString();
        try
        {
            _storage = storage.GetEventStore(_eventStore).RegistrationIntents;
        }
        catch (RegistrationIntentsStorageNotSupported)
        {
            logger.IntentsNotSupported(_eventStore);
            return;
        }

        foreach (var intent in await _storage.GetAll())
        {
            _intents[(intent.Kind, intent.Key)] = intent;
            _revision = Math.Max(_revision, intent.Revision);
        }

        // One supervisor loop per silo resumes what a lost silo left unapplied; the grain arms no timer of its own.
        _supervisionKey = $"registration-inbox:{_eventStore}";
        if (supervisor is not null)
        {
            var self = GrainFactory.GetGrain<IRegistrationInbox>(_eventStore.Value);
            supervisor.Register(
                _supervisionKey,
                _resumeInterval,
                async () =>
                {
                    await self.Ensure();
                    return null;
                },
                null);
        }

        var pending = _intents.Values.Count(intent => intent.State == RegistrationIntentState.Pending);
        if (pending > 0)
        {
            logger.ResumingPending(_eventStore, pending);
            StartWorker();
        }
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        supervisor?.Unregister(_supervisionKey);
        await _stopping.CancelAsync();
    }

    /// <inheritdoc/>
    public Task Ensure()
    {
        if (_storage is not null && _intents.Values.Any(intent => intent.State == RegistrationIntentState.Pending))
        {
            StartWorker();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<RegistrationAcceptance?> AcceptProjections(string signature, IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner, string? registrant, string? version)
    {
        if (_storage is null)
        {
            return null;
        }

        var registrantName = registrant ?? string.Empty;

        // Only a full set from an identified registrant replaces what that registrant sent before. Other registrations
        // are partial: each is kept under its own content, because the one before it is still wanted.
        var key = fullSetOwner is not null ? registrantName : $"{registrantName}|{signature}";
        var slot = (RegistrationKind.Projections, key);
        if (_intents.TryGetValue(slot, out var existing) && existing.Hash == signature && existing.State != RegistrationIntentState.Failed)
        {
            logger.Unchanged(_eventStore, registrantName, existing.Revision);
            return new RegistrationAcceptance(existing.Revision, existing.State);
        }

        var intent = new RegistrationIntent(
            RegistrationKind.Projections,
            key,
            registrantName,
            _revision + 1,
            signature,
            RegistrationIntentState.Pending,
            payloads.Serialize([.. definitions]),
            fullSetOwner is null ? null : (int)fullSetOwner.Value,
            version,
            0,
            null,
            _clock.GetUtcNow());

        await _storage.Save(intent);
        _revision = intent.Revision;
        _intents[slot] = intent;
        _retryAt.Remove(slot);
        logger.Accepted(_eventStore, intent.Kind, intent.Revision, registrantName);
        StartWorker();
        return new RegistrationAcceptance(intent.Revision, intent.State);
    }

    /// <inheritdoc/>
    public Task<RegistrationStatus> GetStatus(RegistrationKind kind, string registrant)
    {
        var mine = _intents.Values.Where(intent => intent.Kind == kind && intent.Registrant == registrant).OrderBy(intent => intent.Revision).ToArray();
        if (mine.Length == 0)
        {
            return Task.FromResult(new RegistrationStatus(RegistrationIntentState.Applied, 0, 0, null));
        }

        var latest = mine[^1];
        var applied = mine.LastOrDefault(intent => intent.State == RegistrationIntentState.Applied)?.Revision ?? 0;
        var state = RegistrationIntentState.Applied;
        if (latest.State == RegistrationIntentState.Failed)
        {
            state = RegistrationIntentState.Failed;
        }
        else if (mine.Any(intent => intent.State == RegistrationIntentState.Pending))
        {
            state = RegistrationIntentState.Pending;
        }

        return Task.FromResult(new RegistrationStatus(state, latest.Revision, applied, state == RegistrationIntentState.Failed ? latest.Failure : null));
    }

    void StartWorker()
    {
        if (!_worker.IsCompleted)
        {
            return;
        }

        _worker = Drain(_stopping.Token);
    }

    async Task Drain(CancellationToken cancellationToken)
    {
        // Bounded: one registration is applied at a time, in the order they were accepted. A failing one is set aside
        // for a retry and does not hold back the ones behind it.
        while (!cancellationToken.IsCancellationRequested)
        {
            var now = _clock.GetUtcNow();
            var pending = _intents.Values.Where(intent => intent.State == RegistrationIntentState.Pending).OrderBy(intent => intent.Revision).ToArray();
            var next = pending.FirstOrDefault(intent => !_retryAt.TryGetValue((intent.Kind, intent.Key), out var at) || at <= now);
            if (next is null)
            {
                if (pending.Length == 0)
                {
                    return;
                }

                var earliest = pending.Min(intent => _retryAt[(intent.Kind, intent.Key)]);
                try
                {
                    await Task.Delay(earliest - now, _clock, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                continue;
            }

            await Apply(next, cancellationToken);
        }
    }

    async Task Apply(RegistrationIntent intent, CancellationToken cancellationToken)
    {
        try
        {
            var definitions = payloads.Deserialize(intent.Payload);
            ProjectionOwner? fullSetOwner = intent.FullSetOwner is { } owner ? (ProjectionOwner)owner : null;
            var registrant = string.IsNullOrEmpty(intent.Registrant) ? null : intent.Registrant;
            var manager = GrainFactory.GetGrain<IProjectionsManager>(_eventStore.Value);
            await SharedProjectionRegistration.Register(manager, intent.Hash, definitions, fullSetOwner, registrant, intent.Version).WaitAsync(cancellationToken);
            await Record(intent with { State = RegistrationIntentState.Applied, Payload = ReadOnlyMemory<byte>.Empty, Failure = null, UpdatedAt = _clock.GetUtcNow() });
            logger.Applied(_eventStore, intent.Kind, intent.Revision, intent.Registrant);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            await Fail(intent, exception);
        }
    }

    async Task Fail(RegistrationIntent intent, Exception exception)
    {
        var settings = options.Value.Registration;
        var attempts = intent.Attempts + 1;
        var maxAttempts = Math.Max(1, settings.MaxApplyAttempts);
        var exhausted = attempts >= maxAttempts;
        if (exhausted)
        {
            logger.ApplyFailed(exception, _eventStore, intent.Kind, intent.Revision, intent.Registrant, attempts);
        }
        else
        {
            logger.ApplyFailedWillRetry(exception, _eventStore, intent.Kind, intent.Revision, intent.Registrant, attempts, maxAttempts);
        }

        var updated = intent with
        {
            State = exhausted ? RegistrationIntentState.Failed : RegistrationIntentState.Pending,
            Attempts = attempts,
            Failure = exception.Message,
            UpdatedAt = _clock.GetUtcNow()
        };
        if (!exhausted)
        {
            _retryAt[(intent.Kind, intent.Key)] = _clock.GetUtcNow() + TimeSpan.FromSeconds(Math.Max(1, settings.ApplyRetryDelaySeconds));
        }

        await Record(updated);
    }

    async Task Record(RegistrationIntent outcome)
    {
        var slot = (outcome.Kind, outcome.Key);

        // A newer registration for the same key was accepted while this one was being applied: it is the one that counts.
        if (!_intents.TryGetValue(slot, out var current) || current.Revision != outcome.Revision)
        {
            return;
        }

        _intents[slot] = outcome;
        try
        {
            await _storage!.Save(outcome);
        }
        catch (Exception exception)
        {
            logger.RecordingOutcomeFailed(exception, _eventStore, outcome.Revision);
        }
    }
}
