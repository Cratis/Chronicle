// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Connections.for_PostgreSqlEntraAuthentication.when_requesting_a_password;

public class and_the_token_is_near_expiry : given.an_authentication_provider
{
    string _password;

    async Task Establish()
    {
        await Authentication.Password();
        Now = Now.AddMinutes(55);
    }

    async Task Because() => _password = await Authentication.Password();

    [Fact] void should_return_a_fresh_token() => _password.ShouldEqual($"token-{Now:O}");
    [Fact] void should_acquire_another_token() => Credential.Requests.Count.ShouldEqual(2);
}
