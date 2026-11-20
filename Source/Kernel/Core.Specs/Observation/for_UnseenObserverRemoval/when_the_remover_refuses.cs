// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_UnseenObserverRemoval;

public class when_the_remover_refuses : given.an_unseen_observer_removal
{
    int _result;

    async Task Because()
    {
        _removalResult = ObserverRemovalResult.Subscribed(First);
        _result = await _removal.Sweep();
    }

    [Fact] void should_not_count_a_removal() => _result.ShouldEqual(0);
    [Fact] void should_have_asked_the_remover() => _removeCalls.ShouldEqual(1);

    protected override Task Seed() => Observer("stale", Old, Old);
}
