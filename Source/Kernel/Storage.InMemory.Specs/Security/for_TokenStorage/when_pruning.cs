// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.InMemory.Security.for_TokenStorage;

public class when_pruning : given.a_token_storage
{
    long _removed;

    async Task Establish()
    {
        await _storage.Create(new Token { Id = "old", CreationDate = DateTimeOffset.UtcNow.AddDays(-2) });
        await _storage.Create(new Token { Id = "recent", CreationDate = DateTimeOffset.UtcNow });
        await _storage.Create(new Token { Id = "no-date" });
    }

    async Task Because() => _removed = await _storage.Prune(DateTimeOffset.UtcNow.AddDays(-1));

    [Fact] void should_report_one_token_removed() => _removed.ShouldEqual(1L);
    [Fact] async Task should_remove_the_old_token() => (await _storage.GetById("old")).ShouldBeNull();
    [Fact] async Task should_keep_the_recent_token() => (await _storage.GetById("recent")).ShouldNotBeNull();
    [Fact] async Task should_keep_the_token_without_a_creation_date() => (await _storage.GetById("no-date")).ShouldNotBeNull();
}
