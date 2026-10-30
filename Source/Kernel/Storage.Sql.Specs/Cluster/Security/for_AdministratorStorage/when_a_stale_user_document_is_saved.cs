// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_AdministratorStorage;

public class when_a_stale_user_document_is_saved : given.an_administrator_storage
{
    Administrator[] _granted;

    async Task Because()
    {
        using var users = new UserStorage(_database);
        var user = new User { Id = _grant.Id, Username = "ordinary" };
        await users.Create(user);
        var stale = await users.GetById(_grant.Id);
        await _storage.Save(_grant);
        await users.Update(stale!);
        _granted = (await _storage.GetAll()).ToArray();
    }

    [Fact] void should_not_overwrite_the_separate_privilege_row() => _granted.ShouldContainOnly([_grant]);
}
