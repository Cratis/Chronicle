// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.InMemory.Security.for_TokenStorage;

public class when_pruning_with_valid_expired_and_inactive_tokens : given.a_token_storage
{
    long _removed;

    async Task Establish()
    {
        await _storage.Create(new Token { Id = "valid", CreationDate = DateTimeOffset.UtcNow.AddDays(-2), Status = TokenStatuses.Valid, ExpirationDate = DateTimeOffset.UtcNow.AddDays(28) });
        await _storage.Create(new Token { Id = "expired", CreationDate = DateTimeOffset.UtcNow.AddDays(-2), Status = TokenStatuses.Valid, ExpirationDate = DateTimeOffset.UtcNow.AddHours(-1) });
        await _storage.Create(new Token { Id = "inactive", CreationDate = DateTimeOffset.UtcNow.AddDays(-2), Status = "revoked" });
    }

    async Task Because() => _removed = await _storage.Prune(DateTimeOffset.UtcNow.AddDays(-1));

    [Fact] void should_report_two_tokens_removed() => _removed.ShouldEqual(2L);
    [Fact] async Task should_keep_the_still_valid_token() => (await _storage.GetById("valid")).ShouldNotBeNull();
    [Fact] async Task should_remove_the_expired_token() => (await _storage.GetById("expired")).ShouldBeNull();
    [Fact] async Task should_remove_the_inactive_token() => (await _storage.GetById("inactive")).ShouldBeNull();
}
