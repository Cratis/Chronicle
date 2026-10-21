// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage.when_another_silo_deleted_after_all_were_loaded;

public class and_getting_all : given.two_silos_over_shared_storage
{
    IEnumerable<ProjectionDefinition> _result;

    async Task Establish()
    {
        await _shared.Save(_definition);
        await _firstSilo.GetAll();
        await _secondSilo.Delete(_id);
    }

    async Task Because() => _result = await _firstSilo.GetAll();

    [Fact] void should_not_include_it() => _result.Select(_ => _.Identifier).ShouldNotContain(_id);
}
