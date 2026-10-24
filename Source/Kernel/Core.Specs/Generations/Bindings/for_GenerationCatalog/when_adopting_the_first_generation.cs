// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Generations.Bindings.for_GenerationCatalog;

public class when_adopting_the_first_generation : given.a_catalog
{
    [Theory]
    [InlineData(FirstGenerationAdoption.Adopted, AdoptResult.Adopted)]
    [InlineData(FirstGenerationAdoption.AlreadyAdopted, AdoptResult.AlreadyAdopted)]
    [InlineData(FirstGenerationAdoption.Conflicting, AdoptResult.Conflicting)]
    async Task should_pass_the_resolved_key_and_map_the_result(FirstGenerationAdoption stored, AdoptResult expected)
    {
        _storage.TryAdoptFirst(_store, _key, _record).Returns(stored);
        (await _catalog.TryAdoptFirst(_store, _namespace, _record)).ShouldEqual(expected);
        await _storage.Received(1).TryAdoptFirst(_store, _key, _record);
    }
}
