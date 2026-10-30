// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Generations.Admission;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer.given;

public class an_initializer : Specification
{
    protected readonly EventStoreName _name = new("orders");
    protected IStorage _storage;
    protected IEventStoreStorage _store;
    protected IGenerationEnablement _flag;
    protected IGenerationExclusionProof _proof;
    protected IGenerationMembershipProbe _membership;
    protected IGenerationLegacyWork _legacy;
    protected ILegacyMaterializations _models;
    protected fault_audit _audit;
    protected catalog _catalog;
    protected GenerationInitializer _initializer;
    protected GenerationInitializationResult _result;
    protected GenerationFloor _floor;
    protected int _writes;
    protected int _failAfter;

    protected LegacyMaterialization[] _snapshot = [new(new("projection-a"), new("tenant"), new("orders"), new("projection-a"), "42", 2), new(new("projection-b"), new("tenant"), new("items"), new("projection-b"), "100", 0)];

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _store = Substitute.For<IEventStoreStorage>();
        _storage.GetEventStore(_name).Returns(_store);
        _store.EventStore.Returns(_name);
        _flag = Substitute.For<IGenerationEnablement>();
        _flag.Enabled.Returns(true);
        _proof = Substitute.For<IGenerationExclusionProof>();
        _proof.IsProven(_name).Returns(true);
        _membership = Substitute.For<IGenerationMembershipProbe>();
        _membership.Probe(Arg.Any<GenerationFloor>()).Returns(new GenerationMembershipEvidence(true, "uniform"));
        _legacy = Substitute.For<IGenerationLegacyWork>();
        _legacy.Inspect(_name).Returns([]);
        _models = Substitute.For<ILegacyMaterializations>();
        _models.GetAll(_name).Returns(_ => Task.FromResult<IReadOnlyList<LegacyMaterialization>>(_snapshot));
        _floor = new(new(1), new(1), [GenerationCapability.FloorAdmission, GenerationCapability.InPlaceAdoption, GenerationCapability.BindingEpoch], new(Guid.NewGuid()), 0, DateTimeOffset.MinValue, "", "");
        _store.GenerationFloor.Returns(Substitute.For<IGenerationFloorStorage>());
        _store.GenerationFloor.Get().Returns(Task.FromResult<GenerationFloor?>(_floor with { Revision = 1 }));
        Reset();
        _storage.ClearReceivedCalls();
        _store.ClearReceivedCalls();
    }

    protected void Reset()
    {
        _writes = 0;
        _audit = new(AfterWrite);
        _catalog = new(AfterWrite);
        _store.GenerationInitialization.Returns(_audit);
        _initializer = Create();
    }

    protected GenerationInitializer Create(bool withCatalog = true, bool bridgeSupport = false) => new(
        _flag,
        _storage,
        _proof,
        _membership,
        _legacy,
        _models,
        bridgeSupport ? GenerationBinarySupport.Current : new(new(1), new(1), new[] { GenerationCapability.FloorAdmission, GenerationCapability.InPlaceAdoption, GenerationCapability.BindingEpoch }.ToFrozenSet()),
        withCatalog ? _catalog : null);

    void AfterWrite()
    {
        _writes++;
        if (_writes == _failAfter)
        {
            throw new simulated_crash();
        }
    }

    protected sealed class simulated_crash : Exception;

    protected sealed class fault_audit(Action after) : IGenerationInitializationStorage
    {
        readonly Storage.InMemory.Generations.GenerationInitializationStorage _inner = new();
        public Task<IReadOnlyList<GenerationInitializationReceipt>> GetReceipts() => _inner.GetReceipts();
        public Task<IReadOnlyList<GenerationInitializationBlocker>> GetBlockers() => _inner.GetBlockers();
        public async Task<bool> TryRecord(GenerationInitializationReceipt receipt)
        {
            var result = await _inner.TryRecord(receipt);
            after();
            return result;
        }
        public async Task RecordBlocker(GenerationInitializationBlocker blocker)
        {
            await _inner.RecordBlocker(blocker);
            after();
        }
        public async Task ResolveBlocker(GenerationInitializationBlockerKind kind, string subject, DateTimeOffset at)
        {
            await _inner.ResolveBlocker(kind, subject, at);
            after();
        }
    }

    protected sealed class catalog(Action after) : IGenerationCatalogSeam
    {
        readonly Lock _gate = new();
        public readonly Dictionary<(EventStoreName Store, EventStoreNamespaceName Namespace, ProjectionId Projection), MaterializationGenerationRecord> Records = new();
        public readonly Dictionary<(EventStoreName Store, EventStoreNamespaceName Namespace, ProjectionId Projection), uint> Epochs = new();
        public int Calls;
        public ProjectionId? Conflict;
        public bool BoundBeforeAllAdopted;
        public Task<AdoptResult> TryAdoptFirst(EventStoreName eventStore, EventStoreNamespaceName @namespace, MaterializationGenerationRecord record)
        {
            lock (_gate)
            {
                Calls++;
                if (record.Identity.ProjectionId == Conflict)
                {
                    return Task.FromResult(AdoptResult.Conflicting);
                }
                var key = (eventStore, @namespace, record.Identity.ProjectionId);
                if (Records.TryGetValue(key, out var old))
                {
                    return Task.FromResult(old == record ? AdoptResult.AlreadyAdopted : AdoptResult.Conflicting);
                }
                Records.Add(key, record);
                after();
                return Task.FromResult(AdoptResult.Adopted);
            }
        }
        public Task<bool> TrySetBindingEpoch(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId projection, uint expected, uint next)
        {
            lock (_gate)
            {
                Calls++;
                BoundBeforeAllAdopted |= Records.Count < 2;
                var key = (eventStore, @namespace, projection);
                if (Epochs.TryGetValue(key, out var epoch))
                {
                    return Task.FromResult(epoch == next);
                }
                Epochs.Add(key, next);
                after();
                return Task.FromResult(true);
            }
        }
    }
}
