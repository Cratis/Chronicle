// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_creating_an_isolated_pipeline;

public class and_checking_shared_side_effects : given.a_pipeline_manager
{
    IEventStoreNamespaceStorage _namespaceStorage;

    void Establish()
    {
        _namespaceStorage = _storage.GetEventStore(_store).GetNamespace(_namespace);
        _namespaceStorage.ClearReceivedCalls();
        _grainFactory.ClearReceivedCalls();
    }

    void Because() => _manager.CreateIsolated(_store, _namespace, _projection, Substitute.For<ISink>());

    [Fact] void should_not_resolve_the_shared_futures_grain() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_access_the_shared_changeset_storage() => _namespaceStorage.ReceivedCalls().Select(_ => _.GetMethodInfo().Name).ShouldNotContain("get_Changesets");
}
