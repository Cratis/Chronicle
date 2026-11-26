// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager;

public class when_getting_producers : given.a_projections_manager_grain
{
    ProjectionDefinition _stored;
    ProjectionDefinition _registered;
    ProjectionDefinition _named;
    ProjectionDefinition _unrelated;
    IEnumerable<ProjectionDefinition> _result;

    void Establish()
    {
        _stored = CreateDefinition("stored-projection", "read-model") with { EventSequenceId = "system" };
        _registered = CreateDefinition("registered-projection", "read-model");
        _named = CreateDefinition("named-projection", "another-read-model");
        _unrelated = CreateDefinition("unrelated-projection", "unrelated-read-model");
        _state.Projections = [_registered, _named, _unrelated];
        _eventStoreStorage.Projections.GetProducersFor("read-model", _named.Identifier).Returns([_stored]);
    }

    async Task Because()
    {
        await _grain.GetProducersFor("read-model", _named.Identifier);
        _result = await _grain.GetProducersFor("read-model", _named.Identifier);
    }

    [Fact] void should_return_a_stored_system_producer_missing_from_registered_state() => _result.ShouldContain(_stored);
    [Fact] void should_return_a_registered_producer() => _result.ShouldContain(_registered);
    [Fact] void should_include_the_named_projection_even_with_a_different_target() => _result.ShouldContain(_named);
    [Fact] void should_exclude_an_unrelated_projection() => _result.ShouldNotContain(_unrelated);
    [Fact] async Task should_use_a_filtered_query_for_both_reads() => await _eventStoreStorage.Projections.Received(2).GetProducersFor("read-model", _named.Identifier);
    [Fact] async Task should_not_scan_stored_definitions() => await _eventStoreStorage.Projections.DidNotReceive().GetAll();
}
