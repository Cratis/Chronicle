// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager;

public class when_getting_all_stored_definitions : given.a_projections_manager_grain
{
    ProjectionDefinition _stored;
    IEnumerable<ProjectionDefinition> _result;

    void Establish()
    {
        _stored = CreateDefinition("stored-projection", "read-model");
        _eventStoreStorage.Projections.GetAll().Returns([_stored]);
    }

    async Task Because() => _result = await _grain.GetStoredProjectionDefinitions();

    [Fact] void should_return_a_definition_missing_from_the_registered_state() => _result.ShouldEqual([_stored]);
}
