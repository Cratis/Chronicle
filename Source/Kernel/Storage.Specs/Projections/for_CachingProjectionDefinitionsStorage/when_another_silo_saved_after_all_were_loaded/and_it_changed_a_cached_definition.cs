// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage.when_another_silo_saved_after_all_were_loaded;

public class and_it_changed_a_cached_definition : given.two_silos_over_shared_storage
{
    ProjectionDefinition _changed;
    ProjectionDefinition _result;

    async Task Establish()
    {
        _changed = _definition with { ReadModel = "another-read-model" };
        await _shared.Save(_definition);
        await _firstSilo.GetAll();
        await _secondSilo.Save(_changed);
    }

    async Task Because()
    {
        await _firstSilo.GetAll();
        _result = await _firstSilo.Get(_id);
    }

    [Fact] void should_refresh_it_from_the_shared_storage() => _result.ShouldEqual(_changed);
}
