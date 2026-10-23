// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Connections.for_PostgreSqlEntraAuthentication.when_requesting_a_password;

public class and_the_token_is_still_valid : given.an_authentication_provider
{
    string _first;
    string _second;

    async Task Establish()
    {
        _first = await Authentication.Password();
        Now = Now.AddMinutes(54);
    }

    async Task Because() => _second = await Authentication.Password();

    [Fact] void should_reuse_the_token() => _second.ShouldEqual(_first);
    [Fact] void should_not_acquire_another_token() => Credential.Requests.Count.ShouldEqual(1);
}
