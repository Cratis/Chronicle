// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_validating_ownership;

public class and_a_projection_is_registered_after_self_reduction : given.self_reducers
{
    Exception _fluentError;
    Exception _modelBoundError;

    async Task Establish()
    {
        _options.ExplicitArtifacts.RegisterReducer<PlainSelf>();
        _store = CreateStore();
        await _store.DiscoverAll();
        _services.ClearReceivedCalls();
    }

    async Task Because()
    {
        _fluentError = await Catch.Exception(RegisterProjection<PlainSelf>);
        _modelBoundError = await Catch.Exception(_store.Projections.Register<PlainSelf>);
    }

    [Fact] void should_reject_fluent_projection_ownership() => _fluentError.ShouldBeOfExactType<AmbiguousReadModelBuilder>();
    [Fact] void should_reject_model_bound_projection_ownership() => _modelBoundError.ShouldBeOfExactType<AmbiguousReadModelBuilder>();
    [Fact] void should_keep_the_self_handler() => _store.Reducers.HasFor<PlainSelf>().ShouldBeTrue();
    [Fact] void should_not_publish_a_projection() => _store.Projections.GetAllHandlers().ShouldBeEmpty();
}
