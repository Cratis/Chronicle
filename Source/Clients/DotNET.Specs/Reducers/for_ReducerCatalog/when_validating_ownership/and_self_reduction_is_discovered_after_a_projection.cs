// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_validating_ownership;

public class and_self_reduction_is_discovered_after_a_projection : given.self_reducers
{
    Exception _error;

    void Establish()
    {
        // A prior projection owner must block a newly discovered self reducer too.
        var projection = Substitute.For<Projections.IProjectionHandler>();
        projection.ReadModelType.Returns(typeof(PlainSelf));
        _artifacts.Reducers.Returns([typeof(PlainSelf)]);
        _options.ExplicitArtifacts.RegisterReducer<PlainSelf>();
        _store = CreateStore();
        var registrations = typeof(Projections.Projections).GetField("_explicitHandlers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        registrations.SetValue(_store.Projections, new[] { projection });
    }

    async Task Because() => _error = await Catch.Exception(_store.Reducers.Discover);

    [Fact] void should_refuse_conflicting_discovery() => _error.ShouldBeOfExactType<AmbiguousReadModelBuilder>();
    [Fact] void should_not_publish_a_reducer() => _store.Reducers.GetAllHandlers().ShouldBeEmpty();
    [Fact] void should_retain_the_previously_registered_projection() => _store.Projections.GetAllHandlers().Count().ShouldEqual(1);
}
