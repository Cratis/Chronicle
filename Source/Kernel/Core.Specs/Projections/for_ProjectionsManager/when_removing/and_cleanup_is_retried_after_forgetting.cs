// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_removing;

public class and_cleanup_is_retried_after_forgetting : given.a_manager_with_an_orphaned_projection
{
    ObserverRemovalResult _result;

    void Establish()
    {
        _state.Projections = [];
        _eventStoreStorage.Projections.Has(_definition.Identifier).Returns(true);
        _eventStoreStorage.Projections.Get(_definition.Identifier).Returns(_definition);
    }

    async Task Because() => _result = await _grain.Remove(_definition.Identifier);

    [Fact] void should_complete_removal() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);
    [Fact] async Task should_delete_the_remaining_definition() => await _eventStoreStorage.Projections.Received(1).Delete(_definition.Identifier);
}
