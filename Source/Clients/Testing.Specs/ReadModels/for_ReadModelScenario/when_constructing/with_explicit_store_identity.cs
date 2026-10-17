// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_constructing;

public class with_explicit_store_identity : Specification
{
    Defaults _defaults;
    ReadModelScenario<StoreIdentity> _scenario;
    StoreIdentity _identity;

    void Establish()
    {
        var artifacts = new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(ReadModelIdentityRecorded)],
            Reducers = [typeof(IdentityReducer)]
        };
        _defaults = new(artifacts, null, null, null, "read-model-store", "read-model-namespace");
        _scenario = new(null, _defaults);
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ReadModelIdentityRecorded());
        _identity = _scenario.Instance!;
    }

    [Fact] void should_reduce_with_the_store_name() => _identity.Name.ShouldEqual(new EventStoreName("read-model-store"));
    [Fact] void should_reduce_with_the_namespace() => _identity.Namespace.ShouldEqual(new EventStoreNamespaceName("read-model-namespace"));

    void Destroy()
    {
        _scenario.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }

    [EventType]
    public record ReadModelIdentityRecorded;

    public record StoreIdentity(EventStoreName Name, EventStoreNamespaceName Namespace);

    public class IdentityReducer : IReducerFor<StoreIdentity>
    {
        public StoreIdentity Reduce(ReadModelIdentityRecorded @event, StoreIdentity? current, EventContext context) => new(context.EventStore, context.Namespace);
    }
}
