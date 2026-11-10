// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_ApplicationStorage.when_creating;

public class the_same_application_twice : given.an_application_storage
{
    Exception? _exception;
    IEnumerable<Application> _all;

    async Task Because()
    {
        await _storage.Create(CreateApplication("chronicle-dev-client", "first-hash"));
        _exception = await Catch.Exception(() => _storage.Create(CreateApplication("chronicle-dev-client", "second-hash")));
        _all = await _storage.GetAll();
    }

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_keep_a_single_record() => _all.Count().ShouldEqual(1);
    [Fact] void should_keep_the_first_writer() => _all.Single().ClientSecret!.Value.ShouldEqual("first-hash");
}
