// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage.when_another_silo_saved_after_all_were_loaded;

public class and_reading_without_a_listing : given.two_silos_over_shared_storage
{
    ProjectionDefinition _updated;
    ProjectionDefinition _read;

    async Task Establish()
    {
        await _firstSilo.Save(_definition);
        await _secondSilo.GetAll();
        _updated = _definition with { IsRewindable = false };
        await _firstSilo.Save(_updated);
    }

    async Task Because() => _read = await _secondSilo.Get(_id);

    [Fact] void should_observe_the_other_silos_write_immediately() => _read.ShouldEqual(_updated);
}
