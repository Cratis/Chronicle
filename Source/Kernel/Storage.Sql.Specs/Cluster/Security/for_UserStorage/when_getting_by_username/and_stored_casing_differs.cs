// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_UserStorage.when_getting_by_username;

public class and_stored_casing_differs : given.a_user_storage
{
    User? _byUpperCase;
    User? _byExactCase;
    User? _byUnknownUsername;

    async Task Because()
    {
        _byUpperCase = await _storage.GetByUsername("ADMIN");
        _byExactCase = await _storage.GetByUsername("admin");
        _byUnknownUsername = await _storage.GetByUsername("someone-else");
    }

    [Fact] void should_find_the_user_when_casing_differs() => _byUpperCase.ShouldNotBeNull();
    [Fact] void should_return_the_stored_user_when_casing_differs() => _byUpperCase!.Id.ShouldEqual(_userId);
    [Fact] void should_still_find_the_user_with_exact_casing() => _byExactCase.ShouldNotBeNull();
    [Fact] void should_not_match_a_different_username() => _byUnknownUsername.ShouldBeNull();
}
