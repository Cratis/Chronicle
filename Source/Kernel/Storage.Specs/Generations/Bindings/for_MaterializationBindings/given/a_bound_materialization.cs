// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings.given;

public abstract class a_bound_materialization<THarness> : Specification
    where THarness : IMaterializationBindingStorageHarness, new()
{
    protected BindingTimeProvider _time = new();
    protected TimeSpan _rollbackWindow = TimeSpan.FromHours(1);
    protected EventStoreName _store = "bindings";
    protected MaterializationKey _key = new(new(Guid.Parse("fa5dc415-e617-48a0-9975-fd1580aa321a")), "one", "projection", "mongodb", SinkConfigurationId.None);
    protected MaterializationGenerationRecord _first;
    protected IMaterializationBindingStorage _storage;
    protected PublicationReceiptId _receipt = new(Guid.NewGuid());
    protected MaterializationGeneration _candidate;
    protected THarness _harness;

    async Task Establish()
    {
        _harness = CreateHarness();
        _storage = _harness.Create(_time);
        _first = new(MaterializationGenerationIdentity.For(_key.Projection, MaterializationGeneration.First, "models"), MaterializationGenerationState.Active, MaterializationRevision.Unknown, DateTimeOffset.UnixEpoch);
        await _storage.TryAdoptFirst(_store, _key, _first);
        await _storage.TrySetBindingEpoch(_store, _key, ServingEpoch.Unset, ServingEpoch.Initial);
    }

    void Destroy() => _harness.Dispose();
    protected virtual THarness CreateHarness() => new();
    protected async Task Reserve(bool following = false)
    {
        _candidate = (await _storage.ReserveNext(_store, _key, "revision", DateTimeOffset.UnixEpoch)).Record!.Identity.Generation;
        if (following) await _storage.TryMarkFollowing(_store, _key, _candidate);
    }
}
