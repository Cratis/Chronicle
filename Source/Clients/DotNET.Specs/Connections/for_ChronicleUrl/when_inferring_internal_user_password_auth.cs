// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleUrl;

public class when_inferring_internal_user_password_auth : Specification
{
    ChronicleUrl _url;

    void Establish() => _url = new ChronicleUrl("chronicle://admin:secret@localhost:35000");

    [Fact] void should_have_internal_user_password_auth_mode() => _url.AuthenticationMode.ShouldEqual(AuthenticationMode.InternalUserPassword);
}
