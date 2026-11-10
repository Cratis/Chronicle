// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_ApplicationStorage.when_creating;

public class an_application_whose_identifier_is_held_by_another_client : given.an_application_storage
{
    Exception? _exception;

    async Task Because()
    {
        await _storage.Create(CreateApplication("some-client", "hash"));
        _exception = await Catch.Exception(() => _storage.Create(CreateApplication("another-client", "hash")));
    }

    [Fact] void should_still_fail_because_it_is_a_real_conflict() => _exception.ShouldNotBeNull();
}
