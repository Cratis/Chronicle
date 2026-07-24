// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_UserStorage.when_getting_by_email;

public class and_stored_casing_differs : given.a_user_storage
{
    User? _byUpperCase;
    User? _byExactCase;
    User? _byUnknownEmail;

    async Task Because()
    {
        _byUpperCase = await _storage.GetByEmail("ADMIN@CRATIS.IO");
        _byExactCase = await _storage.GetByEmail("admin@cratis.io");
        _byUnknownEmail = await _storage.GetByEmail("nobody@cratis.io");
    }

    [Fact] void should_find_the_user_when_casing_differs() => _byUpperCase.ShouldNotBeNull();
    [Fact] void should_return_the_stored_user_when_casing_differs() => _byUpperCase!.Id.ShouldEqual(_userId);
    [Fact] void should_still_find_the_user_with_exact_casing() => _byExactCase.ShouldNotBeNull();
    [Fact] void should_not_match_a_different_email() => _byUnknownEmail.ShouldBeNull();
}
