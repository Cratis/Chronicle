// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.InMemory.Security;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict.for_TokenStore.given;

public class a_token_store : Specification
{
    protected TokenStorage _storage;
    protected TokenStore _store;

    void Establish()
    {
        _storage = new();
        _store = new(_storage);
    }
}
