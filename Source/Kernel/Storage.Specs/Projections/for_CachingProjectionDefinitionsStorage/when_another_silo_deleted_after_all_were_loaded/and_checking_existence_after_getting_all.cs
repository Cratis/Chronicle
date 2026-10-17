// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage.when_another_silo_deleted_after_all_were_loaded;

public class and_checking_existence_after_getting_all : given.two_silos_over_shared_storage
{
    bool _result;

    async Task Establish()
    {
        await _shared.Save(_definition);
        await _firstSilo.GetAll();
        await _secondSilo.Delete(_id);
        await _firstSilo.GetAll();
    }

    async Task Because() => _result = await _firstSilo.Has(_id);

    [Fact] void should_know_it_is_gone() => _result.ShouldBeFalse();
}
