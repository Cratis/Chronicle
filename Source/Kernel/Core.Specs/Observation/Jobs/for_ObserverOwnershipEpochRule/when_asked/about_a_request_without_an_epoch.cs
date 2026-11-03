// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOwnershipEpochRule.when_asked;

public class about_a_request_without_an_epoch : given.a_rule
{
    async Task Because() => _result = await Ask(PartitionCatchUp(null));

    [Fact] void should_admit_it() => _result.ShouldBeTrue();
    [Fact] void should_not_read_the_observer() => _storage.DidNotReceive().GetEventStore(Arg.Any<Concepts.EventStoreName>());
}
