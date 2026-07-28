// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Storage.InMemory.Security.for_AuthorizationStorage;

public class when_pruning : given.an_authorization_storage
{
    static readonly ApplicationId _application = new(Guid.NewGuid());
    long _removed;
    Authorization _recent;

    async Task Establish()
    {
        await _storage.Create(Authorization(_application, "user", DateTimeOffset.UtcNow.AddDays(-2)));
        _recent = Authorization(_application, "user", DateTimeOffset.UtcNow);
        await _storage.Create(_recent);
    }

    async Task Because() => _removed = await _storage.Prune(DateTimeOffset.UtcNow.AddDays(-1));

    [Fact] void should_report_one_authorization_removed() => _removed.ShouldEqual(1L);
    [Fact] async Task should_keep_the_recent_authorization() => (await _storage.GetById(_recent.Id)).ShouldNotBeNull();
}
