// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Connections.for_PostgreSqlEntraAuthentication.when_requesting_a_password;

public class and_no_token_is_cached : given.an_authentication_provider
{
    string _password;
    async Task Because() => _password = await Authentication.Password();

    [Fact] void should_return_the_access_token() => _password.ShouldEqual($"token-{Now:O}");
    [Fact] void should_request_the_postgresql_scope() => Credential.Requests.Single().Scopes.ShouldEqual(["https://ossrdbms-aad.database.windows.net/.default"]);
}
