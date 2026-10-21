// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.InMemory.Identities.for_IdentityStorage;

public class when_getting_by_ids : Specification
{
    IdentityStorage _storage;
    IdentityId _id;
    IdentityId _missing;
    IReadOnlyDictionary<IdentityId, Identity> _result;

    async Task Establish()
    {
        _storage = new IdentityStorage();
        _id = await _storage.GetSingleFor(new Identity("person", "Original name", "username"));
        _missing = IdentityId.New();
        await _storage.Rename("person", "Current name");
    }

    async Task Because() => _result = await _storage.GetByIds([_id, _missing, _id]);

    [Fact] void should_omit_missing_ids() => _result.Keys.ShouldContainOnly(_id);
    [Fact] void should_return_current_names() => _result[_id].Name.ShouldEqual("Current name");
}
