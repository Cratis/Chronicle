// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Aggregates.for_StatefulAggregateRootMutator;

public class when_dehydrating : given.a_stateful_aggregate_root_mutator
{
    async Task Because() => await _mutator.Dehydrate();

    [Fact] void should_call_dehydrate_on_state_provider() => _stateProvider.Received().Dehydrate();
}
