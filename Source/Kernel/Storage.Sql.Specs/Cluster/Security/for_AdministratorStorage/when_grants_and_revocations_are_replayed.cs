// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_AdministratorStorage;

public class when_grants_and_revocations_are_replayed : given.an_administrator_storage
{
    Administrator[] _granted;
    Administrator[] _revoked;

    async Task Because()
    {
        await _storage.Save(_grant);
        await _storage.Save(_grant);
        _granted = (await _storage.GetAll()).ToArray();
        await _storage.Delete(_grant.Id);
        await _storage.Delete(_grant.Id);
        _revoked = (await _storage.GetAll()).ToArray();
    }

    [Fact] void should_converge_repeated_grants() => _granted.ShouldContainOnly([_grant]);
    [Fact] void should_converge_repeated_revocations() => _revoked.ShouldBeEmpty();
}
