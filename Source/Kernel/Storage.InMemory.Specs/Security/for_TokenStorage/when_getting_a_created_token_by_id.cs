// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.InMemory.Security.for_TokenStorage;

public class when_getting_a_created_token_by_id : given.a_token_storage
{
    Token _result;

    async Task Establish() => await _storage.Create(new Token { Id = "token-1", Subject = "user", Status = "valid" });

    async Task Because() => _result = await _storage.GetById("token-1");

    [Fact] void should_return_the_token() => _result.ShouldNotBeNull();
    [Fact] void should_return_the_same_subject() => _result.Subject.ShouldEqual("user");
}
