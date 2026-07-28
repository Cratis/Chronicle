// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Security.for_TokenStorage.given;

public class a_token_storage : Specification
{
    protected TokenStorage _storage;

    void Establish() => _storage = new();
}
