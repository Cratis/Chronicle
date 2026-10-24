// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_two_publications_compete<THarness> : given.a_bound_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication[] _outcomes;
    Task Establish() => Reserve(true);
    async Task Because() => _outcomes = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => _storage.TryPublish(_store, _key, MaterializationGeneration.First, ServingEpoch.Initial, _candidate, new(Guid.NewGuid()))));
    [Fact] public void should_publish_once() => _outcomes.Count(_ => _.Result == MaterializationPublicationResult.Published).ShouldEqual(1);
    [Fact] public void should_refuse_the_losing_request() => _outcomes.Count(_ => _.Result == MaterializationPublicationResult.StaleBinding).ShouldEqual(1);
    [Fact] public async Task should_advance_the_epoch_once() => (await _storage.Get(_store, _key))!.Epoch.ShouldEqual(new ServingEpoch(2));
}
