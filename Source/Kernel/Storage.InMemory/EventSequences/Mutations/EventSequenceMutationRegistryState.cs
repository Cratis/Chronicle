// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations;

/// <summary>
/// Holds the atomically published mutation state for one in-memory event store namespace.
/// </summary>
sealed class EventSequenceMutationRegistryState
{
    readonly object _gate = new();
    readonly Dictionary<EventSequenceMutationId, EventSequenceMutationRegistration> _registrations = [];
    readonly Dictionary<EventSequenceIdentityKey, EventSequenceMutationHead> _heads = [];
    readonly Dictionary<EventSequenceMutationId, EventSequenceMutationHistoryEntry> _historiesById = [];
    readonly Dictionary<EventSequenceMutationHistoryKey, EventSequenceMutationHistoryEntry> _historiesByTargetAndOrdinal = [];

    /// <summary>
    /// Begins or resumes a mutation under the namespace lock.
    /// </summary>
    /// <param name="eventStore">The owning event store.</param>
    /// <param name="namespace">The owning namespace.</param>
    /// <param name="request">The request to register.</param>
    /// <param name="proposedTarget">The target proposed for a winning registration.</param>
    /// <returns>The begin result.</returns>
    internal EventSequenceMutationBeginResult Begin(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceMutationRequest request,
        EventSequenceMutationTarget proposedTarget)
    {
        if (request is null || request.Id is null || request.Id == EventSequenceMutationId.NotSet)
        {
            return EventSequenceMutationBeginResult.Invalid(EventSequenceMutationValidator.ValidateRequest(request));
        }

        lock (_gate)
        {
            if (_registrations.TryGetValue(request.Id, out var registration))
            {
                return ResumeRegistered(eventStore, @namespace, request, registration);
            }

            var validation = EventSequenceMutationValidator.ValidateRequest(request);
            if (!validation.IsValid)
            {
                return EventSequenceMutationBeginResult.Invalid(validation);
            }

            validation = EventSequenceMutationValidator.ValidateDeterministicId(request);
            if (!validation.IsValid)
            {
                return EventSequenceMutationBeginResult.Invalid(validation);
            }

            validation = EventSequenceMutationValidator.ValidateTarget(proposedTarget);
            if (!validation.IsValid)
            {
                return EventSequenceMutationBeginResult.Invalid(validation);
            }

            var scope = Scope(request.TargetSequence, eventStore, @namespace);
            validation = EventSequenceMutationValidator.ValidateDefinitionInputs(scope, request, proposedTarget);
            if (!validation.IsValid)
            {
                return EventSequenceMutationBeginResult.Invalid(validation);
            }

            var head = GetHead(request.TargetSequence.Key);
            if (!IsValidHead(scope, head))
            {
                return EventSequenceMutationBeginResult.Corrupt();
            }

            if (head.Active is not null)
            {
                return EventSequenceMutationBeginResult.MutationAlreadyInProgress(head.Active.Id);
            }

            long ordinalValue;
            try
            {
                ordinalValue = checked(head.LastAssignedOrdinal.Value + 1);
            }
            catch (OverflowException)
            {
                return EventSequenceMutationBeginResult.Corrupt();
            }

            var ordinal = new EventSequenceMutationOrdinal(ordinalValue);
            var definition = EventSequenceMutationDefinition.Create(scope, request, proposedTarget);
            var active = new EventSequenceMutation(
                definition,
                ordinal,
                EventSequenceMutationStateVersion.First,
                EventSequenceMutationPhase.Reserved,
                EventSequenceMutationPhase.None,
                EventSequenceMutationRepairState.Unspecified);
            var bound = new EventSequenceMutationRegistration(
                definition,
                EventSequenceMutationRegistryLifecycle.Bound,
                ordinal,
                null);
            var updatedHead = head with { LastAssignedOrdinal = ordinal, Active = active };

            _registrations.Add(request.Id, bound);
            _heads[request.TargetSequence.Key] = updatedHead;

            return EventSequenceMutationBeginResult.Reserved(active, EventSequenceMutationStateToken.Create(scope, active));
        }
    }

    /// <summary>
    /// Applies a mutation state transition under the namespace lock.
    /// </summary>
    /// <param name="eventStore">The owning event store.</param>
    /// <param name="namespace">The owning namespace.</param>
    /// <param name="target">The target identity.</param>
    /// <param name="token">The predecessor state token.</param>
    /// <param name="transition">The transition to apply.</param>
    /// <returns>The transition result.</returns>
    internal EventSequenceMutationRegistryTransitionResult Transition(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        EventSequenceMutationTransition transition)
    {
        var validation = EventSequenceMutationValidator.ValidateIdentity(target);
        if (!validation.IsValid)
        {
            return EventSequenceMutationRegistryTransitionResult.Invalid(validation);
        }

        validation = EventSequenceMutationValidator.ValidateToken(token);
        if (!validation.IsValid)
        {
            return EventSequenceMutationRegistryTransitionResult.Invalid(validation);
        }

        lock (_gate)
        {
            if (!_registrations.TryGetValue(token.Id, out var registration))
            {
                return EventSequenceMutationRegistryTransitionResult.StateConflict();
            }

            var scope = Scope(target, eventStore, @namespace);
            if (!MatchesBinding(scope, target, token, registration))
            {
                return EventSequenceMutationRegistryTransitionResult.StateConflict();
            }

            if (registration.Lifecycle == EventSequenceMutationRegistryLifecycle.Archived)
            {
                return TryGetArchived(scope, registration, out var history)
                    ? EventSequenceMutationRegistryTransitionResult.AlreadyArchived(scope, registration, history!)
                    : EventSequenceMutationRegistryTransitionResult.Corrupt();
            }

            if (registration.Lifecycle != EventSequenceMutationRegistryLifecycle.Bound ||
                !EventSequenceMutationValidator.ValidateRegistration(scope, registration).IsValid)
            {
                return EventSequenceMutationRegistryTransitionResult.Corrupt();
            }

            if (!_heads.TryGetValue(target.Key, out var head) || !IsExactBoundHead(scope, head, registration))
            {
                return EventSequenceMutationRegistryTransitionResult.Corrupt();
            }

            var result = EventSequenceMutationStateMachine.Apply(scope, head.Active!, transition, token);
            if (!result.Validation.IsValid)
            {
                return EventSequenceMutationRegistryTransitionResult.Invalid(result.Validation);
            }

            if (result.Outcome == EventSequenceMutationTransitionOutcome.Conflict)
            {
                return EventSequenceMutationRegistryTransitionResult.StateConflict();
            }

            if (result.Outcome == EventSequenceMutationTransitionOutcome.AlreadyApplied)
            {
                return EventSequenceMutationRegistryTransitionResult.AlreadyApplied(result.Mutation!, result.Token!);
            }

            if (result.Outcome != EventSequenceMutationTransitionOutcome.Applied)
            {
                return EventSequenceMutationRegistryTransitionResult.Corrupt();
            }

            _heads[target.Key] = head with { Active = result.Mutation };
            return EventSequenceMutationRegistryTransitionResult.Applied(result.Mutation!, result.Token!);
        }
    }

    /// <summary>
    /// Archives a terminal mutation under the namespace lock.
    /// </summary>
    /// <param name="eventStore">The owning event store.</param>
    /// <param name="namespace">The owning namespace.</param>
    /// <param name="target">The target identity.</param>
    /// <param name="token">The terminal state token.</param>
    /// <returns>The archive result.</returns>
    internal EventSequenceMutationRegistryArchiveResult Archive(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token)
    {
        var validation = EventSequenceMutationValidator.ValidateIdentity(target);
        if (!validation.IsValid)
        {
            return EventSequenceMutationRegistryArchiveResult.Invalid(validation);
        }

        validation = EventSequenceMutationValidator.ValidateToken(token);
        if (!validation.IsValid)
        {
            return EventSequenceMutationRegistryArchiveResult.Invalid(validation);
        }

        lock (_gate)
        {
            if (!_registrations.TryGetValue(token.Id, out var registration))
            {
                return EventSequenceMutationRegistryArchiveResult.StateConflict();
            }

            var scope = Scope(target, eventStore, @namespace);
            if (!MatchesBinding(scope, target, token, registration))
            {
                return EventSequenceMutationRegistryArchiveResult.StateConflict();
            }

            if (registration.Lifecycle == EventSequenceMutationRegistryLifecycle.Archived)
            {
                if (!TryGetArchived(scope, registration, out var archivedHistory))
                {
                    return EventSequenceMutationRegistryArchiveResult.Corrupt();
                }

                return IsExactArchiveRetry(token, archivedHistory!)
                    ? EventSequenceMutationRegistryArchiveResult.AlreadyArchived(scope, registration, archivedHistory!)
                    : EventSequenceMutationRegistryArchiveResult.StateConflict();
            }

            if (registration.Lifecycle != EventSequenceMutationRegistryLifecycle.Bound ||
                !EventSequenceMutationValidator.ValidateRegistration(scope, registration).IsValid)
            {
                return EventSequenceMutationRegistryArchiveResult.Corrupt();
            }

            if (!_heads.TryGetValue(target.Key, out var head) || !IsExactBoundHead(scope, head, registration))
            {
                return EventSequenceMutationRegistryArchiveResult.Corrupt();
            }

            var prepared = EventSequenceMutationStateMachine.PrepareArchive(scope, head.Active!, token);
            if (!prepared.Validation.IsValid)
            {
                return EventSequenceMutationRegistryArchiveResult.Invalid(prepared.Validation);
            }

            if (prepared.Outcome == EventSequenceMutationArchiveOutcome.Conflict)
            {
                return EventSequenceMutationRegistryArchiveResult.StateConflict();
            }

            if (prepared.Outcome != EventSequenceMutationArchiveOutcome.Prepared || prepared.History is null)
            {
                return EventSequenceMutationRegistryArchiveResult.Corrupt();
            }

            var history = prepared.History;
            var historyKey = new EventSequenceMutationHistoryKey(target.Key, history.Ordinal.Value);
            if ((_historiesById.TryGetValue(history.Id, out var byId) && byId != history) ||
                (_historiesByTargetAndOrdinal.TryGetValue(historyKey, out var byTarget) && byTarget != history) ||
                _historiesById.ContainsKey(history.Id) != _historiesByTargetAndOrdinal.ContainsKey(historyKey))
            {
                return EventSequenceMutationRegistryArchiveResult.Corrupt();
            }

            if (_historiesById.ContainsKey(history.Id))
            {
                return EventSequenceMutationRegistryArchiveResult.Corrupt();
            }

            var archived = registration with
            {
                Lifecycle = EventSequenceMutationRegistryLifecycle.Archived,
                TerminalWitness = history.TerminalWitness
            };
            if (!EventSequenceMutationValidator.ValidateArchivedRegistration(scope, archived, history).IsValid)
            {
                return EventSequenceMutationRegistryArchiveResult.Corrupt();
            }

            _historiesById.Add(history.Id, history);
            _historiesByTargetAndOrdinal.Add(historyKey, history);
            _heads[target.Key] = head with { Active = null };
            _registrations[token.Id] = archived;

            return EventSequenceMutationRegistryArchiveResult.Archived(scope, archived, history);
        }
    }

    /// <summary>
    /// Begins tracking a target under the namespace lock.
    /// </summary>
    /// <param name="eventStore">The owning event store.</param>
    /// <param name="namespace">The owning namespace.</param>
    /// <param name="target">The target identity.</param>
    /// <param name="expected">The expected coverage.</param>
    /// <returns>The tracking result.</returns>
    internal EventSequenceMutationTrackingResult BeginTracking(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceMutationIdentity target,
        EventSequenceMutationCoverage expected)
    {
        var validation = EventSequenceMutationValidator.ValidateIdentity(target);
        if (!validation.IsValid)
        {
            return EventSequenceMutationTrackingResult.Invalid(validation);
        }

        validation = EventSequenceMutationValidator.ValidateTrackingCoverage(expected);
        if (!validation.IsValid)
        {
            return EventSequenceMutationTrackingResult.Invalid(validation);
        }

        var scope = Scope(target, eventStore, @namespace);
        validation = EventSequenceMutationValidator.ValidateScope(scope);
        if (!validation.IsValid)
        {
            return EventSequenceMutationTrackingResult.Invalid(validation);
        }

        lock (_gate)
        {
            var head = GetHead(target.Key);
            if (!IsValidHead(scope, head))
            {
                return EventSequenceMutationTrackingResult.Corrupt();
            }

            if (head.Coverage == EventSequenceMutationCoverage.Unsealed)
            {
                return EventSequenceMutationTrackingResult.AlreadyTracking();
            }

            if (head.Coverage != EventSequenceMutationCoverage.Untracked)
            {
                return EventSequenceMutationTrackingResult.Conflict(head.Coverage);
            }

            _heads[target.Key] = head with { Coverage = EventSequenceMutationCoverage.Unsealed };
            return EventSequenceMutationTrackingResult.Began();
        }
    }

    EventSequenceMutationBeginResult ResumeRegistered(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceMutationRequest request,
        EventSequenceMutationRegistration registration)
    {
        if (!registration.IsExactRequest(request))
        {
            return EventSequenceMutationBeginResult.DefinitionConflict(request.Id);
        }

        var target = registration.Definition.Request.TargetSequence;
        var scope = Scope(target, eventStore, @namespace);
        if (registration.Lifecycle == EventSequenceMutationRegistryLifecycle.Archived)
        {
            return TryGetArchived(scope, registration, out var history)
                ? EventSequenceMutationBeginResult.Archived(scope, registration, history!)
                : EventSequenceMutationBeginResult.Corrupt();
        }

        if (registration.Lifecycle != EventSequenceMutationRegistryLifecycle.Bound ||
            !EventSequenceMutationValidator.ValidateRegistration(scope, registration).IsValid ||
            !_heads.TryGetValue(target.Key, out var head) ||
            !IsExactBoundHead(scope, head, registration))
        {
            return EventSequenceMutationBeginResult.Corrupt();
        }

        return EventSequenceMutationBeginResult.Resumed(head.Active!, EventSequenceMutationStateToken.Create(scope, head.Active!));
    }

    bool TryGetArchived(
        EventSequenceKey scope,
        EventSequenceMutationRegistration registration,
        out EventSequenceMutationHistoryEntry? history)
    {
        history = null;
        if (registration.Ordinal is null ||
            !_historiesById.TryGetValue(registration.Definition.Request.Id, out var byId) ||
            !_historiesByTargetAndOrdinal.TryGetValue(
                new(registration.Definition.Request.TargetSequence.Key, registration.Ordinal.Value),
                out var byTarget) ||
            byId != byTarget ||
            !EventSequenceMutationValidator.ValidateArchivedRegistration(scope, registration, byId).IsValid)
        {
            return false;
        }

        history = byId;
        return true;
    }

    EventSequenceMutationHead GetHead(EventSequenceIdentityKey targetKey) =>
        _heads.TryGetValue(targetKey, out var head) ? head : EventSequenceMutationHead.Initial;

    EventSequenceKey Scope(
        EventSequenceMutationIdentity target,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace) =>
        new(target.Display, eventStore, @namespace);

    bool IsValidHead(EventSequenceKey scope, EventSequenceMutationHead head) =>
        Enum.IsDefined(head.Coverage) &&
        head.LastAssignedOrdinal is { Value: >= 0 } &&
        (head.Active is null ||
         (head.LastAssignedOrdinal == head.Active.Ordinal && EventSequenceMutationValidator.ValidateActive(scope, head.Active).IsValid));

    bool IsExactBoundHead(
        EventSequenceKey scope,
        EventSequenceMutationHead head,
        EventSequenceMutationRegistration registration) =>
        IsValidHead(scope, head) &&
        head.Active is not null &&
        head.Active.Definition == registration.Definition &&
        head.Active.Ordinal == registration.Ordinal &&
        head.LastAssignedOrdinal == registration.Ordinal;

    bool MatchesBinding(
        EventSequenceKey scope,
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        EventSequenceMutationRegistration registration) =>
        token.Scope == scope &&
        token.TargetKey == target.Key &&
        registration.Definition.Request.TargetSequence == target &&
        token.Id == registration.Definition.Request.Id &&
        token.Ordinal == registration.Ordinal &&
        token.DefinitionDigestV1 == registration.Definition.DefinitionDigestV1;

    bool IsExactArchiveRetry(
        EventSequenceMutationStateToken token,
        EventSequenceMutationHistoryEntry history) =>
        token.Phase == EventSequenceMutationPhase.SourceCommitted &&
        token.BlockedFrom == EventSequenceMutationPhase.None &&
        token.RepairState == history.RepairState &&
        token.StateVersion.Value < long.MaxValue &&
        token.StateVersion.Value + 1 == history.TerminalWitness.FinalStateVersion.Value;

    readonly record struct EventSequenceMutationHistoryKey(EventSequenceIdentityKey Target, long Ordinal);
}
