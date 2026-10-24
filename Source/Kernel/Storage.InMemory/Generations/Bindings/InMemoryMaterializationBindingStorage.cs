// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.InMemory.Generations.Bindings;

/// <summary>
/// Represents the in memory materialization binding storage.
/// </summary>
/// <param name="timeProvider">The clock used for lifecycle stamps.</param>
public sealed class InMemoryMaterializationBindingStorage(TimeProvider? timeProvider = null) : IMaterializationBindingStorage
{
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    readonly object _lock = new();
    readonly Dictionary<(EventStoreName Store, string Key), MaterializationBinding> _bindings = [];
    readonly Dictionary<(EventStoreName Store, EventStoreNamespaceName Namespace, PublicationReceiptId Receipt), (MaterializationPublicationRequest Request, MaterializationPublication Outcome)> _receipts = [];
    readonly Dictionary<(EventStoreName Store, string Gate), long> _gates = [];

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
