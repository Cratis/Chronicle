// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_getting_a_stored_definition;

public class and_it_is_not_in_the_registered_state : given.a_projections_manager_grain
{
    ProjectionId _stored = "stored-projection";
    ProjectionId _missing = "missing-projection";
    ProjectionDefinition _definition;
    ProjectionDefinition _storedResult;
    ProjectionDefinition _missingResult;

    void Establish()
    {
        _definition = CreateDefinition(_stored, "read-model");
        _eventStoreStorage.Projections.Has(_stored).Returns(true);
        _eventStoreStorage.Projections.Get(_stored).Returns(_definition);
        _eventStoreStorage.Projections.Has(_missing).Returns(false);
    }

    async Task Because()
    {
        _storedResult = await _grain.GetStoredProjectionDefinition(_stored);
        _missingResult = await _grain.GetStoredProjectionDefinition(_missing);
    }

    [Fact] void should_return_the_stored_definition() => _storedResult.ShouldEqual(_definition);
    [Fact] void should_return_nothing_when_none_is_stored() => _missingResult.ShouldBeNull();
}
