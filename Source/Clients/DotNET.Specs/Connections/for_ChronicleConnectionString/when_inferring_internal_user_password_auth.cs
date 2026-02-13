// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Clients.Connections.for_ChronicleConnectionString;

public class when_inferring_internal_user_password_auth : Specification
{
    ChronicleConnectionString _url;

    void Establish() => _url = new ChronicleConnectionString("chronicle://admin:secret@localhost:35000");

    [Fact] void should_have_client_credentials_auth_mode() => _url.AuthenticationMode.ShouldEqual(AuthenticationMode.ClientCredentials);
}
