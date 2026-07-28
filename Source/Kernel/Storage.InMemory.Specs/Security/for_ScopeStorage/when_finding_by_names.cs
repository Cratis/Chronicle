// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.InMemory.Security.for_ScopeStorage;

public class when_finding_by_names : given.a_scope_storage
{
    IEnumerable<Scope> _result;

    async Task Establish()
    {
        await _storage.Create(Scope("scope-api", "api"));
        await _storage.Create(Scope("scope-profile", "profile"));
        await _storage.Create(Scope("scope-email", "email"));
    }

    async Task Because() => _result = await _storage.FindByNames(["api", "email"]);

    [Fact] void should_return_only_the_named_scopes() => _result.Select(_ => _.Name).ShouldContainOnly(["api", "email"]);
}
