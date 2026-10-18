// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage.when_another_silo_deleted_after_all_were_loaded;

public class and_checking_existence_without_a_listing : given.two_silos_over_shared_storage
{
    bool _exists;

    async Task Establish()
    {
        await _firstSilo.Save(_definition);
        await _secondSilo.GetAll();
        await _firstSilo.Delete(_id);
    }

    async Task Because() => _exists = await _secondSilo.Has(_id);

    [Fact] void should_observe_the_other_silos_delete_immediately() => _exists.ShouldBeFalse();
}
