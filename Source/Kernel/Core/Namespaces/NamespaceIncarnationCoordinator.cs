// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Coordinates one canonical namespace's creation and bounded removal from durable control state.</summary>
/// <param name="name">The canonical, validated namespace name.</param>
/// <param name="lifecycle">The control storage outside the namespace database.</param>
/// <param name="marker">The marker for this namespace, never a separate append-admission check.</param>
/// <param name="effects">The durable wake, creation, and removal effects.</param>
/// <param name="clock">The kernel clock.</param>
/// <remarks>
/// The owning grain must serialize Ensure, Remove, and recovery calls and refuse default-namespace removal before calling
/// this coordinator. Creation effects must not call back into an INamespaces grain awaiting Ensure. A lost effect reply
/// leaves Preparing intact, allowing the same reserved identity to be recovered, never replaced by a new identity.
/// </remarks>
public sealed partial class NamespaceIncarnationCoordinator(
    EventStoreNamespaceName name,
    INamespaceLifecycleStorage lifecycle,
    INamespaceMarkerStorage marker,
    INamespaceIncarnationEffects effects,
    TimeProvider clock)
{
    readonly NamespaceRemovalRecovery _recovery = new(
        lifecycle,
        async state =>
        {
            if (state.Stage == NamespaceRemovalStage.CloseMarker)
            {
                if (!await marker.TryClose(state.Incarnation)) throw new NamespaceRecoveryRequired(state.Namespace);
            }
            else
            {
                await effects.Remove(state);
            }
        },
        clock);

    /// <summary>Installs the reserved marker, acknowledges creation, and opens admission last.</summary>
    /// <param name="listed">Whether an uninitialized legacy namespace is already listed; never overrides persisted intent.</param>
    /// <returns>The acknowledged open lifecycle with its stable identity.</returns>
    /// <exception cref="NamespaceClosing">Admission is closed by a pending removal.</exception>
    /// <exception cref="NamespaceRecoveryRequired">The lifecycle or marker requires reconciliation.</exception>
    /// <exception cref="NamespaceLifecycleChanged">A conditional control write lost to another revision.</exception>
    public async Task<NamespaceLifecycle> Ensure(bool listed)
    {
        var current = await lifecycle.Get(name);
        if (current?.Status == NamespaceLifecycleStatus.Closing) throw new NamespaceClosing(current.Namespace);
        if (current?.Status == NamespaceLifecycleStatus.RecoveryRequired) throw new NamespaceRecoveryRequired(current.Namespace);
        if (current?.Status == NamespaceLifecycleStatus.Open) return await VerifyOpen(current);
        if (current is null || current.Status == NamespaceLifecycleStatus.Removed) current = await Reserve(current, listed);
        else await effects.EnsureWake(current.Revision);
        if (!await marker.TryOpen(current.Incarnation, current.PreviousIncarnation))
        {
            await Save(current, current.RequireRecovery("NamespaceMarkerOpenNotAcknowledged"));
            throw new NamespaceRecoveryRequired(current.Namespace);
        }
        if (current.NotifyOnOpen) await effects.Create(current);
        return await Save(current, current with { Revision = current.Revision + 1, Status = NamespaceLifecycleStatus.Open, NotifyOnOpen = false });
    }

    /// <summary>Closes admission before driving one bounded removal attempt, preserving terminal outcomes.</summary>
    /// <returns>The latest removal state; Closing and RecoveryRequired are not successful deletion.</returns>
    /// <exception cref="NamespaceLifecycleChanged">A conditional control write lost or the control state disappeared.</exception>
    public async Task<NamespaceLifecycle> Remove()
    {
        var current = await lifecycle.Get(name);
        if (current is null || current.Status == NamespaceLifecycleStatus.Preparing) current = await Ensure(true);
        if (current.Status is NamespaceLifecycleStatus.Removed or NamespaceLifecycleStatus.RecoveryRequired) return current;
        await effects.EnsureWake(current.Status == NamespaceLifecycleStatus.Open ? current.Revision + 1 : current.Revision);
        if (current.Status == NamespaceLifecycleStatus.Open)
        {
            await Save(current, current.BeginRemoval(Guid.NewGuid(), clock.GetUtcNow()));
        }
        return await _recovery.Tick(name) ?? throw new NamespaceLifecycleChanged(name);
    }

    async Task<NamespaceLifecycle> Reserve(NamespaceLifecycle? previous, bool listed)
    {
        var prepared = new NamespaceLifecycle(previous?.Namespace ?? name, NamespaceIncarnationId.New(), clock.GetUtcNow())
        {
            Revision = (previous?.Revision ?? 0) + 1,
            PreviousIncarnation = previous?.Incarnation,
            NotifyOnOpen = previous is not null || !listed
        };
        await effects.EnsureWake(prepared.Revision);
        if (!await lifecycle.TryWrite(name, previous?.Revision, prepared)) throw new NamespaceLifecycleChanged(name);
        return prepared;
    }

    async Task<NamespaceLifecycle> VerifyOpen(NamespaceLifecycle current)
    {
        var installed = await marker.Get();
        if (installed is { IsOpen: true } && installed.Incarnation == current.Incarnation) return current;
        await Save(current, current.RequireRecovery("NamespaceOpenMarkerMismatch"));
        throw new NamespaceRecoveryRequired(current.Namespace);
    }

    async Task<NamespaceLifecycle> Save(NamespaceLifecycle previous, NamespaceLifecycle next) =>
        await lifecycle.TryWrite(name, previous.Revision, next) ? next : throw new NamespaceLifecycleChanged(name);
}
