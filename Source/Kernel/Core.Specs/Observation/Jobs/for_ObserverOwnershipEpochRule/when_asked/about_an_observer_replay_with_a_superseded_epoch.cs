// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOwnershipEpochRule.when_asked;

public class about_an_observer_replay_with_a_superseded_epoch : given.a_rule
{
    void Establish() => PersistObserverWideClaim(ClaimHolderKind.ObserverReplay, 4);
    async Task Because() => _result = await Ask(ObserverReplay(3));

    [Fact] void should_refuse_it() => _result.ShouldBeFalse();
}
