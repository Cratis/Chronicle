// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Clients.Connections.for_ChronicleConnectionString;

public class when_parsing_url_with_tls_disabled : Specification
{
    ChronicleConnectionString _withoutCredentials;
    ChronicleConnectionString _withCredentials;
    ChronicleConnectionString _withApiKey;
    ChronicleConnectionString _withTls;

    void Establish()
    {
        _withoutCredentials = new ChronicleConnectionString("chronicle://localhost:35001/?disableTls=true");
        _withCredentials = new ChronicleConnectionString("chronicle://someone:secret@localhost:35001/?disableTls=true");
        _withApiKey = new ChronicleConnectionString("chronicle://localhost:35001/?disableTls=true&apiKey=token");
        _withTls = new ChronicleConnectionString("chronicle://localhost:35001/");
    }

    [Fact] void should_disable_tls() => _withoutCredentials.DisableTls.ShouldBeTrue();
    [Fact] void should_keep_tls_when_not_asked_to_disable_it() => _withTls.DisableTls.ShouldBeFalse();
    [Fact] void should_not_fall_back_to_the_development_credentials() => _withoutCredentials.AuthenticationMode.ShouldEqual(AuthenticationMode.None);
    [Fact] void should_keep_explicit_client_credentials() => _withCredentials.AuthenticationMode.ShouldEqual(AuthenticationMode.ClientCredentials);
    [Fact] void should_keep_an_api_key() => _withApiKey.AuthenticationMode.ShouldEqual(AuthenticationMode.ApiKey);
    [Fact] void should_keep_the_setting_when_written_back() => _withoutCredentials.ToString().ShouldContain("disableTls=true");
}
