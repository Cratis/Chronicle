// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Clients.Connections.for_ChronicleConnectionString;

public class when_parsing_url_without_authentication_requested : Specification
{
    ChronicleConnectionString _url;
    ChronicleConnectionString _withCredentialsToo;

    void Establish()
    {
        _url = new ChronicleConnectionString("chronicle://localhost:35000?auth=none");

        // Credentials left behind by a shared configuration must not quietly turn the exchange back on.
        _withCredentialsToo = new ChronicleConnectionString("chronicle://someone:secret@localhost:35000?auth=none");
    }

    [Fact] void should_have_correct_host() => _url.ServerAddress.Host.ShouldEqual("localhost");
    [Fact] void should_have_no_authentication_mode() => _url.AuthenticationMode.ShouldEqual(AuthenticationMode.None);
    [Fact] void should_have_no_authentication_mode_even_when_credentials_are_present() => _withCredentialsToo.AuthenticationMode.ShouldEqual(AuthenticationMode.None);
}
