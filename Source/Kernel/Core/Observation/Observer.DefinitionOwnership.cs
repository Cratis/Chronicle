// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Gets whether this activation's key addresses the event sequence the stored definition is on.
    /// </summary>
    /// <remarks>
    /// Definition and state are stored by observer identifier alone, so an activation under another event sequence
    /// loads the record of the real observer. Such an activation is inert: it never counts as an existing observer,
    /// never persists state or the definition, and rejects operator operations. A definition without an event
    /// sequence belongs to no sequence yet, so any key owns it.
    /// </remarks>
    bool KeyOwnsDefinition => Definition.IsOwnedBy(_observerKey.EventSequenceId);

    /// <summary>
    /// Claims the definition for the event sequence of this key.
    /// </summary>
    /// <remarks>
    /// Only a registration - a subscription - may do this. Registering an observer on another event sequence is how
    /// it legitimately moves, and the subscription persists the definition on the new sequence.
    /// </remarks>
    async Task<DefinitionOwnershipSwitch?> ClaimDefinitionForKey()
    {
        var from = Definition.EventSequenceId;
        var ownershipChanged = !KeyOwnsDefinition;
        var currentNamespaceResult = SequenceHandoverResult.Claimed;
        observerDefinition.State = observerDefinition.State with { EventSequenceId = _observerKey.EventSequenceId };
        if (ownershipChanged)
        {
            // Failed-partition cleanup checks the stored definition, not this activation's claimed copy.
            await observerDefinition.WriteStateAsync();
            var eventStore = storage.GetEventStore(_observerKey.EventStore);
            var namespaces = await eventStore.Namespaces.GetAll();
            foreach (var @namespace in namespaces)
            {
                var result = await ObserverSequenceHandover.HandOver(
                    eventStore.GetNamespace(@namespace.Name),
                    _observerId,
                    from,
                    _observerKey.EventSequenceId,
                    logger);
                if (@namespace.Name == _observerKey.Namespace)
                {
                    currentNamespaceResult = result;
                }
            }

            var change = new DefinitionOwnershipSwitch(
                _observerId,
                from,
                _observerKey.EventSequenceId,
                _pendingPrecedence?.Outcome ?? RegistrationPrecedenceOutcome.NoConflict,
                _incomingRegistrationVersion,
                _pendingPrecedence?.OwnerVersion ?? string.Empty);
            await ReportOwnershipSwitch(change, namespaces.Select(@namespace => @namespace.Name));
            _pendingPrecedence = null;
            await ApplyHandoverResult(currentNamespaceResult);
            return change;
        }

        _pendingPrecedence = null;

        // The definition already moved, so a stored owner on another sequence is a handover that was interrupted.
        if (State.Owner is { } owner && owner != _observerKey.EventSequenceId)
        {
            var interrupted = await ObserverSequenceHandover.HandOver(
                storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace),
                _observerId,
                owner,
                _observerKey.EventSequenceId,
                logger);
            await ApplyHandoverResult(interrupted);
        }

        return null;
    }

    /// <summary>
    /// Brings this activation in line with the outcome of handing its namespace's progress over to the key's sequence.
    /// </summary>
    /// <param name="result">The outcome of the handover for this activation's namespace.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The progress loaded before the handover is stale once the target sequence's progress is selected, also when a
    /// concurrent registration through another namespace selected it first. A handover that did not complete leaves the
    /// stored progress with another sequence, so the subscription must not continue on this one.
    /// </remarks>
    async Task ApplyHandoverResult(SequenceHandoverResult result)
    {
        if (result == SequenceHandoverResult.NotHandedOver)
        {
            throw new ObserverSequenceHandoverDidNotComplete(_observerKey);
        }

        if (result is SequenceHandoverResult.HandedOver or SequenceHandoverResult.AlreadyOnTarget)
        {
            await RefreshAfterHandover();
        }
    }

    /// <summary>
    /// Re-reads everything derived from the progress and failures of the sequence that was just selected.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    async Task RefreshAfterHandover()
    {
        await ReloadObserverState();
        await failures.ReadStateAsync();
        _deliveryFences.Clear();
        await RestoreDeliveryFences();
        RestoreShadowClaims();
    }

    /// <summary>
    /// Writes the failed partitions, unless this activation does not own the definition.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Failed partitions are stored by observer identifier alone, like the definition, so an inert activation writing
    /// them would overwrite those of the real observer. The storage provider refuses such a write as well.
    /// </remarks>
    async Task WriteFailedPartitions()
    {
        if (!KeyOwnsDefinition) return;
        try
        {
            await failures.WriteStateAsync();
        }
        catch (ObserverKeyDoesNotMatchDefinition)
        {
            await RelinquishOwnershipIfMoved();
        }
    }

    /// <summary>
    /// Makes an activation inert after its stored definition moves to another sequence.
    /// </summary>
    /// <returns>Whether the activation relinquished ownership.</returns>
    async Task<bool> RelinquishOwnershipIfMoved()
    {
        await observerDefinition.ReadStateAsync();
        if (KeyOwnsDefinition) return false;
        if (!_observerExists) return true;

        _observerExists = false;
        _subscription = ObserverSubscription.Unsubscribed;
        _pendingHandledCounts.Clear();
        _pendingRetryReminders.Clear();
        _failureStateWritePending = false;
        await TransitionTo<Disconnected>();
        await RemoveFailedPartitionReminders();
        await UnregisterReminderNamed(AlertReminderName);
        await UnregisterReminderNamed(WebhookProbeReminderName);
        logger.LostOwnershipToAnotherEventSequence(_observerId, _observerKey.EventSequenceId, Definition.EventSequenceId);
        DeactivateOnIdle();
        return true;
    }

    /// <summary>
    /// Throws when this activation does not own the stored definition.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="ObserverKeyDoesNotMatchDefinition">The key addresses another event sequence than the definition.</exception>
    /// <exception cref="ObserverActivationIsStale">A registration moved the definition to this key's event sequence after activation.</exception>
    async Task ThrowIfKeyDoesNotOwnDefinition()
    {
        if (KeyOwnsDefinition) return;

        var activatedOn = Definition.EventSequenceId;
        await observerDefinition.ReadStateAsync();
        if (!KeyOwnsDefinition) throw new ObserverKeyDoesNotMatchDefinition(_observerKey, Definition.EventSequenceId);

        // The definition is shared across namespaces, so a registration through another namespace's activation can
        // move it onto this key's sequence. This activation stayed inert and holds state read under that premise;
        // let a fresh activation take over rather than adopt it.
        DeactivateOnIdle();
        throw new ObserverActivationIsStale(_observerKey, activatedOn);
    }
}
