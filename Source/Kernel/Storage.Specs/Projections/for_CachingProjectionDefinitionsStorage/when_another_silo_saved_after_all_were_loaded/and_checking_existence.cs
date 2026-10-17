// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage.when_another_silo_saved_after_all_were_loaded;

public class and_checking_existence : given.two_silos_over_shared_storage
{
    bool _result;

    async Task Establish()
    {
        await _firstSilo.GetAll();
        await _secondSilo.Save(_definition);
    }

    async Task Because() => _result = await _firstSilo.Has(_id);

    [Fact] void should_know_it_exists() => _result.ShouldBeTrue();
}
