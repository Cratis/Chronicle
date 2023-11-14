// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRootStateProviders;

public class when_creating_for_an_aggregate_without_state_provider : given.an_aggregate_root_state_manager
{
    Exception result;

    void Establish()
    {
        reducers_registrar.Setup(_ => _.HasReducerFor(typeof(StateForAggregateRoot))).Returns(false);
        immediate_projections.Setup(_ => _.HasProjectionFor(typeof(StateForAggregateRoot))).Returns(false);
    }

    async Task Because() => result = await Catch.Exception(() => state_providers.CreateFor(aggregate_root));

    [Fact] void should_throw_missing_state_provider_exception() => result.ShouldBeOfExactType<MissingAggregateRootStateProvider>();
}
