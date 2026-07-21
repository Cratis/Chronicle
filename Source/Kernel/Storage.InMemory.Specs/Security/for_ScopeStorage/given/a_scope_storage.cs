// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.InMemory.Security.for_ScopeStorage.given;

public class a_scope_storage : Specification
{
    protected ScopeStorage _storage;

    void Establish() => _storage = new();

    protected static Scope Scope(string id, string name, params string[] resources) =>
        new(id, name, null, null, [.. resources], []);
}
