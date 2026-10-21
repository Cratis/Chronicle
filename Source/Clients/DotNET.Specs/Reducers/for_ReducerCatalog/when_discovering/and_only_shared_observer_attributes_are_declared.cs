// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_discovering;

public class and_only_shared_observer_attributes_are_declared : given.self_reducers
{
    void Establish()
    {
        _artifacts.Reducers.Returns([typeof(SharedAttributes)]);
        _store = CreateStore();
    }

    Task Because() => _store.DiscoverAll();

    [Fact] void should_not_discover_projection_ownership() => typeof(SharedAttributes).HasModelBoundProjectionAttributes().ShouldBeFalse();
    [Fact] void should_select_the_declared_sequence() => _store.Reducers.GetHandlerFor<SharedAttributes>().EventSequenceId.Value.ShouldEqual("self-sequence");
    [Fact] void should_preserve_passivity() => _store.Reducers.GetHandlerFor<SharedAttributes>().IsActive.ShouldBeFalse();
    [Fact] void should_preserve_the_configured_identity() => _store.Reducers.GetHandlerFor<SharedAttributes>().Id.Value.ShouldEqual("shared-self");

    [Reducer("shared-self"), EventSequence("self-sequence"), Passive, Tag("label"), FilterEventsByTag("accepted")]
    public record SharedAttributes(int Value)
    {
        public static SharedAttributes Reduce(OwnershipEvent @event, SharedAttributes? current) => new(@event.Value);
    }
}
