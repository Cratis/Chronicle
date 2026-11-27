// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Owns one namespace's serialized lifecycle and durable recovery wake.</summary>
/// <param name="storage">The infrastructure storage.</param>
/// <param name="incarnations">The deployment and provider capability gate.</param>
/// <param name="steps">The existing removal checkpoints.</param>
/// <param name="client">The cluster client for namespace notifications.</param>
/// <param name="logger">The recovery diagnostic logger.</param>
/// <param name="clock">The optional kernel clock.</param>
[GrainType("namespace-incarnation")]
public sealed class NamespaceIncarnation(
    IStorage storage,
    INamespaceIncarnations incarnations,
    INamespaceRemovalSteps steps,
    IClusterClient client,
    ILogger<NamespaceIncarnation> logger,
    TimeProvider? clock = null) : Grain, INamespaceIncarnation, IRemindable, INamespaceIncarnationEffects
{
    const string WakeReminderPrefix = "namespace-incarnation-wake:";
    readonly TimeProvider _clock = clock ?? TimeProvider.System;
    readonly HashSet<long> _durableWakes = [];
    EventStoreName _eventStore = null!;
    EventStoreNamespaceName _name = null!;
    IEventStoreStorage _store = null!;
    IHaveNamespaceIncarnations _capability = null!;
    NamespaceIncarnationCoordinator? _coordinator;
    IGrainTimer? _timer;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var key = JsonSerializer.Deserialize<NamespaceIncarnationKey>(this.GetPrimaryKeyString())!;
        _eventStore = key.EventStore;
        _name = key.Namespace;
        _capability = incarnations.GetStorage(_eventStore);
        _store = storage.GetEventStore(_eventStore);
        var state = await _capability.NamespaceLifecycles.Get(_name);
        if (state?.Status is NamespaceLifecycleStatus.Preparing or NamespaceLifecycleStatus.Closing) ArmTimer();
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <exception cref="NamespaceLifecycleChanged">The requested namespace does not belong to this owner.</exception>
    /// <exception cref="InvalidNamespaceName">An unlisted name contains a comma.</exception>
    public async Task<EventStoreNamespaceName> Ensure(EventStoreNamespaceName @namespace)
    {
        if (!_name.Value.Equals(@namespace.Value, StringComparison.OrdinalIgnoreCase)) throw new NamespaceLifecycleChanged(@namespace);
        var listed = (await _store.Namespaces.GetAll()).FirstOrDefault(_ => _.Name.Value.Equals(@namespace.Value, StringComparison.OrdinalIgnoreCase));
        var current = await _capability.NamespaceLifecycles.Get(_name);
        var canonical = current?.Namespace ?? listed?.Name ?? @namespace;
        if (canonical.Value.Equals(EventStoreNamespaceName.Default.Value, StringComparison.OrdinalIgnoreCase)) canonical = EventStoreNamespaceName.Default;
        if (listed is null && NamespaceNameRules.ContainsComma(canonical.Value)) throw new InvalidNamespaceName(_eventStore, canonical);
        var opened = await Coordinator(canonical).Ensure(listed is not null);
        await ClearTerminalWake(opened);
        return opened.Namespace;
    }

    /// <inheritdoc/>
    /// <exception cref="NamespaceCannotBeRemoved">The default namespace cannot be removed.</exception>
    /// <exception cref="NamespaceIncarnationUnavailable">No initialized lifecycle exists for this owner.</exception>
    /// <exception cref="NamespaceClosing">Removal is pending, not acknowledged deletion.</exception>
    /// <exception cref="NamespaceRecoveryRequired">Removal requires explicit reconciliation.</exception>
    public async Task Remove()
    {
        GuardDefaultNamespace();
        var current = await _capability.NamespaceLifecycles.Get(_name) ?? throw new NamespaceIncarnationUnavailable(_eventStore);
        var result = await Coordinator(current.Namespace).Remove();
        await ClearTerminalWake(result);
        if (result.Status == NamespaceLifecycleStatus.RecoveryRequired) throw new NamespaceRecoveryRequired(result.Namespace);
        if (result.Status != NamespaceLifecycleStatus.Removed) throw new NamespaceClosing(result.Namespace);
    }

    /// <inheritdoc/>
    /// <exception cref="NamespaceIncarnationUnavailable">No lifecycle exists.</exception>
    /// <exception cref="NamespaceRecoveryRequired">The resumed operation still requires reconciliation.</exception>
    public async Task Resume(Guid operation, long revision)
    {
        GuardDefaultNamespace();
        var current = await _capability.NamespaceLifecycles.Get(_name) ?? throw new NamespaceIncarnationUnavailable(_eventStore);
        await Coordinator(current.Namespace).Resume(operation, revision);
        await Wake();
        if ((await _capability.NamespaceLifecycles.Get(_name))?.Status == NamespaceLifecycleStatus.RecoveryRequired) throw new NamespaceRecoveryRequired(_name);
    }

    /// <inheritdoc/>
    /// <exception cref="NamespaceIncarnationUnavailable">No lifecycle exists.</exception>
    public async Task Abort(Guid operation, long revision)
    {
        GuardDefaultNamespace();
        var current = await _capability.NamespaceLifecycles.Get(_name) ?? throw new NamespaceIncarnationUnavailable(_eventStore);
        await Coordinator(current.Namespace).Abort(operation, revision);
        await Ensure(current.Namespace);
    }

    /// <inheritdoc/>
    public async Task Wake()
    {
        var state = await _capability.NamespaceLifecycles.Get(_name);
        if (state?.Status == NamespaceLifecycleStatus.Preparing)
        {
            await Ensure(state.Namespace);
            return;
        }
        if (state?.Status == NamespaceLifecycleStatus.Closing)
        {
            GuardDefaultNamespace();
            state = await Coordinator(state.Namespace).Remove();
        }
        await ClearTerminalWake(state);
    }

    /// <inheritdoc/>
    public Task ReceiveReminder(string reminderName, TickStatus status) => WakeRevision(reminderName).HasValue ? Wake() : Task.CompletedTask;

    /// <inheritdoc/>
    Task INamespaceIncarnationEffects.EnsureWake(long revision) => EnsureWake(revision);

    /// <inheritdoc/>
    async Task INamespaceIncarnationEffects.Create(NamespaceLifecycle state)
    {
        var added = new NamespaceAdded(_eventStore, state.Namespace);
        var result = await GrainFactory.GetSystemEventSequence().Append($"{_eventStore}/{state.Namespace}", added);
        if (!result.IsSuccess) throw new NamespaceAddedCouldNotBeAppended(_eventStore, state.Namespace);
        await _store.Namespaces.Create(state.Namespace, state.Created);
        var channel = client.GetBroadcastChannelProvider(WellKnownBroadcastChannelNames.NamespaceAdded);
        await channel.GetChannelWriter<NamespaceAdded>(ChannelId.Create(WellKnownBroadcastChannelNames.NamespaceAdded, _eventStore)).Publish(added);
    }

    /// <inheritdoc/>
    Task INamespaceIncarnationEffects.Remove(NamespaceLifecycle state) => state.Stage == NamespaceRemovalStage.Forget
        ? steps.ForgetForIncarnation(_eventStore, state)
        : steps.Execute(_eventStore, state.Namespace, state.Stage);

    static long? WakeRevision(string name) =>
        name.StartsWith(WakeReminderPrefix, StringComparison.Ordinal) &&
        long.TryParse(name.AsSpan(WakeReminderPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var revision) && revision > 0
            ? revision
            : null;

    NamespaceIncarnationCoordinator Coordinator(EventStoreNamespaceName name) => _coordinator ??= new(
        name, _capability.NamespaceLifecycles, _capability.GetNamespaceMarker(name), this, _clock);

    void GuardDefaultNamespace()
    {
        if (_name.Value.Equals(EventStoreNamespaceName.Default.Value, StringComparison.OrdinalIgnoreCase)) throw new NamespaceCannotBeRemoved(_eventStore, EventStoreNamespaceName.Default);
    }

    void ArmTimer() => _timer ??= this.RegisterGrainTimer(_ => Wake(), new()
    {
        DueTime = TimeSpan.FromSeconds(1),
        Period = TimeSpan.FromSeconds(5),
        Interleave = false
    });

    async Task EnsureWake(long revision)
    {
        if (!_durableWakes.Contains(revision))
        {
            await this.RegisterOrUpdateReminder($"{WakeReminderPrefix}{revision.ToString(CultureInfo.InvariantCulture)}", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            _durableWakes.Add(revision);
        }
        ArmTimer();
    }

    async Task ClearTerminalWake(NamespaceLifecycle? state)
    {
        if (state is null || state.Status is NamespaceLifecycleStatus.Preparing or NamespaceLifecycleStatus.Closing) return;
        if (state.Status == NamespaceLifecycleStatus.RecoveryRequired) logger.NamespaceRecoveryRequired(state.Namespace, state.LastFailure);
        var outstanding = false;
        foreach (var reminder in await this.GetReminders())
        {
            if (WakeRevision(reminder.ReminderName) is not { } revision) continue;

            // A missing or predecessor row cannot disprove a pending CAS whose outcome is still unknown.
            if (revision > state.Revision)
            {
                outstanding = true;
                continue;
            }
            await this.UnregisterReminder(reminder);
            _durableWakes.Remove(revision);
        }
        if (outstanding) return;
        _timer?.Dispose();
        _timer = null;
    }
}
