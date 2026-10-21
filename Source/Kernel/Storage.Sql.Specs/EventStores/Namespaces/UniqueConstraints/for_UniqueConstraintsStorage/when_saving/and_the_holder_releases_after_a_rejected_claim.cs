// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_saving;

public class and_the_holder_releases_after_a_rejected_claim : given.a_unique_constraints_storage
{
    bool _allowed;

    async Task Establish()
    {
        await _storage.Save("holder", _definition, 42UL, "shared");
        await Catch.Exception(() => _storage.Save("claimant", _definition, 43UL, "shared"));
    }

    async Task Because()
    {
        await _storage.Remove("holder", _definition);
        (_allowed, _) = await _storage.IsAllowed("third-source", _definition, "shared");
    }

    [Fact] void should_free_the_value() => _allowed.ShouldBeTrue();
}
