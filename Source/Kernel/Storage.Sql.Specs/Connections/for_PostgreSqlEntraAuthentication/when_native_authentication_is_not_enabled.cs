// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Connections.for_PostgreSqlEntraAuthentication;

public class when_native_authentication_is_not_enabled : Specification
{
    PostgreSqlEntraAuthentication? _authentication;

    void Because() => _authentication = PostgreSqlEntraAuthentication.From(new Configuration.Storage().PostgreSqlEntra);

    [Fact] void should_not_create_a_credential_provider_by_default() => _authentication.ShouldBeNull();
}
