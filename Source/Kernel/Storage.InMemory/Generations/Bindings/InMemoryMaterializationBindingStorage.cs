// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.InMemory.Generations.Bindings;

/// <summary>
/// Represents the in memory materialization binding storage.
/// </summary>
/// <param name="timeProvider">The clock used for lifecycle stamps.</param>
public sealed class InMemoryMaterializationBindingStorage(TimeProvider? timeProvider = null) : IMaterializationBindingStorage, IGenerationBuildStorage, IGenerationWriterGrants
{
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    readonly object _lock = new();
    readonly Dictionary<(EventStoreName Store, string Key), MaterializationBinding> _bindings = [];
    readonly Dictionary<(EventStoreName Store, EventStoreNamespaceName Namespace, PublicationReceiptId Receipt), (MaterializationPublicationRequest Request, MaterializationPublication Outcome)> _receipts = [];
    readonly Dictionary<(EventStoreName Store, string Gate), long> _gates = [];
    readonly Dictionary<(EventStoreName Store, string Gate), (GenerationBuildOperationId Attempt, long Epoch)> _granted = [];
    readonly Dictionary<(EventStoreName Store, string Key, MaterializationGeneration Generation), MaterializationExecutionSpecification> _executions = [];
    readonly Dictionary<(EventStoreName Store, string Scope, GenerationBuildOperationId Operation), (GenerationBuildRequest Request, GenerationReservation Reservation)> _operations = [];
    readonly Dictionary<(EventStoreName Store, string Key, MaterializationGeneration Generation), GenerationObserverBinding> _observers = [];
    readonly Dictionary<(EventStoreName Store, string Gate), Sinks.InMemorySink> _destinations = [];

    /// <inheritdoc/>
    public Task<GenerationWriterGrant?> TryAcquire(EventStoreName store, GenerationObserverBinding binding, GenerationBuildOperationId attempt, long expectedEpoch)
    {
        lock (_lock)
        {
            var identity = (store, binding.Key.Value, binding.Identity.Generation);
            var gate = new GenerationGateId(binding.Key, binding.Identity.Generation);
            if (expectedEpoch < 0) return Task.FromResult<GenerationWriterGrant?>(null);
            if (attempt.Value == Guid.Empty) return Task.FromResult<GenerationWriterGrant?>(null);
            if (!_observers.TryGetValue(identity, out var recorded)) return Task.FromResult<GenerationWriterGrant?>(null);
            if (recorded != binding) return Task.FromResult<GenerationWriterGrant?>(null);
            var record = _bindings[(store, binding.Key.Value)].Generations.Single(_ => _.Identity == binding.Identity);
            if (record.State == MaterializationGenerationState.Abandoned) return Task.FromResult<GenerationWriterGrant?>(null);
            if (_gates.GetValueOrDefault((store, gate.Value)) != expectedEpoch) return Task.FromResult<GenerationWriterGrant?>(null);
            var epoch = Revoke(store, gate);
            _granted[(store, gate.Value)] = (attempt, epoch);
            return Task.FromResult<GenerationWriterGrant?>(new(store, binding, attempt, epoch));
        }
    }

    /// <inheritdoc/>
    public Task<GenerationWriterGrant?> GetAdmitted(EventStoreName store, GenerationObserverBinding binding)
    {
        lock (_lock)
        {
            var gate = new GenerationGateId(binding.Key, binding.Identity.Generation);
            if (!_granted.TryGetValue((store, gate.Value), out var grant)) return Task.FromResult<GenerationWriterGrant?>(null);
            if (grant.Epoch <= 0 || _gates.GetValueOrDefault((store, gate.Value)) != grant.Epoch) return Task.FromResult<GenerationWriterGrant?>(null);
            if (!_observers.TryGetValue((store, binding.Key.Value, binding.Identity.Generation), out var recorded) || recorded != binding) return Task.FromResult<GenerationWriterGrant?>(null);
            var record = _bindings[(store, binding.Key.Value)].Generations.Single(_ => _.Identity == binding.Identity);
            if (record.State == MaterializationGenerationState.Abandoned) return Task.FromResult<GenerationWriterGrant?>(null);
            return Task.FromResult<GenerationWriterGrant?>(new(store, binding, grant.Attempt, grant.Epoch));
        }
    }

    /// <inheritdoc/>
    /// <exception cref="GenerationBuildConflict">An immutable reservation claim conflicts.</exception>
    /// <exception cref="InvalidMaterializationExecution">The adopted execution is missing.</exception>
    public Task<GenerationReservation> ReserveNext(EventStoreName store, MaterializationKey key, MaterializationExecutionSpecification execution, GenerationBuildOperationId operationId, DateTimeOffset at)
    {
        execution.EnsureValid();
        lock (_lock)
        {
            var request = new GenerationBuildRequest(key, execution, operationId, at);
            var operationKey = (store, $"{key.Incarnation.Value:N}/{Uri.EscapeDataString(key.Namespace.Value)}", operationId);
            if (_operations.TryGetValue(operationKey, out var previous))
            {
                if (previous.Request != request) throw new GenerationBuildConflict();
                if (!previous.Request.At.EqualsExact(request.At)) throw new GenerationBuildConflict();
                return Task.FromResult(previous.Reservation);
            }
            if (!_bindings.TryGetValue((store, key.Value), out var binding)) return Task.FromResult(new GenerationReservation(GenerationReservationResult.UnknownMaterialization));
            if (binding.Epoch == ServingEpoch.Unset) return Task.FromResult(new GenerationReservation(GenerationReservationResult.NotBound));
            if (!_executions.ContainsKey((store, key.Value, MaterializationGeneration.First))) throw new InvalidMaterializationExecution();
            var candidate = binding.Generations.FirstOrDefault(_ => _.State is MaterializationGenerationState.Building or MaterializationGenerationState.Following);
            if (candidate is not null) return Task.FromResult(new GenerationReservation(GenerationReservationResult.CandidateInFlight, candidate));
            var identity = MaterializationGenerationIdentity.For(key.Projection, binding.Next.Next(), binding.Container);
            EnsureUnclaimed(store, key, identity);
            var record = new MaterializationGenerationRecord(identity, MaterializationGenerationState.Building, execution.Revision, at);
            var reservation = new GenerationReservation(GenerationReservationResult.ReservationCreated, record);
            _bindings[(store, key.Value)] = binding with { Next = identity.Generation, Generations = Array.AsReadOnly(binding.Generations.Append(record).ToArray()) };
            _executions.Add((store, key.Value, identity.Generation), execution);
            _observers.Add((store, key.Value, identity.Generation), new(key, identity, execution.EventSequence, execution.Revision));
            _operations.Add(operationKey, (request, reservation));
            return Task.FromResult(reservation);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="GenerationBuildConflict">The first generation already has different execution inputs.</exception>
    public Task<bool> TryAdoptExecution(EventStoreName store, MaterializationKey key, MaterializationExecutionSpecification execution)
    {
        execution.EnsureValid();
        lock (_lock)
        {
            var first = _bindings.GetValueOrDefault((store, key.Value))?.Generations.SingleOrDefault(_ => _.Identity.Generation.IsFirst);
            if (first is null) return Task.FromResult(false);
            var snapshot = execution with { HasKnownHistoricalProvenance = false };
            var id = (store, key.Value, MaterializationGeneration.First);
            if (_executions.TryGetValue(id, out var previous))
            {
                if (snapshot != previous) throw new GenerationBuildConflict();
                return Task.FromResult(true);
            }
            EnsureUnclaimed(store, key, first.Identity);
            _executions.Add(id, snapshot);
            _observers.Add(id, new(key, first.Identity, snapshot.EventSequence, snapshot.Revision));
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<MaterializationExecutionSpecification?> GetExecution(EventStoreName store, MaterializationKey key, MaterializationGeneration generation)
    {
        lock (_lock) return Task.FromResult(_executions.GetValueOrDefault((store, key.Value, generation)));
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidMaterializationExecution">The observer has ambiguous incarnation claims.</exception>
    public Task<GenerationObserverBinding?> ResolveObserver(EventStoreName store, EventStoreNamespaceName ns, EventSequenceId eventSequence, ObserverId observerId)
    {
        lock (_lock)
        {
            var matches = _observers.Where(_ => _.Key.Store == store && _.Value.Key.Namespace == ns && _.Value.EventSequence == eventSequence && _.Value.Identity.ObserverId == observerId).Select(_ => _.Value).ToArray();
            if (matches.Length > 1) throw new InvalidMaterializationExecution();
            if (matches.Length == 0) EnsureUnbound(store, ns, eventSequence, observerId);
            return Task.FromResult(matches.SingleOrDefault());
        }
    }

    /// <inheritdoc/>
    public Task<MaterializationBinding?> Get(EventStoreName store, MaterializationKey key)
    {
        lock (_lock)
        {
            return Task.FromResult(_bindings.GetValueOrDefault((store, key.Value)));
        }
    }

    /// <inheritdoc/>
    public Task<FirstGenerationAdoption> TryAdoptFirst(EventStoreName store, MaterializationKey key, MaterializationGenerationRecord record)
    {
        lock (_lock)
        {
            if (_bindings.TryGetValue((store, key.Value), out var binding))
            {
                return Task.FromResult(binding.Generations.FirstOrDefault(_ => _.Identity.Generation.IsFirst) == record ? FirstGenerationAdoption.AlreadyAdopted : FirstGenerationAdoption.Conflicting);
            }
            _bindings.Add((store, key.Value), new(key, record.Identity.Destination, MaterializationGeneration.First, record.Identity.Destination, ServingEpoch.Unset, MaterializationGeneration.First, null, null, Array.AsReadOnly<MaterializationGenerationRecord>([record])));
            return Task.FromResult(FirstGenerationAdoption.Adopted);
        }
    }

    /// <inheritdoc/>
    public Task<bool> TrySetBindingEpoch(EventStoreName store, MaterializationKey key, ServingEpoch expected, ServingEpoch next)
    {
        lock (_lock)
        {
            if (!_bindings.TryGetValue((store, key.Value), out var binding))
            {
                return Task.FromResult(false);
            }
            if (binding.Epoch == next)
            {
                return Task.FromResult(true);
            }
            if (binding.Epoch != expected)
            {
                return Task.FromResult(false);
            }
            _bindings[(store, key.Value)] = binding with { Epoch = next };
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<GenerationReservation> ReserveNext(EventStoreName store, MaterializationKey key, MaterializationRevision revision, DateTimeOffset at)
    {
        lock (_lock)
        {
            if (!_bindings.TryGetValue((store, key.Value), out var binding))
            {
                return Task.FromResult(new GenerationReservation(GenerationReservationResult.UnknownMaterialization));
            }
            if (binding.Epoch == ServingEpoch.Unset)
            {
                return Task.FromResult(new GenerationReservation(GenerationReservationResult.NotBound));
            }
            var candidate = binding.Generations.FirstOrDefault(_ => _.State is MaterializationGenerationState.Building or MaterializationGenerationState.Following);
            if (candidate is not null)
            {
                return Task.FromResult(new GenerationReservation(GenerationReservationResult.CandidateInFlight, candidate));
            }
            var record = new MaterializationGenerationRecord(MaterializationGenerationIdentity.For(key.Projection, binding.Next.Next(), binding.Container), MaterializationGenerationState.Building, revision, at);
            _bindings[(store, key.Value)] = binding with { Next = record.Identity.Generation, Generations = Array.AsReadOnly(binding.Generations.Append(record).ToArray()) };
            return Task.FromResult(new GenerationReservation(GenerationReservationResult.ReservationCreated, record));
        }
    }

    /// <inheritdoc/>
    public Task<GenerationTransitionResult> TryMarkFollowing(EventStoreName store, MaterializationKey key, MaterializationGeneration generation) => Transition(store, key, generation, MaterializationGenerationState.Following);

    /// <inheritdoc/>
    public Task<GenerationTransitionResult> TryAbandon(EventStoreName store, MaterializationKey key, MaterializationGeneration generation) => Transition(store, key, generation, MaterializationGenerationState.Abandoned);

    /// <inheritdoc/>
    public Task<MaterializationPublication> TryPublish(EventStoreName store, MaterializationKey key, MaterializationGeneration expectedActive, ServingEpoch expectedEpoch, MaterializationGeneration candidate, PublicationReceiptId receipt) =>
        TryPublish(store, key, expectedActive, expectedEpoch, candidate, TimeSpan.Zero, receipt);

    /// <inheritdoc/>
    public Task<MaterializationPublication> TryPublish(EventStoreName store, MaterializationKey key, MaterializationGeneration expectedActive, ServingEpoch expectedEpoch, MaterializationGeneration candidate, TimeSpan rollbackWindow, PublicationReceiptId receipt) =>
        Activate(store, new(key, expectedActive, expectedEpoch, candidate, rollbackWindow, receipt, false));

    /// <inheritdoc/>
    public Task<MaterializationPublication> TryRestore(EventStoreName store, MaterializationKey key, MaterializationGeneration expectedActive, ServingEpoch expectedEpoch, MaterializationGeneration candidate, TimeSpan rollbackWindow, PublicationReceiptId receipt) =>
        Activate(store, new(key, expectedActive, expectedEpoch, candidate, rollbackWindow, receipt, true));

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<GenerationContainerClaim>> GetClaims(EventStoreName store, EventStoreNamespaceName ns, ReadModelContainerName container)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyCollection<GenerationContainerClaim>>(_bindings.Where(_ => _.Key.Store == store && _.Value.Key.Namespace == ns).SelectMany(_ => _.Value.Generations).Where(_ => _.Identity.Destination == container).Select(_ => new GenerationContainerClaim(_.Identity.Destination, _.Identity.Generation, _.State)).ToArray());
        }
    }

    /// <summary>
    /// Executes a synchronous in-memory operation under the same lock as admission and publication.
    /// </summary>
    /// <typeparam name="T">The type the operation returns.</typeparam>
    /// <param name="grant">The fixed admission.</param>
    /// <param name="operation">The synchronous operation (in-memory sink tasks are already completed).</param>
    /// <returns>The operation result.</returns>
    /// <exception cref="GenerationWriterGrantRevoked">The grant's epoch is no longer current.</exception>
    internal T WithGrant<T>(GenerationWriterGrant grant, Func<T> operation)
    {
        lock (_lock)
        {
            if (_gates.GetValueOrDefault((grant.Store, grant.Gate.Value)) != grant.Epoch || grant.Epoch <= 0)
                throw new GenerationWriterGrantRevoked();
            return operation();
        }
    }

    /// <summary>
    /// Reuses destination data independently of sink wrappers and grant refresh.
    /// </summary>
    /// <param name="grant">The fixed admission.</param>
    /// <param name="create">Creates a destination only when it has never been opened.</param>
    /// <returns>The shared destination.</returns>
    internal Sinks.InMemorySink GetDestination(GenerationWriterGrant grant, Func<Sinks.InMemorySink> create) => WithGrant(grant, () =>
    {
        var key = (grant.Store, grant.Gate.Value);
        if (!_destinations.TryGetValue(key, out var sink)) _destinations.Add(key, sink = create());
        return sink;
    });

    static void EnsureGenerationUnclaimed(MaterializationBinding binding, MaterializationKey key, MaterializationGenerationIdentity identity)
    {
        if (binding.Generations.Any(record => record.Identity.Destination == identity.Destination)) throw new GenerationBuildConflict();
        if (binding.Key.Incarnation != key.Incarnation) return;
        if (binding.Generations.Any(record => record.Identity.ObserverId == identity.ObserverId)) throw new GenerationBuildConflict();
    }

    void EnsureUnbound(EventStoreName store, EventStoreNamespaceName ns, EventSequenceId eventSequence, ObserverId observerId)
    {
        foreach (var binding in _bindings.Where(_ => _.Key.Store == store && _.Value.Key.Namespace == ns).Select(_ => _.Value))
        {
            foreach (var generation in binding.Generations.Where(_ => _.Identity.ObserverId == observerId))
            {
                if (!_executions.TryGetValue((store, binding.Key.Value, generation.Identity.Generation), out var execution)) throw new InvalidMaterializationExecution();
                if (execution.EventSequence == eventSequence) throw new InvalidMaterializationExecution();
            }
        }
    }

    void EnsureUnclaimed(EventStoreName store, MaterializationKey key, MaterializationGenerationIdentity identity)
    {
        foreach (var binding in _bindings.Where(_ => _.Key.Store == store && _.Value.Key.Namespace == key.Namespace).Select(_ => _.Value))
        {
            if (binding.Key == key) continue;
            EnsureGenerationUnclaimed(binding, key, identity);
        }
    }

    Task<MaterializationPublication> Activate(EventStoreName store, MaterializationPublicationRequest request)
    {
        lock (_lock)
        {
            var receiptKey = (store, request.Key.Namespace, request.Receipt);
            if (_receipts.TryGetValue(receiptKey, out var previous))
            {
                return Task.FromResult(previous.Request == request ? previous.Outcome : previous.Outcome with { Result = MaterializationPublicationResult.ReceiptConflict });
            }
            if (!_bindings.TryGetValue((store, request.Key.Value), out var binding))
            {
                return Task.FromResult(new MaterializationPublication(MaterializationPublicationResult.UnknownMaterialization, MaterializationGeneration.Unspecified, ServingEpoch.Unset));
            }
            var now = _timeProvider.GetUtcNow();
            var outcome = request.Decide(binding, now);
            if (outcome.Result == MaterializationPublicationResult.Published)
            {
                _bindings[(store, request.Key.Value)] = Publish(store, binding, request, now);
            }
            _receipts.Add(receiptKey, (request, outcome));
            return Task.FromResult(outcome);
        }
    }

    MaterializationBinding Publish(EventStoreName store, MaterializationBinding binding, MaterializationPublicationRequest request, DateTimeOffset now)
    {
        var gate = new GenerationGateId(request.Key, request.Candidate);
        var epoch = Revoke(store, gate);
        Revoke(store, new(request.Key, request.Active));
        var records = binding.Generations.Select(record => request.Apply(record, now)).ToArray();
        return binding with { Active = request.Candidate, Destination = records.Single(_ => _.Identity.Generation == request.Candidate).Identity.Destination, Epoch = request.Epoch.Next(), LastReceipt = request.Receipt, Fence = new(gate, epoch), Generations = Array.AsReadOnly(records) };
    }

    Task<GenerationTransitionResult> Transition(EventStoreName store, MaterializationKey key, MaterializationGeneration generation, MaterializationGenerationState target)
    {
        lock (_lock)
        {
            if (!_bindings.TryGetValue((store, key.Value), out var binding))
            {
                return Task.FromResult(GenerationTransitionResult.UnknownGeneration);
            }
            var record = binding.Generations.FirstOrDefault(_ => _.Identity.Generation == generation);
            if (record is null)
            {
                return Task.FromResult(GenerationTransitionResult.UnknownGeneration);
            }
            if (record.State == target)
            {
                return Task.FromResult(GenerationTransitionResult.AlreadyInState);
            }
            if (!record.State.CanTransitionTo(target))
            {
                return Task.FromResult(GenerationTransitionResult.Refused);
            }
            _bindings[(store, key.Value)] = binding with { Generations = Array.AsReadOnly(binding.Generations.Select(_ => _ == record ? _ with { State = target } : _).ToArray()) };
            return Task.FromResult(GenerationTransitionResult.Transitioned);
        }
    }

    long Revoke(EventStoreName store, GenerationGateId gate)
    {
        var key = (store, gate.Value);
        return _gates[key] = checked(_gates.GetValueOrDefault(key) + 1);
    }
}
